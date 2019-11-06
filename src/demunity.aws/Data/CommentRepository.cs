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
    public class CommentRepository : ICommentRepository
    {
        private readonly ILogWriter<CommentRepository> logWriter;
        private readonly IPhotoRepository photoRepository;
        private readonly IDynamoDbCore dynamoDbCore;
        private readonly ISystem system;
        private readonly IScoreCalculator scoreCalculator;
        private readonly string tableName;

        public CommentRepository(
            IPhotoRepository photoRepository,
            IDynamoDbCore dynamoDbCore,
            ISystem system,
            IScoreCalculator scoreCalculator,
            ILogWriterFactory logWriterFactory)
        {
            if (logWriterFactory is null)
            {
                throw new System.ArgumentNullException(nameof(logWriterFactory));
            }

            logWriter = logWriterFactory.CreateLogger<CommentRepository>();
            this.photoRepository = photoRepository ?? throw new System.ArgumentNullException(nameof(photoRepository));
            this.dynamoDbCore = dynamoDbCore ?? throw new ArgumentNullException(nameof(dynamoDbCore));
            this.system = system ?? throw new ArgumentNullException(nameof(system));
            this.scoreCalculator = scoreCalculator ?? throw new ArgumentNullException(nameof(scoreCalculator));
            tableName = system.Environment.GetVariable(Constants.EnvironmentVariables.DynamoDbTableName);
        }
        public async Task AddPhotoComment(PhotoComment comment)
        {
            logWriter.LogInformation($"{nameof(AddPhotoComment)}({nameof(comment.PhotoId)} = '{comment.PhotoId}', {nameof(comment.UserId)} = '{comment.UserId}', {nameof(comment.Text)} = '{comment.Text}')");

            var photo = await photoRepository.GetPhotoById(comment.PhotoId, Guid.Empty);

            var scoreDelta = scoreCalculator.GetCommentScore(comment);

            var putItemRequest = new PutItemRequest()
            {
                TableName = tableName,
                Item = Mappers.PhotoComment.ToDbItem(comment)
            };

            try
            {
                await dynamoDbCore.PutItem(putItemRequest);
                await UpdateCommentCountAndScore(photo, 1, scoreDelta);
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"{nameof(AddPhotoComment)}({nameof(comment.PhotoId)} = '{comment.PhotoId}', {nameof(comment.UserId)} = '{comment.UserId}', {nameof(comment.Text)} = '{comment.Text}'):\n{ex.ToString()}");

                throw;
            }
        }

        private async Task UpdateCommentCountAndScore(PhotoModel photo, int countDelta, double scoreDelta)
        {
            var updateItemRequests = (new[]
            {
                GetCommentCountAndScoreUpdateRequest(Mappers.PhotoModel.ToDbKey(photo), countDelta, scoreDelta)
            })
           .Union(photo.Hashtags.Select(hashtag =>
               GetCommentCountUpdateRequest(Mappers.HashtagPhoto.ToDbKey(new HashtagPhoto { Hashtag = hashtag, Photo = photo }), countDelta)));

            await Task.WhenAll(updateItemRequests.Select(request => dynamoDbCore.UpdateItem(request)));
        }

        private UpdateItemRequest GetCommentCountUpdateRequest(Dictionary<string, AttributeValue> dbKey, int commentCountDelta)
        {
            var expressionAttributeNames = new Dictionary<string, string>
                {
                    {"#CommentCount", FieldMappings.Photo.CommentCount},
                };
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":count", new AttributeValue{ N = commentCountDelta.ToString()}}
                };
            const string updateExpression = "SET #CommentCount = #CommentCount + :count";

            string lowerLimitExpression = null;
            if (commentCountDelta < 0)
            {
                expressionAttributeValues.Add(":zero", new AttributeValue { N = "0" });
                lowerLimitExpression = "#CommentCount > :zero";
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

        private UpdateItemRequest GetCommentCountAndScoreUpdateRequest(Dictionary<string, AttributeValue> dbKey, int commentCountDelta, double scoreDelta)
        {
            var expressionAttributeNames = new Dictionary<string, string>
                {
                    {"#CommentCount", FieldMappings.Photo.CommentCount},
                    {"#Score", FieldMappings.Photo.Score}
                };
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":count", new AttributeValue{ N = commentCountDelta.ToString()}},
                    {":scoreDelta", new AttributeValue{N=scoreDelta.ToString()}}
                };
            const string updateExpression = "SET #CommentCount = #CommentCount + :count, #Score = #Score + :scoreDelta";

            string lowerLimitExpression = null;
            if (commentCountDelta < 0)
            {
                expressionAttributeValues.Add(":zero", new AttributeValue { N = "0" });
                lowerLimitExpression = "#CommentCount > :zero";
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

        public Task<IEnumerable<PhotoComment>> GetComments(PhotoId photoId)
        {
            logWriter.LogInformation($"{nameof(GetComments)}({nameof(photoId)} = '{photoId}')");
            QueryRequest request = new QueryRequest
            {
                TableName = tableName,
                KeyConditions = new Dictionary<string, Condition>
                {
                    {FieldMappings.PartitionKey, dynamoDbCore.GetStringEqualsCondition(photoId.ToDbValue())},
                    {FieldMappings.SortKey, dynamoDbCore.GetStringBeginsWithCondition("comment")}
                }
            };

            return dynamoDbCore.Query(request, Mappers.PhotoComment);
        }



        public async Task DeletePhotoComment(PhotoComment comment)
        {

            var photo = await photoRepository.GetPhotoById(comment.PhotoId, Guid.Empty);
            var scoreDelta = scoreCalculator.GetCommentScore(comment) * -1;

            var deleteCommentRequest = new DeleteItemRequest
            {
                TableName = tableName,
                Key = Mappers.PhotoComment.ToDbKey(comment)
            };

            try
            {
                await dynamoDbCore.DeleteItem(deleteCommentRequest);
                await UpdateCommentCountAndScore(photo, -1, scoreDelta);

            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(DeletePhotoComment)}():\n{ex.ToString()}");
                throw;
            }
        }
    }
}