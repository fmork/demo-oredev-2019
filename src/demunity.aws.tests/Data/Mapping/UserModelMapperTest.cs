using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data;
using demunity.aws.Data.Mapping;
using demunity.lib;
using demunity.lib.Data.Models;
using Shouldly;
using Xunit;

namespace demunity.aws.tests.Data.Mapping
{
    public class UserModelMapperTest
    {
        private DateTimeOffset createdTime = DateTimeOffset.UtcNow;
        private string email = "email";
        private UserId userId = Guid.NewGuid();
        private string name = "name";
        private OnlineProfile onlineProfile1 = new OnlineProfile
        {
            Type = OnlineProfileType.Instagram,
            Profile = "photosbyfmork"
        };
        private OnlineProfile onlineProfile2 = new OnlineProfile
        {
            Type = OnlineProfileType.Web,
            Profile = "https://www.fmork.net"
        };

        [Fact]
        public void ToDbModel_FullyPopulated_ExpectedNumberOfAttributes()
        {
            UserModel user = GetFullyPopulatedUser();
            var dbItem = Mappers.UserModel.ToDbItem(user);
            dbItem.Count.ShouldBe(7);
        }

        [Fact]
        public void ToDbModel_UserWithoutOnlineProfiles_ExpectedNumberOfAttributes()
        {
            UserModel user = GetUserWithoutOnlineProfiles();
            var dbItem = Mappers.UserModel.ToDbItem(user);
            dbItem.Count.ShouldBe(6);
        }

        [Fact]
        public void ToDbModel_FullyPopulated_AttributesHasExpectedValues()
        {
            UserModel user = GetFullyPopulatedUser();
            var dbItem = Mappers.UserModel.ToDbItem(user);
            dbItem[FieldMappings.User.CreatedTime].S.ShouldBe(createdTime.ToString(Constants.DateTimeFormatWithMilliseconds));
            dbItem[FieldMappings.User.Email].S.ShouldBe(email);
            dbItem[FieldMappings.User.Id].S.ShouldBe(userId.ToDbValue());
            dbItem[FieldMappings.User.Name].S.ShouldBe(name);
            dbItem[FieldMappings.User.RecordType].S.ShouldBe("user");

            var onlineProfilesAttributeValue = dbItem[FieldMappings.User.OnlineProfiles].L;
            onlineProfilesAttributeValue.Count.ShouldBe(2);
            var onlineProfileItems = onlineProfilesAttributeValue.Select(x => x.M);
            onlineProfileItems.ShouldContain(x => x["Type"].S == "Instagram" && x["Profile"].S == "photosbyfmork");
            onlineProfileItems.ShouldContain(x => x["Type"].S == "Web" && x["Profile"].S == "https://www.fmork.net");

        }

        [Fact]
        public void ToDbKey_FullyPopulated_ItemHasExpectedAttributes()
        {
            UserModel user = GetFullyPopulatedUser();
            var dbItem = Mappers.UserModel.ToDbKey(user);

            dbItem.Count.ShouldBe(2);
            dbItem.ShouldContain(x => x.Key == FieldMappings.PartitionKey);
            dbItem.ShouldContain(x => x.Key == FieldMappings.SortKey);

        }

        [Fact]
        public void FromDbItem_FullyPopulated_UserHasExpectedPropertyValues()
        {
            var dbItem = Mappers.UserModel.ToDbItem(GetFullyPopulatedUser());
            var user = Mappers.UserModel.FromDbItem(dbItem);

            // user.CreatedTime.ShouldBe(createdTime);
            user.Email.ShouldBe(email);
            user.Id.ShouldBe(userId);
            user.Name.ShouldBe(name);
            user.OnlineProfiles.Count().ShouldBe(2);
            user.OnlineProfiles.ShouldContain(x => x.Type == OnlineProfileType.Instagram && x.Profile == "photosbyfmork");
            user.OnlineProfiles.ShouldContain(x => x.Type == OnlineProfileType.Web && x.Profile == "https://www.fmork.net");


        }

        [Fact]
        public void FromDbItem_UserWithoutOnlineProfiles_UserHasExpectedPropertyValues()
        {
            var dbItem = Mappers.UserModel.ToDbItem(GetUserWithoutOnlineProfiles());
            var user = Mappers.UserModel.FromDbItem(dbItem);

            // user.CreatedTime.ShouldBe(createdTime);
            user.Email.ShouldBe(email);
            user.Id.ShouldBe(userId);
            user.Name.ShouldBe(name);
            user.OnlineProfiles.Count().ShouldBe(0);

        }


        private UserModel GetFullyPopulatedUser()
        {
            return new UserModel
            {
                CreatedTime = createdTime,
                Email = email,
                Id = userId,
                Name = name,
                OnlineProfiles = new[]{
                    onlineProfile1,
                    onlineProfile2
                }
            };
        }

        private UserModel GetUserWithoutOnlineProfiles()
        {
            return new UserModel
            {
                CreatedTime = createdTime,
                Email = email,
                Id = userId,
                Name = name
            };
        }
    }
}
