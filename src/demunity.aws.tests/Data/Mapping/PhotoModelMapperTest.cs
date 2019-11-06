using System;
using System.Collections.Generic;
using System.Drawing;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data;
using demunity.aws.Data.Mapping;
using demunity.lib;
using demunity.lib.Data.Models;
using Shouldly;
using Xunit;

namespace demunity.aws.tests.Data.Mapping
{
    public class PhotoModelMapperTest
    {
        private static readonly PhotoId photoId = new Guid("169c683f-b035-4ab6-8f54-2067ce1c2671");
        private static readonly DateTimeOffset createdTime = new DateTimeOffset(2019, 9, 18, 8, 11, 59, 362, TimeSpan.Zero);
        private static readonly int likeCount = 3;
        private static readonly IEnumerable<Size> sizes = new[]{
                new Size(100,50),
                new Size(200,100)
            };

        private static readonly IEnumerable<HashtagModel> hashtags = new[]{
            new HashtagModel { PhotoId = photoId, Hashtag = "hashtag1" },
            new HashtagModel { PhotoId = photoId, Hashtag = "hashtag2" }
        };

        private static readonly string rawText = "This is the text";
        private static readonly UserId userId = new Guid("c80bbd87-d302-4bb5-8c9b-fabafd09440a");
        private static readonly string objectKey = "object/key";
        private static readonly string userName = "Fredrik Mörk";


        [Fact]
        public void ToDbItem_FullyPopulated_ExpectedItemCount()
        {
            PhotoModel input = GetFullyPopulatedPhotoModel();

            var dbItem = Mappers.PhotoModel.ToDbItem(input);

            dbItem.Count.ShouldBe(14);
        }



        [Fact]
        public void ToDbItem_WithoutSizeAndText_ExpectedItemCount()
        {
            PhotoModel input = GetPhotoModelWithoutSizesOrText();

            var dbItem = Mappers.PhotoModel.ToDbItem(input);

            dbItem.Count.ShouldBe(7);
        }



        [Fact]
        public void ToDbItem_FullyPopulated_ExpectedItemContent()
        {
            PhotoModel input = GetFullyPopulatedPhotoModel();

            var dbItem = Mappers.PhotoModel.ToDbItem(input);

            dbItem[FieldMappings.Photo.CreatedTime].S.ShouldBe(createdTime.ToString(Constants.DateTimeFormatWithMilliseconds));
            dbItem[FieldMappings.Photo.LikeCount].N.ShouldBe(likeCount.ToString());
            dbItem[FieldMappings.Photo.PhotoId].S.ShouldBe($"photo|{photoId}");
            dbItem[FieldMappings.Photo.UserId].S.ShouldBe($"user|{userId}");
            dbItem[FieldMappings.Photo.UserName].S.ShouldBe(userName);
            dbItem[FieldMappings.Photo.RawText].S.ShouldBe(rawText);
            dbItem[FieldMappings.Photo.ObjectKey].S.ShouldBe(objectKey);

            List<AttributeValue> sizesStructure = dbItem[FieldMappings.Photo.Sizes].L;
            sizesStructure.ShouldContain(value => value.M["Width"].S == "100" && value.M["Height"].S == "50");
            sizesStructure.ShouldContain(value => value.M["Width"].S == "200" && value.M["Height"].S == "100");
        }

        [Fact]
        public void FromDbItem_FullyPopulated_ExpectedContent()
        {
            Dictionary<string, AttributeValue> input = GetFullyPopulatedDbItem();
            PhotoModel result = Mappers.PhotoModel.FromDbItem(input);

            result.CreatedTime.ShouldBe(createdTime);
            result.LikeCount.ShouldBe(likeCount);
            result.ObjectKey.ShouldBe(objectKey);
            result.PhotoId.ShouldBe(photoId);
            result.RawText.ShouldBe(rawText);
            result.Sizes.ShouldBe(sizes);
            result.UserId.ShouldBe(userId);
            result.UserName.ShouldBe(userName);
            result.Hashtags.ShouldContain(x => x.Hashtag == "hashtag1");
            result.Hashtags.ShouldContain(x => x.Hashtag == "hashtag2");
        }


        private static Dictionary<string, AttributeValue> GetFullyPopulatedDbItem()
        {
            return Mappers.PhotoModel.ToDbItem(GetFullyPopulatedPhotoModel());
        }

        private static PhotoModel GetFullyPopulatedPhotoModel()
        {
            return new PhotoModel
            {
                PhotoId = photoId,
                CreatedTime = createdTime,
                LikeCount = likeCount,
                State = PhotoState.PhotoAvailable,
                CommentCount = 0,
                ObjectKey = objectKey,
                Sizes = sizes,
                RawText = rawText,
                UserId = userId,
                UserName = userName,
                Score = 1,
                Hashtags = hashtags
            };
        }

        private static PhotoModel GetPhotoModelWithoutSizesOrText()
        {
            return new PhotoModel
            {
                PhotoId = photoId,
                CreatedTime = createdTime,
                LikeCount = likeCount,
                UserId = userId
            };
        }
    }
}
