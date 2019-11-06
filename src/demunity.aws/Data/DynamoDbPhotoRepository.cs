using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using demunity.aws.Data.Mapping;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using Newtonsoft.Json;
using demunity.lib.Data;
using demunity.lib;
using System.Collections.Concurrent;
using Amazon.DynamoDBv2;

namespace demunity.aws.Data
{
    public class DynamoDbPhotoRepository : IPhotoRepository
    {
        private enum HashtagUpdateMode
        {
            Undefined,
            CreateOnly,
            Update
        }

        private readonly static IEnumerable<HashtagModel> emptyHashtagSequence = new HashtagModel[] { };
        private readonly ILogWriter<DynamoDbPhotoRepository> logWriter;
        private readonly string tableName;
        private readonly IScoreCalculator scoreCalculator;
        private readonly IDynamoDbCore dynamoDbCore;

        public DynamoDbPhotoRepository(
            IScoreCalculator scoreCalculator,
            IDynamoDbCore dynamoDbCore,
            ISystem system,
            ILogWriterFactory logWriterFactory)
        {
            if (system is null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (logWriterFactory is null)
            {
                throw new ArgumentNullException(nameof(logWriterFactory));
            }

            logWriter = logWriterFactory.CreateLogger<DynamoDbPhotoRepository>();
            tableName = system.Environment.GetVariable(Constants.EnvironmentVariables.DynamoDbTableName);
            this.scoreCalculator = scoreCalculator ?? throw new ArgumentNullException(nameof(scoreCalculator));
            this.dynamoDbCore = dynamoDbCore ?? throw new ArgumentNullException(nameof(dynamoDbCore));
        }

        public async Task<PhotoModel> CreatePhoto(PhotoModel photo)
        {
            photo.Score = scoreCalculator.GetPhotoScore(photo);

            logWriter.LogInformation($"{nameof(CreatePhoto)}({nameof(photo.Filename)} = '{photo.Filename}')");
            try
            {
                var item = Mappers.PhotoModel.ToDbItem(photo);
                var request = new PutItemRequest(tableName, item);
                BatchWriteItemRequest hashtagsRequests = GetHashtagsRequests(photo.RawText, photo.Hashtags, photo, HashtagUpdateMode.CreateOnly);

                await dynamoDbCore.PutItem(request);

                if (hashtagsRequests != null)
                {
                    await dynamoDbCore.BatchWriteItem(hashtagsRequests);
                }

                logWriter.LogInformation($"Photo record created for '{photo.Filename}'");
                return photo;
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(CreatePhoto)}(photo.Id = '{photo.PhotoId}'):\n{ex.ToString()}");
                throw new Exception("Error when creating photo.", ex);
            }
        }



        public async Task<IEnumerable<PhotoModel>> GetLatestPhotos(UserId currentUserId)
        {
            logWriter.LogInformation($"{nameof(GetLatestPhotos)}()");
            var request = new QueryRequest
            {
                TableName = tableName,
                IndexName = "GSI2",
                KeyConditionExpression = $"{FieldMappings.RecordType} = :recordType",
                FilterExpression = $"{FieldMappings.Photo.State} = :state",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":recordType", new AttributeValue{S="photo"}},
                    {":state", new AttributeValue{S=PhotoState.PhotoAvailable.ToString()}},
                },
                ScanIndexForward = false, // order descending
                Limit = 250,
                ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES
            };

            var response = (await dynamoDbCore.Query(request, Mappers.PhotoModel));
            var result = new ConcurrentDictionary<PhotoId, PhotoModel>();
            foreach (var photo in response)
            {
                result.TryAdd(photo.PhotoId, photo);
            }
            await AddLikeDataForUser(currentUserId, result);
            return result.Values.OrderByDescending(x => x.CreatedTime);

        }

        public async Task<PhotoModel> GetPhotoById(PhotoId photoId, UserId currentUserId)
        {
            PhotoModel result = null;
            bool photoIsLikedByCurrentUser = false;

            logWriter.LogInformation($"{nameof(GetPhotoById)}({nameof(photoId)} = '{photoId}')");
            var request = new BatchGetItemRequest
            {
                RequestItems = new Dictionary<string, KeysAndAttributes>
                {
                    {
                        tableName,
                        new KeysAndAttributes
                        {
                            Keys = new List<Dictionary<string, AttributeValue>>
                            {
                                Mappers.PhotoModel.ToDbKey(new PhotoModel{ PhotoId = photoId }),
                                Mappers.PhotoLike.ToDbKey(new PhotoLikeRecord
                                {
                                    PhotoId = photoId,
                                    UserId = currentUserId
                                })
                            }
                        }
                    }
                }
            };

            logWriter.LogInformation($"Batch get keys:\n{JsonConvert.SerializeObject(request)}");

            try
            {
                var getItemResponse = await dynamoDbCore.BatchGetItem(request);
                foreach (var item in getItemResponse.SelectMany(r => r.Value))
                {
                    logWriter.LogInformation($"Record type: {item[FieldMappings.RecordType].S}");
                    switch (item[FieldMappings.RecordType].S.ToLowerInvariant())
                    {
                        case "photo":
                            result = Mappers.PhotoModel.FromDbItem(item);
                            break;
                        case "photolike":
                            photoIsLikedByCurrentUser = true;
                            break;
                    }
                }

                if (result != null)
                {
                    result.PhotoIsLikedByCurrentUser = photoIsLikedByCurrentUser;
                }
                return result;
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"{nameof(GetPhotoById)}({nameof(photoId)} = '{photoId}'):\n{ex.ToString()}");
                throw;
            }
        }

        public async Task<PhotoState> GetPhotoState(PhotoId photoId)
        {
            logWriter.LogInformation($"{nameof(GetPhotoState)}({nameof(photoId)} = '{photoId}')");

            var request = new GetItemRequest(tableName, new Dictionary<string, AttributeValue>
            {
                {FieldMappings.PartitionKey, new AttributeValue(photoId.ToDbValue())},
                {FieldMappings.SortKey, new AttributeValue(photoId.ToDbValue())}
            });

            var photo = await dynamoDbCore.GetItem(request, Mappers.PhotoModel);

            if (photo == null)
            {
                logWriter.LogInformation($"No photo found for id '{photoId.ToDbValue()}'");
                return PhotoState.Undefined;
            }

            return photo.State;

        }





        public async Task SetPhotoState(UserId userId, PhotoId photoId, PhotoState photoState)
        {
            logWriter.LogInformation($"{nameof(SetPhotoState)}({nameof(userId)} = '{userId}', {nameof(photoId)} = '{photoId}', {nameof(photoState)} = '{photoState}'");

            var request = new UpdateItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    {FieldMappings.PartitionKey, new AttributeValue(photoId.ToDbValue())},
                    {FieldMappings.SortKey, new AttributeValue(photoId.ToDbValue())}
                },
                UpdateExpression = $"SET {FieldMappings.Photo.State} = :newstate",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {
                        ":newstate", new AttributeValue
                        {
                            S=photoState.ToString()
                        }
                    }
                }
            };

            await dynamoDbCore.UpdateItem(request);

        }


        public Task UpdatePhoto(PhotoModel photo)
        {
            logWriter.LogInformation($"{nameof(UpdatePhoto)}({nameof(photo.PhotoId)} = '{photo.PhotoId}'");

            IEnumerable<WriteRequest> writeRequests = new[] { GetWritePutRequest(photo, Mappers.PhotoModel) }
                .Union(photo.Hashtags.Select(hashtag => GetWritePutRequest(new HashtagPhoto
                {
                    Hashtag = hashtag,
                    Photo = photo
                }, Mappers.HashtagPhoto)));

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    {
                        tableName,
                        writeRequests.ToList()
                    }
                }
            };

            return dynamoDbCore.BatchWriteItem(request);
        }

        private WriteRequest GetWritePutRequest<TItem>(TItem item, IValueMapper<TItem> valueMapper)
        {
            return new WriteRequest
            {
                PutRequest = new PutRequest
                {
                    Item = valueMapper.ToDbItem(item)
                }
            };
        }

        public async Task DeletePhoto(PhotoId photoId)
        {
            logWriter.LogInformation($"{nameof(DeletePhoto)}({nameof(photoId)} = '{photoId}'");

            // Tirst we need to get the keys for all items to delete
            IEnumerable<Dictionary<string, AttributeValue>> itemKeys = await GetAllItemKeys(photoId);

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    {
                        tableName,
                        itemKeys.Distinct(new DbKeyEqualityComparer()).Select(key => new WriteRequest{
                            DeleteRequest = new DeleteRequest
                            {
                                Key = key
                            }
                        }).ToList()
                    }
                }
            };

            await dynamoDbCore.BatchWriteItem(request);
        }

        private async Task<IEnumerable<Dictionary<string, AttributeValue>>> GetAllItemKeys(PhotoId photoId)
        {
            logWriter.LogInformation($"{nameof(GetAllItemKeys)}({nameof(photoId)} = '{photoId}')");

            var photo = await GetPhotoById(photoId, Guid.Empty);

            UserId userId = photo.UserId;

            QueryRequest requestForPhotoWithCommentsAndLikes = new QueryRequest(tableName)
            {
                KeyConditionExpression = $"{FieldMappings.PartitionKey} = :pkvalue",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            {":pkvalue", new AttributeValue(photoId.ToDbValue())}
                        }
            };

            // get all items related to the photo (mainly hashtags)
            QueryRequest requestForItemsRelatedToPhoto = new QueryRequest(tableName)
            {
                IndexName = "GSI1",
                KeyConditionExpression = $"{FieldMappings.Gsi1PartitionKey} = :userId AND {FieldMappings.SortKey} = :photoId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            {":userId", new AttributeValue(userId.ToDbValue())},
                            {":photoId", new AttributeValue(photoId.ToDbValue())}
                        }
            };

            var queryTasks = new[]{
                dynamoDbCore.Query(requestForPhotoWithCommentsAndLikes, Mappers.Noop),
                dynamoDbCore.Query(requestForItemsRelatedToPhoto, Mappers.Noop),
            };

            await Task.WhenAll(queryTasks);

            var comparer = EqualityComparer<Dictionary<string, AttributeValue>>.Default;
            return queryTasks
                // flatten into one sequence of dictionaries
                .SelectMany(x => x.Result)
                // strip all values except main partition- and sort key
                .Select(x => x.Where(y => y.Key == FieldMappings.PartitionKey || y.Key == FieldMappings.SortKey).ToDictionary(y => y.Key, y => y.Value))
                // filter down to distinct values
                .Distinct(comparer);
        }

        public async Task SetPhotoText(UserId userId, PhotoId photoId, string text, IEnumerable<HashtagModel> hashtags)
        {
            logWriter.LogInformation($"{nameof(SetPhotoText)}({nameof(userId)} = '{userId}', {nameof(photoId)} = '{photoId}', {nameof(text)} = '{text}')");

            var existingItem = await GetPhotoById(photoId, userId);
            BatchWriteItemRequest writeItemsRequest = GetHashtagsRequests(text, hashtags, existingItem, HashtagUpdateMode.Update);

            var request = GetUpdateTextRequest(new PhotoModel
            {
                PhotoId = photoId,
                UserId = userId,
                RawText = text,
                Hashtags = hashtags
            });

            logWriter.LogInformation($"UpdateItemRequest:\n{JsonConvert.SerializeObject(request)}");
            logWriter.LogInformation($"BatchWriteItemRequest:\n{JsonConvert.SerializeObject(writeItemsRequest)}");

            await dynamoDbCore.UpdateItem(request);
            if (writeItemsRequest != null)
            {
                await dynamoDbCore.BatchWriteItem(writeItemsRequest);
            }
        }

        private BatchWriteItemRequest GetHashtagsRequests(string text, IEnumerable<HashtagModel> hashtags, PhotoModel existingItem, HashtagUpdateMode updateMode)
        {
            IEnumerable<HashtagModel> hashtagsBefore = updateMode == HashtagUpdateMode.Update
                ? existingItem.Hashtags
                : emptyHashtagSequence;

            existingItem.RawText = text;
            existingItem.Hashtags = hashtags;

            BatchWriteItemRequest writeItemsRequest = GetHashtagsWriteRequests(hashtags, existingItem, hashtagsBefore, updateMode);
            return writeItemsRequest;
        }

        private BatchWriteItemRequest GetHashtagsWriteRequests(
            IEnumerable<HashtagModel> hashtags,
            PhotoModel existingItem,
            IEnumerable<HashtagModel> hashtagsBefore,
            HashtagUpdateMode updateMode)
        {

            var hashtagsToRemove = updateMode == HashtagUpdateMode.Update
                ? GetHashTagsToRemove(hashtagsBefore, hashtags)
                : emptyHashtagSequence;

            var hashtagsToAdd = GetHashTagsToAdd(hashtagsBefore, hashtags).Select(x => new HashtagPhoto { Hashtag = x, Photo = existingItem });

            logWriter.LogInformation($"Existing hashtags:\n{JsonConvert.SerializeObject(existingItem?.Hashtags.ToArray() ?? emptyHashtagSequence)}\nIncoming hashtags:\n{JsonConvert.SerializeObject(hashtags.ToArray())}\nHashtags to add:\n{JsonConvert.SerializeObject(hashtagsToAdd.ToArray())}\nHashtags to delete:\n{JsonConvert.SerializeObject(hashtagsToRemove.ToArray())}");

            var hashTagRemoveRequests = hashtagsToRemove.Select(x => new WriteRequest
            {
                DeleteRequest = new DeleteRequest
                {
                    Key = Mappers.Hashtag.ToDbKey(x),
                }
            });


            var hashTagAddRequests = hashtagsToAdd.Select(x => new WriteRequest
            {
                PutRequest = new PutRequest
                {
                    Item = Mappers.HashtagPhoto.ToDbItem(x)
                }
            });

            var writeRequests = hashTagRemoveRequests.Union(hashTagAddRequests);

            return writeRequests.Any()
                ? new BatchWriteItemRequest { RequestItems = new Dictionary<string, List<WriteRequest>> { { tableName, writeRequests.ToList() } } }
                : null;
        }

        private UpdateItemRequest GetUpdateTextRequest(PhotoModel updateItem)
        {
            Dictionary<string, AttributeValue> expressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":newtext", new AttributeValue{ S=updateItem.RawText }},
                    {":userid", new AttributeValue { S=updateItem.UserId.ToDbValue() }},
                    {":newState", new AttributeValue{S=PhotoState.PhotoAvailable.ToString()}}
                };

            var updateExpression = $"SET {FieldMappings.Photo.RawText} = :newtext, {FieldMappings.Photo.State} = :newState";
            if (updateItem.Hashtags.Any())
            {
                updateExpression += $", {FieldMappings.Photo.Hashtags} = :hashtags";
                expressionAttributeValues.Add(":hashtags", new AttributeValue
                {
                    L = updateItem.Hashtags.Select(hashtag => new AttributeValue
                    {
                        S = hashtag.Hashtag
                    }).ToList()
                });
            }
            else
            {
                updateExpression += $" REMOVE {FieldMappings.Photo.Hashtags}";
            }


            return new UpdateItemRequest()
            {
                TableName = tableName,
                Key = Mappers.PhotoModel.ToDbKey(updateItem),
                UpdateExpression = updateExpression,

                // update only if the call is made by the user who created the photo
                ConditionExpression = $"{FieldMappings.Photo.UserId} = :userid",
                ExpressionAttributeValues = expressionAttributeValues,
            };
        }

        private IEnumerable<HashtagModel> GetHashTagsToRemove(IEnumerable<HashtagModel> oldHashtags, IEnumerable<HashtagModel> newHashtags)
        {
            return oldHashtags.Except(newHashtags);
        }

        private IEnumerable<HashtagModel> GetHashTagsToAdd(IEnumerable<HashtagModel> oldHashtags, IEnumerable<HashtagModel> newHashtags)
        {
            return newHashtags.Except(oldHashtags);
        }



        public async Task<IEnumerable<PhotoModel>> GetPhotosByUser(UserId userId, UserId currentUserId)
        {
            var request = new QueryRequest
            {
                TableName = tableName,
                IndexName = "GSI1",
                KeyConditionExpression = $"{FieldMappings.Gsi1PartitionKey} = :userId and begins_with({FieldMappings.SortKey}, :photoIdPrefix)",
                FilterExpression = $"begins_with({FieldMappings.PartitionKey}, :photoIdPrefix) and {FieldMappings.Photo.State} = :state",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":userId", new AttributeValue{S=userId.ToDbValue()}},
                    {":state", new AttributeValue{S=PhotoState.PhotoAvailable.ToString()}},
                    {":photoIdPrefix", new AttributeValue{S=$"{PhotoId.Prefix}|"}}
                }
            };


            var response = await dynamoDbCore.Query(request, Mappers.Noop);

            var result = new ConcurrentDictionary<PhotoId, PhotoModel>();
            foreach (var photo in response
                .Where(x => x[FieldMappings.RecordType].S == "photo")
                .Select(Mappers.PhotoModel.FromDbItem))
            {
                result.TryAdd(photo.PhotoId, photo);
            }

            await AddLikeDataForUser(currentUserId, result);
            return result.Values;
        }

        public async Task<IEnumerable<PhotoWithCommentsAndLikes>> GetPhotosWithCommentsAndLikesForScoring(DateTimeOffset dateTimeOffset)
        {
            List<PhotoWithCommentsAndLikes> result = new List<PhotoWithCommentsAndLikes>();
            try
            {
                logWriter.LogInformation($"{nameof(GetPhotosWithCommentsAndLikesForScoring)}({nameof(dateTimeOffset)} = '{dateTimeOffset.ToString(Constants.DateTimeFormatWithMilliseconds)}')");

                PhotoId[] touchedPhotoIds = (await GetTouchedPhotoIds(dateTimeOffset)).ToArray();

                PhotoId[] nextBatch;
                int currentPosition = 0;
                while ((nextBatch = touchedPhotoIds.Skip(currentPosition).Take(70).ToArray()).Any())
                {
                    currentPosition += nextBatch.Length;

                    // Create a dictionary of expression filter values (:photoId0 = id0, :photoId1 = id1, ...).
                    Dictionary<string, AttributeValue> expressionFilterValues = new Dictionary<string, AttributeValue>();
                    for (int i = 0; i < nextBatch.Length; i++)
                    {
                        expressionFilterValues.Add($":photoId{i}", new AttributeValue(nextBatch[i].ToDbValue()));
                    }

                    // Create the filter expression
                    string filterExpression = $"{FieldMappings.PartitionKey} IN ({string.Join(", ", expressionFilterValues.Keys)})";

                    ScanRequest query = new ScanRequest(tableName)
                    {
                        FilterExpression = filterExpression,
                        ExpressionAttributeValues = expressionFilterValues,
                        ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES
                    };

                    logWriter.LogInformation($"Querying.\nFilter expression: {filterExpression}\nExpression values:\n{JsonConvert.SerializeObject(expressionFilterValues)}");


                    var response = await dynamoDbCore.Scan(query, Mappers.Noop);

                    // Sort the result set on the partition key (the photo id), so that all records relating to the
                    // same photo comes in sequence.
                    var sortedResult = response.OrderBy(item => item[FieldMappings.PartitionKey].S);
                    result.AddRange(ParsePhotosWithCommentsAndLikes(sortedResult));

                }
                return result;

            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(GetPhotosWithCommentsAndLikesForScoring)}({nameof(dateTimeOffset)} = '{dateTimeOffset.ToString(Constants.DateTimeFormatWithMilliseconds)}'):\n{ex.ToString()}");
                throw;
            }

        }

        private IEnumerable<PhotoWithCommentsAndLikes> ParsePhotosWithCommentsAndLikes(IOrderedEnumerable<Dictionary<string, AttributeValue>> sortedResult)
        {
            PhotoId latestPhotoId = Guid.Empty;
            List<PhotoWithCommentsAndLikes> result = new List<PhotoWithCommentsAndLikes>();

            PhotoWithCommentsAndLikes currentResult = null;
            List<PhotoComment> currentPhotoComments = null;
            List<PhotoLikeRecord> currentPhotoLikes = null;

            foreach (var item in sortedResult)
            {
                var photoId = PhotoId.FromDbValue(item[FieldMappings.PartitionKey].S);
                string recordType = item[FieldMappings.RecordType].S;


                if (latestPhotoId != photoId)
                {
                    if (currentResult != null)
                    {
                        result.Add(currentResult);
                    }

                    currentResult = new PhotoWithCommentsAndLikes();
                    currentResult.Comments = (currentPhotoComments = new List<PhotoComment>());
                    currentResult.Likes = (currentPhotoLikes = new List<PhotoLikeRecord>());

                    latestPhotoId = photoId;
                }

                switch (recordType.ToLowerInvariant())
                {
                    case "photo":
                        currentResult.Photo = Mappers.PhotoModel.FromDbItem(item);
                        break;
                    case "photocomment":
                        currentPhotoComments.Add(Mappers.PhotoComment.FromDbItem(item));
                        break;
                    case "photolike":
                        currentPhotoLikes.Add(Mappers.PhotoLike.FromDbItem(item));
                        break;
                    default:
                        logWriter.LogWarning($"RecordType '{recordType}' was not expected in {nameof(ParsePhotosWithCommentsAndLikes)}");
                        break;
                }
            }

            if (currentResult != null)
            {
                result.Add(currentResult);
            }

            return result;
        }

        private async Task<IEnumerable<PhotoId>> GetTouchedPhotoIds(DateTimeOffset dateTimeOffset)
        {
            logWriter.LogInformation($"{nameof(GetTouchedPhotoIds)}({nameof(dateTimeOffset)} = '{dateTimeOffset.ToString(Constants.DateTimeFormatWithMilliseconds)}')");

            ScanRequest query = new ScanRequest(tableName)
            {
                // 'photolike', 'photocomment', 'photo'
                IndexName = "GSI2",
                FilterExpression = $"(RecordType = :photolike OR RecordType = :photocomment OR RecordType = :photo) AND CreatedTime > :createdTime",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":photolike", new AttributeValue("photolike")},
                    {":photocomment", new AttributeValue("photocomment")},
                    {":photo", new AttributeValue("photo")},
                    {":createdTime", new AttributeValue(dateTimeOffset.ToString(Constants.DateTimeFormatWithMilliseconds))},
                },
                ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES
            };

            var response = await dynamoDbCore.Scan(query, Mappers.Noop);
            return response.Select(x => PhotoId.FromDbValue(x[FieldMappings.PartitionKey].S)).Distinct();
        }

        // private string GetRecordType(Dictionary<string, AttributeValue> dbItem)
        // {
        //     return dbItem[FieldMappings.RecordType].S.ToLowerInvariant();
        // }

        public async Task UpdateScores(IEnumerable<PhotoScore> scores)
        {
            logWriter.LogInformation($"{nameof(UpdateScores)}({scores.Count()} scores)");

            try
            {
                var updateTasks = scores.Select(score =>
                {
                    try
                    {
                        return dynamoDbCore.UpdateItem(GetScoreUpdateRequest(score));
                    }
                    catch (Exception ex)
                    {
                        logWriter.LogError(ex, $"Error in {nameof(UpdateScores)}(updating {nameof(score.PhotoId)} to {score.Score}):\n{ex.ToString()}");
                        throw;
                    }
                });

                await Task.WhenAll(updateTasks);

            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(UpdateScores)}({scores.Count()} scores):\n{ex.ToString()}");
                throw;
            }
        }

        private UpdateItemRequest GetScoreUpdateRequest(PhotoScore score)
        {
            return new UpdateItemRequest()
            {
                TableName = tableName,
                Key = Mappers.PhotoModel.ToDbKey(new PhotoModel { PhotoId = score.PhotoId }),
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#Score", FieldMappings.Photo.Score}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":score", new AttributeValue{N = score.Score.ToString()}}
                },
                UpdateExpression = "SET #Score = :score",

                // make the dynamodb layer log consumed capacity for this request
                ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES
            };
        }

        public async Task<IEnumerable<PhotoModel>> GetPhotosByHashtag(UserId currentUserId, HashtagModel hashtag)
        {

            logWriter.LogInformation($"{nameof(GetPhotosByHashtag)}({nameof(hashtag)} = '{hashtag.Hashtag}')");
            var hashtagId = Mappers.Hashtag.ToDbKey(hashtag)[FieldMappings.Hashtag.HastagId];

            QueryRequest request = new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = $"{FieldMappings.PartitionKey} = :hashtagId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":hashtagId", hashtagId }
                },
                ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES
            };

            var response = await dynamoDbCore.Query(request, Mappers.HashtagPhoto);
            var result = new ConcurrentDictionary<PhotoId, PhotoModel>();
            foreach (var photo in response.Select(x => x.Photo))
            {
                result.TryAdd(photo.PhotoId, photo);
            }

            await AddLikeDataForUser(currentUserId, result);
            return result.Values;

        }

        public async Task<IEnumerable<PhotoModel>> GetPopularPhotos(UserId currentUserId)
        {
            var request = new QueryRequest
            {
                TableName = tableName,
                IndexName = "GSI3",
                KeyConditionExpression = $"{FieldMappings.RecordType} = :recordType and {FieldMappings.Photo.Score} > :lowerScoreLimit",
                ScanIndexForward = false, // order descending
                Limit = 250,
                FilterExpression = $"{FieldMappings.Photo.State} = :state",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":state", new AttributeValue{S=PhotoState.PhotoAvailable.ToString()}},
                    {":recordType", new AttributeValue{S="photo"}},
                    {":lowerScoreLimit", new AttributeValue{N="1"}},
                },
                ReturnConsumedCapacity = ReturnConsumedCapacity.INDEXES
            };

            var result = new ConcurrentDictionary<PhotoId, PhotoModel>();
            foreach (var photo in (await dynamoDbCore.Query(request, Mappers.PhotoModel)))
            {
                result.TryAdd(photo.PhotoId, photo);
            }
            await AddLikeDataForUser(currentUserId, result);

            return result.Values.OrderByDescending(x => x.Score);

        }

        private async Task AddLikeDataForUser(UserId currentUserId, ConcurrentDictionary<PhotoId, PhotoModel> result)
        {
            if (currentUserId != (UserId)Guid.Empty)
            {
                var request = new QueryRequest
                {
                    TableName = tableName,
                    IndexName = "GSI1",
                    KeyConditionExpression = $"{FieldMappings.Gsi1PartitionKey} = :userId and {FieldMappings.SortKey} = :userlike",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":userId", new AttributeValue{S=currentUserId.ToDbValue()}},
                        {":userlike", new AttributeValue{S = $"like|{currentUserId.ToDbValue()}"}}
                    },
                    ProjectionExpression = FieldMappings.PartitionKey

                };

                var likedPhotoIds = (await dynamoDbCore.Query(request, Mappers.Noop)).Select(x => PhotoId.FromDbValue(x[FieldMappings.PartitionKey].S));

                foreach (var photoId in likedPhotoIds)
                {
                    if (result.TryGetValue(photoId, out var photo))
                    {
                        photo.PhotoIsLikedByCurrentUser = true;
                    }
                }

            }
        }
    }
}