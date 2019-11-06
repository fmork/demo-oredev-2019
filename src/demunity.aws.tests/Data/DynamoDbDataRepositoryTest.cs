using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data;
using demunity.aws.Data.Mapping;
using demunity.lib;
using demunity.lib.Data.Models;
using demunity.lib.Logging;
using demunity.testtools;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace demunity.aws.tests.Data
{
    public class DynamoDbDataRepositoryTest
    {

        private readonly Mock<IDynamoDbCore> dynamoDbCoreMock = new Mock<IDynamoDbCore>();
        private readonly Mock<IScoreCalculator> scoreCalculatorMock = new Mock<IScoreCalculator>();
        private readonly Mock<ISystem> systemMock = new Mock<ISystem>();
        private readonly Mock<ILogWriterFactory> logWriterFactoryMock = new Mock<ILogWriterFactory>();
        private DateTimeOffset referenceTime = new DateTimeOffset(2019, 9, 26, 19, 22, 23, 121, TimeSpan.Zero);
        private const string tableName = "tableName";

        PhotoId photoId1 = Guid.NewGuid();
        PhotoId photoId2 = Guid.NewGuid();
        PhotoId photoId3 = Guid.NewGuid();


        public DynamoDbDataRepositoryTest(ITestOutputHelper outputHelper)
        {
            SetupLogWriterFactoryMock(outputHelper);
            SetupEnvironmentMock();
            SetupDbCoreMock();
        }

        private void SetupDbCoreMock()
        {
            dynamoDbCoreMock
                .Setup(x => x.Scan(It.IsAny<ScanRequest>(), It.IsAny<IValueMapper<Dictionary<string, AttributeValue>>>()))
                .Returns(() => Task.FromResult(GetExpectedScanDataset()));
        }

        [Fact]
        public async Task GetPhotosWithCommentsAndLikes_PassesExpectedQueryParametersAsync()
        {
            // Mock<IAmazonDynamoDB> dbClientMock = GetMockedDbClient();

            List<ScanRequest> observerdScanRequests = new List<ScanRequest>();

            dynamoDbCoreMock
                .Setup(x => x.Scan(It.IsAny<ScanRequest>(), It.IsAny<IValueMapper<Dictionary<string, AttributeValue>>>()))
                .Callback<ScanRequest, IValueMapper<Dictionary<string, AttributeValue>>>((query, valueMapper) => observerdScanRequests.Add(query))
                .Returns<ScanRequest, IValueMapper<Dictionary<string, AttributeValue>>>((query, valueMapper) => Task.FromResult(GetExpectedScanDataset()));

            //Given
            var repository = new DynamoDbPhotoRepository(scoreCalculatorMock.Object, dynamoDbCoreMock.Object, systemMock.Object, logWriterFactoryMock.Object);

            //When
            var items = await repository.GetPhotosWithCommentsAndLikesForScoring(referenceTime);

            //Then

            observerdScanRequests.Count.ShouldBe(2);
            observerdScanRequests[0].TableName.ShouldBe(tableName);
            observerdScanRequests[0].IndexName.ShouldBe("GSI2"); // from GetTouchedPhotoIds

            observerdScanRequests[1].IndexName.ShouldBeNull();
            observerdScanRequests[1].FilterExpression.ShouldNotBeEmpty();
        }

        private static List<Dictionary<string, AttributeValue>> GetPhotoIdItem()
        {
            return new List<Dictionary<string, AttributeValue>>
            {
                new Dictionary<string, AttributeValue>
                {
                    {FieldMappings.PartitionKey, new AttributeValue(((PhotoId)new Guid("88c95173-d51b-45f5-ab09-31aa96c04f39")).ToDbValue())}
                }
            };
        }

        [Fact]
        public async Task GetPhotosWithCommentsAndLikes_ParsesResponseCorrectlyAsync()
        {

            var repository = new DynamoDbPhotoRepository(scoreCalculatorMock.Object, dynamoDbCoreMock.Object, systemMock.Object, logWriterFactoryMock.Object);

            var items = await repository.GetPhotosWithCommentsAndLikesForScoring(referenceTime);

            items.Count().ShouldBe(3);
            var firstItem = items.First();
            firstItem.Photo.ShouldNotBeNull();
            firstItem.Comments.Count().ShouldBe(4);
            firstItem.Likes.Count().ShouldBe(4);
        }

        [Fact]
        public async Task SetPhotoText_AddAndRemovesHashtags()
        {

            Guid photoId = new Guid("36d8ba29-b39b-4e54-bdb4-c53ab5a89384");
            UserId userId = new Guid("c72e750d-c15f-43bf-be30-a94b5517f03e");
            DateTimeOffset createdTime = DateTimeOffset.UtcNow;

            var photo = new PhotoModel
            {
                PhotoId = photoId,
                UserId = userId,
                Hashtags = new[] {
                    new HashtagModel { PhotoId = photoId, Hashtag = "#hashtag1"},
                    new HashtagModel { PhotoId = photoId, Hashtag = "#hashtag2"}
                },
                State = PhotoState.PhotoAvailable
            };

            var repository = new DynamoDbPhotoRepository(scoreCalculatorMock.Object, dynamoDbCoreMock.Object, systemMock.Object, logWriterFactoryMock.Object);

            dynamoDbCoreMock
                .Setup(x => x.BatchGetItem(It.IsAny<BatchGetItemRequest>()))
                .Returns(() => GetBatchGetItemResponse(photo));

            BatchWriteItemRequest observedBatchRequest = null;
            dynamoDbCoreMock
                .Setup(x => x.BatchWriteItem(It.IsAny<BatchWriteItemRequest>()))
                .Callback<BatchWriteItemRequest>(requests =>
                {
                    observedBatchRequest = requests;
                });

            HashtagModel[] newHashtags = new[]
            {
                new HashtagModel { PhotoId = photo.PhotoId, Hashtag = "#hashtag1", CreatedTime = createdTime },
                new HashtagModel { PhotoId = photo.PhotoId, Hashtag = "#hashtag3", CreatedTime = createdTime }
            };

            await repository.SetPhotoText(photo.UserId, photo.PhotoId, photo.RawText, newHashtags);

            observedBatchRequest.ShouldNotBeNull();
            var requestItems = observedBatchRequest.RequestItems[tableName];
            requestItems.Count.ShouldBe(2);
            var deleteRequest = requestItems.First(x => x.DeleteRequest != null);
            var putRequest = requestItems.First(x => x.PutRequest != null);

            // verify that hashtag2 is removed
            deleteRequest.DeleteRequest.Key[FieldMappings.Hashtag.HastagId].S.ShouldBe("hash|hashtag2");
            deleteRequest.DeleteRequest.Key[FieldMappings.Hashtag.PhotoId].S.ShouldBe(photo.PhotoId.ToDbValue());

            // verify that hashtag3 is added
            putRequest.PutRequest.Item[FieldMappings.Hashtag.HastagId].S.ShouldBe("hash|hashtag3");
            putRequest.PutRequest.Item[FieldMappings.Hashtag.PhotoId].S.ShouldBe(photo.PhotoId.ToDbValue());
            putRequest.PutRequest.Item[FieldMappings.Hashtag.CreatedTime].S.ShouldBe(createdTime.ToString(Constants.DateTimeFormatWithMilliseconds));

        }

        [Fact]
        public async Task SetPhotoText_NoHashtagChange_DoesNotCallBatchWriteItems()
        {
            Guid photoId = new Guid("36d8ba29-b39b-4e54-bdb4-c53ab5a89384");
            UserId userId = new Guid("c72e750d-c15f-43bf-be30-a94b5517f03e");
            DateTimeOffset createdTime = DateTimeOffset.UtcNow;

            var photo = new PhotoModel
            {
                PhotoId = photoId,
                UserId = userId,
                Hashtags = new[] {
                    new HashtagModel { PhotoId = photoId, Hashtag = "#hashtag1"},
                    new HashtagModel { PhotoId = photoId, Hashtag = "#hashtag2"}
                },
                State = PhotoState.PhotoAvailable
            };

            var repository = new DynamoDbPhotoRepository(scoreCalculatorMock.Object, dynamoDbCoreMock.Object, systemMock.Object, logWriterFactoryMock.Object);

            dynamoDbCoreMock
                .Setup(x => x.BatchGetItem(It.IsAny<BatchGetItemRequest>()))
                .Returns(() => GetBatchGetItemResponse(photo));

            bool batchWriteItemWasCalled = false;
            dynamoDbCoreMock
                .Setup(x => x.BatchWriteItem(It.IsAny<BatchWriteItemRequest>()))
                .Callback<BatchWriteItemRequest>(requests =>
                {
                    batchWriteItemWasCalled = true;
                });

            HashtagModel[] newHashtags = new[]
            {
                new HashtagModel { PhotoId = photo.PhotoId, Hashtag = "#hashtag1", CreatedTime = createdTime },
                new HashtagModel { PhotoId = photo.PhotoId, Hashtag = "#hashtag2", CreatedTime = createdTime }
            };

            await repository.SetPhotoText(photo.UserId, photo.PhotoId, photo.RawText, newHashtags);

            batchWriteItemWasCalled.ShouldBeFalse();
        }

        // [Fact]
        // public async Task DeletePhoto_FiltersOutDuplicateKeysAsync()
        // {
        //     var dbClientMock = GetMockedDbClient();
        //     dbClientMock
        //         .Setup(x => x.BatchGetItemAsync(It.IsAny<Dictionary<string, KeysAndAttributes>>(), It.IsAny<CancellationToken>()))
        //         .Returns(() => GetBatchGetItemResponse(Mappers.PhotoModel.FromDbItem(GetDataset().First(x => x.Contains(new KeyValuePair<string, AttributeValue>("RecordType", new AttributeValue("photo")))))));

        //     var repository = new DynamoDbDataRepository(clientFactoryMock.Object, dynamoDbCoreMock.Object, systemMock.Object, logWriterFactoryMock.Object);

        //     await repository.DeletePhoto(photoId1);

        // }


        private static Task<Dictionary<string, List<Dictionary<string, AttributeValue>>>> GetBatchGetItemResponse(PhotoModel photo)
        {
            return Task.FromResult(new Dictionary<string, List<Dictionary<string, AttributeValue>>>
                    {
                        {
                            "table",
                            new List<Dictionary<string, AttributeValue>>
                            {
                                Mappers.PhotoModel.ToDbItem(photo)
                            }
                        }
                    });
        }

        private void SetupEnvironmentMock()
        {
            Mock<IEnvironment> environmentMock = new Mock<IEnvironment>();
            environmentMock
                .Setup(x => x.GetVariable(Constants.EnvironmentVariables.DynamoDbTableName))
                .Returns(tableName);

            systemMock
                .Setup(x => x.Environment)
                .Returns(environmentMock.Object);
        }


        private void SetupLogWriterFactoryMock(ITestOutputHelper outputHelper)
        {
            logWriterFactoryMock
                .Setup(x => x.CreateLogger<DynamoDbPhotoRepository>())
                .Returns(() => new TestOutputHelperLogger<DynamoDbPhotoRepository>(outputHelper));
        }


        // private Mock<IAmazonDynamoDB> GetMockedDbClient()
        // {
        //     Mock<IAmazonDynamoDB> dbClientMock = new Mock<IAmazonDynamoDB>();


        //     dbClientMock
        //         .Setup(x => x.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
        //         .Returns(() => Task.FromResult(GetExpectedScanDataset()));

        //     dbClientMock
        //         .Setup(x => x.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
        //         .Returns(() => Task.FromResult(GetExpectedQueryDataset()));

        //     clientFactoryMock
        //         .Setup(x => x.GetClient(It.IsAny<AwsRegion>()))
        //         .Returns(() => dbClientMock.Object);

        //     return dbClientMock;
        // }

        private IEnumerable<Dictionary<string, AttributeValue>> GetExpectedScanDataset()
        {
            return GetDataset();
        }

        private QueryResponse GetExpectedQueryDataset()
        {
            List<Dictionary<string, AttributeValue>> result = GetDataset();

            return new QueryResponse
            {
                Items = result
            };
        }

        private List<Dictionary<string, AttributeValue>> GetDataset()
        {
            PhotoModel photo1 = GetPhoto(photoId1);
            PhotoModel photo2 = GetPhoto(photoId2);
            PhotoModel photo3 = GetPhoto(photoId3);

            List<Dictionary<string, AttributeValue>> result = new List<Dictionary<string, AttributeValue>>();
            result.Add(Mappers.PhotoModel.ToDbItem(photo1));
            result.Add(Mappers.PhotoModel.ToDbItem(photo2));
            result.Add(Mappers.PhotoModel.ToDbItem(photo3));

            result.AddRange(GetComments(photo1).Select(Mappers.PhotoComment.ToDbItem));
            result.AddRange(GetLikes(photo1).Select(Mappers.PhotoLike.ToDbItem));

            result.AddRange(GetComments(photo2).Select(Mappers.PhotoComment.ToDbItem));
            result.AddRange(GetLikes(photo2).Select(Mappers.PhotoLike.ToDbItem));

            result.AddRange(GetComments(photo3).Select(Mappers.PhotoComment.ToDbItem));
            result.AddRange(GetLikes(photo3).Select(Mappers.PhotoLike.ToDbItem));
            return result;
        }

        private PhotoModel GetPhoto(Guid id)
        {
            return new PhotoModel
            {
                PhotoId = id,
                CreatedTime = referenceTime,
                Filename = "filename",
                LikeCount = 0,
                ObjectKey = "object key",
                RawText = "RawText",
                Sizes = new Size[] { },
                State = PhotoState.PhotoAvailable,
                UserId = Guid.NewGuid(),
                UserName = "user name",
                Hashtags = new[]{
                    new HashtagModel
                    {
                        PhotoId = id,
                        CreatedTime = referenceTime,
                        Hashtag = "hashag1"
                    },
                    new HashtagModel
                    {
                        PhotoId = id,
                        CreatedTime = referenceTime,
                        Hashtag = "hashag2"
                    }
                }
            };
        }

        private IEnumerable<PhotoComment> GetComments(PhotoModel photo)
        {
            return Enumerable.Range(0, 4).Select(n => new PhotoComment
            {
                PhotoId = photo.PhotoId,
                CreatedTime = referenceTime,
                UserId = Guid.NewGuid(),
                UserName = "user name",
                Text = $"Comment {n} for photo {photo.PhotoId}"
            });
        }

        private IEnumerable<PhotoLikeRecord> GetLikes(PhotoModel photo)
        {
            return Enumerable.Range(0, 4).Select(n => new PhotoLikeRecord
            {
                PhotoId = photo.PhotoId,
                CreatedTime = referenceTime,
                UserId = Guid.NewGuid()
            });
        }
    }
}