using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using demunity.lib;
using demunity.lib.Data.Models;

namespace demunity.aws.Data.Mapping
{
    public class UserModelMapper : IValueMapper<UserModel>
    {
        public UserModel FromDbItem(Dictionary<string, AttributeValue> input)
        {
            var result = new UserModel
            {
                CreatedTime = DateTimeOffset.ParseExact(input[FieldMappings.User.CreatedTime].S, new[] { Constants.DateTimeFormat, Constants.DateTimeFormatWithMilliseconds }, null, DateTimeStyles.None),
                Email = input[FieldMappings.User.Email].S,
                Id = UserId.FromDbValue(input[FieldMappings.User.Id].S),
                Name = input[FieldMappings.User.Name].S
            };

            if (input.TryGetValue(FieldMappings.User.OnlineProfiles, out var onlineProfiles))
            {
                result.OnlineProfiles = onlineProfiles.L.Select(x => new OnlineProfile
                {
                    Profile = x.M["Profile"].S,
                    Type = Enum.TryParse<OnlineProfileType>(x.M["Type"].S, out var profileType) ? profileType : OnlineProfileType.Undefined
                });
            }
            else
            {
                result.OnlineProfiles = new OnlineProfile[] { };
            }

            return result;
        }

        public Dictionary<string, AttributeValue> ToDbItem(UserModel input)
        {
            var result = new Dictionary<string, AttributeValue>
            {
                {FieldMappings.User.CreatedTime, new AttributeValue(input.CreatedTime.ToString(Constants.DateTimeFormatWithMilliseconds))},
                {FieldMappings.User.Email, new AttributeValue(input.Email.ToLowerInvariant())},
                {FieldMappings.User.Id, new AttributeValue(input.Id.ToDbValue())},
                {FieldMappings.User.Name, new AttributeValue(input.Name)},
                {FieldMappings.User.RecordType, new AttributeValue("user")},
                {FieldMappings.RecordType, new AttributeValue("user")},

            };


            if (input.OnlineProfiles?.Any() ?? false)
            {
                var list = input.OnlineProfiles.Select(x => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                        {
                            {"Type", new AttributeValue(x.Type.ToString())},
                            {"Profile", new AttributeValue{S = x.Profile}}
                        }
                }).ToList();

                result.Add(FieldMappings.User.OnlineProfiles, new AttributeValue { L = list });
            }

            return result;
        }

        public Dictionary<string, AttributeValue> ToDbKey(UserModel input)
        {
            return new Dictionary<string, AttributeValue>
            {
                {FieldMappings.User.Email, new AttributeValue(input.Email.ToLowerInvariant())},
                {FieldMappings.User.RecordType, new AttributeValue("user")}
            };
        }
    }
}