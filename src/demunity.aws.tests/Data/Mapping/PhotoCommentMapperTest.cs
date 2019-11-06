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
    public class PhotoCommentMapperTest
    {

        private static readonly PhotoId photoId = new Guid("258bf9e4-0f0f-4453-8d5f-3fc6afc0b7bf");
        private static readonly UserId userId = new Guid("c9fad63f-6817-4a35-abf6-059bebcda772");
        private static readonly string userName = "Fredrik Mörk";
        private static readonly string text = "Comment text";
        private static readonly DateTimeOffset createdTime = new DateTimeOffset(2019, 9, 21, 9, 21, 38, 723, TimeSpan.Zero);

        [Fact]
        public void ToDbItem_ExpectedItemCount()
        {
            Mappers.PhotoComment.ToDbItem(GetPopulatedPhotoCommentRecord()).Count.ShouldBe(7);
        }



        [Fact]
        public void ToDbItem_ExpectedItemContent()
        {
            Dictionary<string, AttributeValue> dbItem = Mappers.PhotoComment.ToDbItem(GetPopulatedPhotoCommentRecord());

            string expectedCreatedTimeString = createdTime.ToString(Constants.DateTimeFormatWithMilliseconds);
            dbItem[FieldMappings.PhotoComment.CreatedTime].S.ShouldBe(expectedCreatedTimeString);
            dbItem[FieldMappings.PhotoComment.PhotoId].S.ShouldBe($"photo|{photoId}");
            dbItem[FieldMappings.PhotoComment.UserId].S.ShouldBe($"user|{userId}");
            dbItem[FieldMappings.PhotoComment.UserName].S.ShouldBe(userName);

            dbItem[FieldMappings.PartitionKey].S.ShouldBe($"photo|{photoId}");
            dbItem[FieldMappings.SortKey].S.ShouldBe($"comment|user|{userId}|{expectedCreatedTimeString}");
        }

        [Fact]
        public void FromDbItem_FullyPopulated_ExpectedContent()
        {
            Dictionary<string, AttributeValue> input = GetPopulatedDbItem();
            PhotoComment result = Mappers.PhotoComment.FromDbItem(input);

            result.CreatedTime.ShouldBe(createdTime);
            result.PhotoId.ShouldBe(photoId);
            result.UserId.ShouldBe(userId);
			result.UserName.ShouldBe(userName);
            result.Text.ShouldBe(text);
        }



        private static PhotoComment GetPopulatedPhotoCommentRecord()
        {
            return new PhotoComment
            {
                PhotoId = photoId,
                UserId = userId,
                UserName = userName,
                Text = text,
                CreatedTime = createdTime
            };
        }

        private static Dictionary<string, AttributeValue> GetPopulatedDbItem()
        {
            return Mappers.PhotoComment.ToDbItem(GetPopulatedPhotoCommentRecord());
        }

    }
}