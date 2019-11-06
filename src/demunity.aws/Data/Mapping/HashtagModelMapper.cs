using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.DynamoDBv2.Model;
using demunity.lib;
using demunity.lib.Data.Models;

namespace demunity.aws.Data.Mapping
{
    public class HashtagModelMapper : IValueMapper<HashtagModel>
    {
        public HashtagModel FromDbItem(Dictionary<string, AttributeValue> input)
        {
            return new HashtagModel
            {
                Hashtag = input.GetValue(FieldMappings.Hashtag.HastagId, value => $"#{value.Split('|')[1]}"),
                PhotoId = input.GetValue(FieldMappings.Hashtag.PhotoId, value => PhotoId.FromDbValue(value)),
                CreatedTime = input.GetDateTimeOffset(FieldMappings.Hashtag.CreatedTime)
            };
        }

        public Dictionary<string, AttributeValue> ToDbItem(HashtagModel input)
        {
            var result = ToDbKey(input);
            result.Add(FieldMappings.Hashtag.CreatedTime, new AttributeValue(input.CreatedTime.ToString(Constants.DateTimeFormatWithMilliseconds)));
            return result;
        }

        public Dictionary<string, AttributeValue> ToDbKey(HashtagModel input)
        {
            return new Dictionary<string, AttributeValue>
            {
                {FieldMappings.Hashtag.HastagId, new AttributeValue($"hash|{input.Hashtag.Substring(1)}".ToLower())},
                {FieldMappings.Hashtag.PhotoId, new AttributeValue(input.PhotoId.ToDbValue())},
            };
        }
    }
}