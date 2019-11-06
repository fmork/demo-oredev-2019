using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using demunity.lib.Data.Models;

namespace demunity.aws.Data.Mapping
{
	public class HashtagPhotoMapper : IValueMapper<HashtagPhoto>
	{
		public HashtagPhoto FromDbItem(Dictionary<string, AttributeValue> input)
		{
			return new HashtagPhoto
			{
				Hashtag = Mappers.Hashtag.FromDbItem(input),
				Photo = Mappers.PhotoModel.FromDbItem(input)
			};
		}

		public Dictionary<string, AttributeValue> ToDbItem(HashtagPhoto input)
		{
			var photoItem = Mappers.PhotoModel.ToDbItem(input.Photo);

			var hashtagItem = Mappers.Hashtag.ToDbItem(input.Hashtag);

			// add all non-key attributes from the photo
			foreach (var item in photoItem.Where(pair => pair.Key != FieldMappings.PartitionKey && pair.Key != FieldMappings.SortKey))
			{
				hashtagItem[item.Key] = item.Value;
			}

			hashtagItem[FieldMappings.RecordType] = new AttributeValue("hashphoto");
			return hashtagItem;

		}

		public Dictionary<string, AttributeValue> ToDbKey(HashtagPhoto input)
		{
			return Mappers.Hashtag.ToDbKey(input.Hashtag);
		}
	}
}