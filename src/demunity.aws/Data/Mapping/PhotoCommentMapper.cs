using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.DynamoDBv2.Model;
using demunity.lib;
using demunity.lib.Data.Models;

namespace demunity.aws.Data.Mapping
{
    public class PhotoCommentMapper : IValueMapper<PhotoComment>
    {
        public PhotoComment FromDbItem(Dictionary<string, AttributeValue> input)
        {
            return new PhotoComment
            {
                UserId = input.GetValue(FieldMappings.PhotoComment.UserId, value => UserId.FromDbValue(value)),
                PhotoId = input.GetValue(FieldMappings.PhotoComment.PhotoId, value => PhotoId.FromDbValue(value)),
                UserName = input.GetString(FieldMappings.PhotoComment.UserName),
                CreatedTime = input.GetDateTimeOffset(FieldMappings.PhotoComment.CreatedTime),
                Text = input.GetString(FieldMappings.PhotoComment.Text)
            };
        }

        public Dictionary<string, AttributeValue> ToDbItem(PhotoComment input)
        {
            string createdTimeString = input.CreatedTime.ToString(Constants.DateTimeFormatWithMilliseconds);
            return new Dictionary<string, AttributeValue>{
                { FieldMappings.PartitionKey, new AttributeValue(input.PhotoId.ToDbValue())},
                { FieldMappings.SortKey, new AttributeValue($"comment|{input.UserId.ToDbValue()}|{createdTimeString}")},
                { FieldMappings.Gsi1PartitionKey, new AttributeValue(input.UserId.ToDbValue())},
                { FieldMappings.PhotoComment.UserName, new AttributeValue(input.UserName)},
                { FieldMappings.PhotoComment.CreatedTime, new AttributeValue(createdTimeString)},
                { FieldMappings.PhotoComment.Text, new AttributeValue(input.Text)},
                {"RecordType", new AttributeValue("photocomment")}
            };
        }

        public Dictionary<string, AttributeValue> ToDbKey(PhotoComment input)
        {
            string createdTimeString = input.CreatedTime.ToString(Constants.DateTimeFormatWithMilliseconds);
            return new Dictionary<string, AttributeValue>
            {
                { FieldMappings.PartitionKey, new AttributeValue(input.PhotoId.ToDbValue())},
                { FieldMappings.SortKey, new AttributeValue($"comment|{input.UserId.ToDbValue()}|{createdTimeString}")}
            };
        }
    }
}