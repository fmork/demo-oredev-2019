using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data;
using demunity.aws.Data.Mapping;
using demunity.lib.Data.Models;
using Shouldly;
using Xunit;

namespace demunity.aws.tests.Data.Mapping
{
	public class HashtagModelMapperTest
	{
		[Fact]
		public void ToDbItem_ItemHasExpectedAttributeCount()
		{

			PhotoId photoId = new Guid("d3eaa102-6ce7-4cbe-823d-9ee1d046e1b6");
			string hashtagText = "#hashtag";
			var hashtag = new HashtagModel
			{
				PhotoId = photoId,
				Hashtag = hashtagText
			};

			var dbItem = Mappers.Hashtag.ToDbItem(hashtag);

			dbItem.Count.ShouldBe(3);

		}

		[Fact]
		public void ToDbItem_AttributesHaveExpectedValues()
		{
			PhotoId photoId = new Guid("d3eaa102-6ce7-4cbe-823d-9ee1d046e1b6");
			string hashtagText = "#hashtag";
			DateTimeOffset createdTime = new DateTimeOffset(2019, 9, 29, 8, 4, 19, 32, TimeSpan.Zero);
			var hashtag = new HashtagModel
			{
				PhotoId = photoId,
				Hashtag = hashtagText,
				CreatedTime = createdTime
			};

			var dbItem = Mappers.Hashtag.ToDbItem(hashtag);

			dbItem[FieldMappings.Hashtag.HastagId].S.ShouldBe("hash|hashtag");
			dbItem[FieldMappings.Hashtag.PhotoId].S.ShouldBe(photoId.ToDbValue());
			dbItem[FieldMappings.Hashtag.CreatedTime].S.ShouldBe("2019-09-29T08:04:19,032Z");

		}


		[Fact]
		public void FromDbItem_PropertiesHaveExpectedValues()
		{

            PhotoId expectedPhotoId = new Guid("d3eaa102-6ce7-4cbe-823d-9ee1d046e1b6");
			DateTimeOffset expectedCreatedTime = new DateTimeOffset(2019, 9, 29, 8, 4, 19, 32, TimeSpan.Zero);

			var input = new Dictionary<string, AttributeValue>
			{
				{FieldMappings.Hashtag.HastagId, new AttributeValue("hash|hashtag")},
				{FieldMappings.Hashtag.PhotoId, new AttributeValue("photo|d3eaa102-6ce7-4cbe-823d-9ee1d046e1b6")},
				{FieldMappings.Hashtag.CreatedTime, new AttributeValue("2019-09-29T08:04:19,032Z")},
			};

            var hashtagModel = Mappers.Hashtag.FromDbItem(input);
            hashtagModel.Hashtag.ShouldBe("#hashtag");
            hashtagModel.PhotoId.ShouldBe(expectedPhotoId);
            hashtagModel.CreatedTime.ShouldBe(expectedCreatedTime);
		}
	}
}