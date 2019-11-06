using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.DynamoDBv2.Model;
using demunity.lib;
using demunity.lib.Data.Models;

namespace demunity.aws.Data.Mapping
{
    public class PhotoLikeMapper : IValueMapper<PhotoLikeRecord>
    {
        public PhotoLikeRecord FromDbItem(Dictionary<string, AttributeValue> input)
        {
            return new PhotoLikeRecord
            {
                UserId = input.GetValue(FieldMappings.PhotoLike.UserId, value => UserId.FromDbValue(value)),
                PhotoId = input.GetValue(FieldMappings.PartitionKey, value => PhotoId.FromDbValue(value)),
                CreatedTime = input.GetDateTimeOffset(FieldMappings.PhotoLike.CreatedTime)
            };
        }

        public Dictionary<string, AttributeValue> ToDbItem(PhotoLikeRecord input)
        {
            return new Dictionary<string, AttributeValue>{
                {FieldMappings.PartitionKey, new AttributeValue(input.PhotoId.ToDbValue())},
                {FieldMappings.SortKey, new AttributeValue($"like|{input.UserId.ToDbValue()}")},
                {FieldMappings.Gsi1PartitionKey, new AttributeValue(input.UserId.ToDbValue())},
                {FieldMappings.Photo.CreatedTime, new AttributeValue(input.CreatedTime.ToString(Constants.DateTimeFormatWithMilliseconds))},
                {"RecordType", new AttributeValue("photolike")}
            };
        }

        public Dictionary<string, AttributeValue> ToDbKey(PhotoLikeRecord input)
        {
            return new Dictionary<string, AttributeValue>
            {
                {FieldMappings.PartitionKey, new AttributeValue(input.PhotoId.ToDbValue())},
                {FieldMappings.SortKey, new AttributeValue($"like|{input.UserId.ToDbValue()}")},
            };
        }
    }
}