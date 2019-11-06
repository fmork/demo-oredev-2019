using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using demunity.lib;
using demunity.lib.Data.Models;

namespace demunity.aws.Data.Mapping
{
    public class PhotoModelMapper : IValueMapper<PhotoModel>
    {
        public PhotoModel FromDbItem(Dictionary<string, AttributeValue> input)
        {
            PhotoId photoId = input.TryGetValue(FieldMappings.Photo.PhotoId, out var photoIdValue)
                    ? PhotoId.FromDbValue(photoIdValue.S)
                    : (PhotoId)Guid.Empty;

            var result = new PhotoModel
            {
                CreatedTime = input.GetDateTimeOffset(FieldMappings.Photo.CreatedTime),
                PhotoId = photoId,
                ObjectKey = input.GetString(FieldMappings.Photo.ObjectKey),
                State = input.GetValue(FieldMappings.Photo.State, value => (PhotoState)Enum.Parse(typeof(PhotoState), value)),
                UserId = input.GetValue(FieldMappings.Photo.UserId, value => UserId.FromDbValue(value)),
                UserName = input.GetString(FieldMappings.Photo.UserName),
                LikeCount = input.GetInt32(FieldMappings.Photo.LikeCount),
                CommentCount = input.GetInt32(FieldMappings.Photo.CommentCount),
                Hashtags = input.GetList(FieldMappings.Photo.Hashtags, value => new HashtagModel { PhotoId = photoId, Hashtag = value })
            };

            if (input.TryGetValue(FieldMappings.Photo.RawText, out var rawCommentValue))
            {
                result.RawText = rawCommentValue.S;
            }

            if (input.TryGetValue(FieldMappings.Photo.Score, out var scoreValue))
            {
                result.Score = double.Parse(scoreValue.N);
            }

            if (input.TryGetValue(FieldMappings.Photo.Sizes, out var sizeValues))
            {
                result.Sizes = sizeValues.L.Select(value =>
                {
                    int width;
                    int height;

                    if (!value.M.TryGetValue("Width", out var widthValue) || !int.TryParse(widthValue.S, out width))
                    {
                        throw new Exception($"Failed to parse '{widthValue.S}' as a Size Width");
                    }

                    if (!value.M.TryGetValue("Height", out var heightValue) || !int.TryParse(heightValue.S, out height))
                    {
                        throw new Exception($"Failed to parse '{heightValue.S}' as a Size Height");
                    }

                    return new Size(width, height);
                });
            }

            return result;

        }

        public Dictionary<string, AttributeValue> ToDbItem(PhotoModel input)
        {

            var result = ToDbKey(input);
            result.Add(FieldMappings.RecordType, new AttributeValue("photo"));

            if (input.UserId != default(UserId))
            {
                result.Add(FieldMappings.Photo.UserId, new AttributeValue(input.UserId.ToDbValue()));
            }

            if (!string.IsNullOrEmpty(input.UserName))
            {
                result.Add(FieldMappings.Photo.UserName, new AttributeValue(input.UserName));
            }

            if (!string.IsNullOrEmpty(input.ObjectKey))
            {
                result.Add(FieldMappings.Photo.ObjectKey, new AttributeValue(input.ObjectKey));
            }

            if (input.CreatedTime != default(DateTimeOffset))
            {
                result.Add(
                    FieldMappings.Photo.CreatedTime,
                    new AttributeValue(
                        input.CreatedTime.ToString(Constants.DateTimeFormatWithMilliseconds)));
            }

            if (input.State != default(PhotoState))
            {
                result.Add(FieldMappings.Photo.State, new AttributeValue(input.State.ToString()));
            }


            // always add the like count, even if it's 0
            result.Add(FieldMappings.Photo.LikeCount, new AttributeValue { N = input.LikeCount.ToString() });
            result.Add(FieldMappings.Photo.CommentCount, new AttributeValue { N = input.CommentCount.ToString() });


            if (input.Score != default(double))
            {
                result.Add(FieldMappings.Photo.Score, new AttributeValue { N = input.Score.ToString() });
            }

            if (!string.IsNullOrEmpty(input.RawText))
            {
                result.Add(FieldMappings.Photo.RawText, new AttributeValue(input.RawText));
            }

            if (input.Sizes.Any())
            {
                result.Add(FieldMappings.Photo.Sizes, new AttributeValue
                {
                    L = input?.Sizes.Select(size => new AttributeValue
                    {
                        M = new Dictionary<string, AttributeValue>{
                            {"Width", new AttributeValue(size.Width.ToString())},
                            {"Height", new AttributeValue(size.Height.ToString())}
                        }
                    }).ToList()
                });
            }

            if (input.Hashtags.Any())
            {
                result.Add(FieldMappings.Photo.Hashtags, new AttributeValue
                {
                    L = input.Hashtags.Select(hashtag => new AttributeValue
                    {
                        S = hashtag.Hashtag
                    }).ToList()
                });
            }

            return result;
        }

        public Dictionary<string, AttributeValue> ToDbKey(PhotoModel input)
        {
            return new Dictionary<string, AttributeValue>
            {
                {FieldMappings.PartitionKey, new AttributeValue(input.PhotoId.ToDbValue())},
                {FieldMappings.SortKey, new AttributeValue(input.PhotoId.ToDbValue())},
            };
        }
    }
}
