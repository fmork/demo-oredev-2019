using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data.Mapping;
using demunity.lib;
using demunity.lib.Data;
using demunity.lib.Data.Models;
using demunity.lib.Logging;

namespace demunity.aws.Data
{
    public class LikeRepository : ILikeRepository
    {
        private const string updateExpression = "SET #LikeCount = #LikeCount + :countDelta, #Score = #Score + :scoreDelta";
        private readonly ILogWriter<LikeRepository> logWriter;
        private readonly string tableName;
        private readonly ISystem system;
        private readonly IPhotoRepository photoRepository;
        private readonly IScoreCalculator scoreCalculator;
        private readonly IDynamoDbCore dynamoDbCore;

        public LikeRepository(
            ISystem system,
            IPhotoRepository photoRepository,
            IScoreCalculator scoreCalculator,
            IDynamoDbCore dynamoDbCore,
            ILogWriterFactory logWriterFactory)
        {


            if (logWriterFactory is null)
            {
                throw new System.ArgumentNullException(nameof(logWriterFactory));
            }

            logWriter = logWriterFactory.CreateLogger<LikeRepository>();
            this.system = system ?? throw new System.ArgumentNullException(nameof(system));
            this.photoRepository = photoRepository ?? throw new ArgumentNullException(nameof(photoRepository));
            this.scoreCalculator = scoreCalculator ?? throw new ArgumentNullException(nameof(scoreCalculator));
            this.dynamoDbCore = dynamoDbCore ?? throw new System.ArgumentNullException(nameof(dynamoDbCore));
            tableName = system.Environment.GetVariable(Constants.EnvironmentVariables.DynamoDbTableName);

        }
        public Task SetLikeState(PhotoId photoId, UserId userId, bool like)
        {
            logWriter.LogInformation($"{nameof(SetLikeState)}({nameof(photoId)} = '{photoId}',  {nameof(userId)} = '{userId}', {nameof(like)} = '{like}')");

            return like
                ? AddLikeRecord(userId, photoId)
                : RemoveLikeRecord(userId, photoId);


        }

        private async Task RemoveLikeRecord(UserId userId, PhotoId photoId)
        {
            BatchGetItemRequest request = new BatchGetItemRequest
            {
                RequestItems = new Dictionary<string, KeysAndAttributes>
                {
                    { tableName, new KeysAndAttributes{
                        Keys = new[]{
                            Mappers.PhotoModel.ToDbKey(new PhotoModel{PhotoId = photoId}),
                            Mappers.PhotoLike.ToDbKey(new PhotoLikeRecord{PhotoId=photoId, UserId = userId})
                        }.ToList()
                    }}
                }
            };

            var items = await dynamoDbCore.BatchGetItem(request);

            PhotoModel photo = null;
            PhotoLikeRecord likeRecord = null;

            foreach (var item in items[tableName])
            {
                switch (item[FieldMappings.RecordType].S.ToLower())
                {
                    case "photo":
                        photo = Mappers.PhotoModel.FromDbItem(item);
                        break;
                    case "photolike":
                        likeRecord = Mappers.PhotoLike.FromDbItem(item);
                        break;
                }
            }

            if (likeRecord == null)
            {
                return;
            }

            logWriter.LogInformation($"{nameof(RemoveLikeRecord)}({nameof(userId)} = '{userId}', {nameof(photoId)} = '{photoId}')");

            var scoreDelta = scoreCalculator.GetLikeScore(likeRecord) * -1;

            var deleteItemRequest = new DeleteItemRequest(
                tableName,
                Mappers.PhotoLike.ToDbKey(new PhotoLikeRecord
                {
                    UserId = userId,
                    PhotoId = photoId,
                    CreatedTime = system.Time.UtcNow
                }));

            try
            {
                await dynamoDbCore.DeleteItem(deleteItemRequest);
                await UpdateLikeCountAndScore(photo, -1, scoreDelta);
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(RemoveLikeRecord)}({nameof(userId)} = '{userId}', {nameof(photoId)} = '{photoId}'):\n{ex.ToString()}");
                throw;
            }
        }

        private async Task AddLikeRecord(UserId userId, PhotoId photoId)
        {
            logWriter.LogInformation($"{nameof(AddLikeRecord)}({nameof(userId)} = '{userId}', {nameof(photoId)} = '{photoId}')");

            var photo = await photoRepository.GetPhotoById(photoId, Guid.Empty);
            if (photo.PhotoIsLikedByCurrentUser)
            {
                return;
            }

            PhotoLikeRecord likeRecord = new PhotoLikeRecord
            {
                UserId = userId,
                PhotoId = photoId,
                CreatedTime = DateTimeOffset.UtcNow
            };

            var scoreDelta = scoreCalculator.GetLikeScore(likeRecord);

            var putItemRequest = new PutItemRequest()
            {
                TableName = tableName,
                Item = Mappers.PhotoLike.ToDbItem(likeRecord),
            };

            try
            {
                await dynamoDbCore.PutItem(putItemRequest);
                await UpdateLikeCountAndScore(photo, 1, scoreDelta);
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(AddLikeRecord)}({nameof(userId)} = '{userId}', {nameof(photoId)} = '{photoId}'):\n{ex.ToString()}");
                throw;
            }
        }

        public Task UpdateLikeCountAndScore(PhotoModel photo, int likeCountDelta, double scoreDelta)
        {
            UpdateItemRequest setLikeCountForPhotoRequest = GetLikeCountAndScoreUpdateRequest(Mappers.PhotoModel.ToDbKey(photo), likeCountDelta, scoreDelta);

            var requests = photo.Hashtags
                .Select(hashtag => GetLikeCountUpdateRequest(Mappers.Hashtag.ToDbKey(hashtag), likeCountDelta))
                .Union(new[] { setLikeCountForPhotoRequest });

            return Task.WhenAll(requests.Select(x => dynamoDbCore.UpdateItem(x)));
        }

        private UpdateItemRequest GetLikeCountUpdateRequest(
            Dictionary<string, AttributeValue> dbKey,
            int likeCountDelta)
        {

            Dictionary<string, string> expressionAttributeNames = new Dictionary<string, string>
                {
                    {"#LikeCount", "LikeCount"}
                };

            Dictionary<string, AttributeValue> expressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":count", new AttributeValue{ N = likeCountDelta.ToString()}}
                };

            string lowerLimitExpression = null;
            if (likeCountDelta < 0)
            {
                expressionAttributeValues.Add(":zero", new AttributeValue { N = "0" });
                lowerLimitExpression = "#LikeCount > :zero";
            }

            return new UpdateItemRequest()
            {
                TableName = tableName,
                Key = dbKey,
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues,
                UpdateExpression = "SET #LikeCount = #LikeCount + :count",
                ConditionExpression = lowerLimitExpression
            };
        }

        private UpdateItemRequest GetLikeCountAndScoreUpdateRequest(
            Dictionary<string, AttributeValue> dbKey,
            int likeCountDelta,
            double scoreDelta)
        {



            var expressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":countDelta", new AttributeValue{N = likeCountDelta.ToString()}},
                    {":scoreDelta", new AttributeValue{N = scoreDelta.ToString()}}
                };

            var expressionAttributeNames = new Dictionary<string, string>
                {
                    {"#LikeCount", FieldMappings.Photo.LikeCount},
                    {"#Score", FieldMappings.Photo.Score}
                };


            string lowerLimitExpression = null;
            if (likeCountDelta < 0)
            {
                expressionAttributeValues.Add(":zero", new AttributeValue { N = "0" });
                lowerLimitExpression = "#LikeCount > :zero";
            }

            return new UpdateItemRequest()
            {
                TableName = tableName,
                Key = dbKey,
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues,
                UpdateExpression = updateExpression,
                ConditionExpression = lowerLimitExpression
            };
        }
    }
}