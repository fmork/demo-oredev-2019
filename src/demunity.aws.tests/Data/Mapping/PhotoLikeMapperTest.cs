using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data;
using demunity.aws.Data.Mapping;
using demunity.lib;
using demunity.lib.Data.Models;
using Shouldly;
using Xunit;

namespace demunity.aws.tests.Data.Mapping
{
    public class PhotoLikeMapperTest
    {
        private static readonly PhotoId photoId = new Guid("169c683f-b035-4ab6-8f54-2067ce1c2671");
        private static readonly DateTimeOffset createdTime = new DateTimeOffset(2019, 9, 18, 8, 11, 59, 362, TimeSpan.Zero);
        private static readonly UserId userId = new Guid("c80bbd87-d302-4bb5-8c9b-fabafd09440a");



        [Fact]
        public void ToDbItem_ExpectedItemCount()
        {
            Mappers.PhotoLike.ToDbItem(GetPopulatedPhotoLikeRecord()).Count.ShouldBe(5);
        }



        [Fact]
        public void ToDbItem_ExpectedItemContent()
        {
            Dictionary<string, AttributeValue> dbItem = Mappers.PhotoLike.ToDbItem(GetPopulatedPhotoLikeRecord());

            dbItem[FieldMappings.PhotoLike.CreatedTime].S.ShouldBe(createdTime.ToString(Constants.DateTimeFormatWithMilliseconds));
            dbItem[FieldMappings.PhotoLike.PhotoId].S.ShouldBe($"photo|{photoId}");
            dbItem[FieldMappings.PhotoLike.UserId].S.ShouldBe($"user|{userId}");

            dbItem[FieldMappings.PartitionKey].S.ShouldBe($"photo|{photoId}");
            dbItem[FieldMappings.SortKey].S.ShouldBe($"like|user|{userId}");
        }

        [Fact]
        public void FromDbItem_FullyPopulated_ExpectedContent()
        {
            Dictionary<string, AttributeValue> input = GetPopulatedDbItem();
            PhotoLikeRecord result = Mappers.PhotoLike.FromDbItem(input);

            result.CreatedTime.ShouldBe(createdTime);
            result.PhotoId.ShouldBe(photoId);
            result.UserId.ShouldBe(userId);
        }


        private static Dictionary<string, AttributeValue> GetPopulatedDbItem()
        {
            return Mappers.PhotoLike.ToDbItem(GetPopulatedPhotoLikeRecord());
        }

        private static PhotoLikeRecord GetPopulatedPhotoLikeRecord()
        {
            return new PhotoLikeRecord
            {
                PhotoId = photoId,
                UserId = userId,
                CreatedTime = createdTime
            };
        }
    }
}
