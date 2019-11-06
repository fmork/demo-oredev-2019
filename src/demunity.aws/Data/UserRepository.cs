using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data.Mapping;
using demunity.lib;
using demunity.lib.Data;
using demunity.lib.Data.Models;
using demunity.lib.Logging;

namespace demunity.aws.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly string tableName;
        private readonly ILogWriter<UserRepository> logWriter;
        private readonly IDynamoDbCore dynamoDbCore;

        public UserRepository(
            IDynamoDbCore dynamoDbCore,
            ISystem system,
            ILogWriterFactory logWriterFactory)
        {
            if (logWriterFactory is null)
            {
                throw new System.ArgumentNullException(nameof(logWriterFactory));
            }

            if (system is null)
            {
                throw new System.ArgumentNullException(nameof(system));
            }

            tableName = system.Environment.GetVariable(Constants.EnvironmentVariables.DynamoDbTableName);
            logWriter = logWriterFactory.CreateLogger<UserRepository>();
            this.dynamoDbCore = dynamoDbCore ?? throw new System.ArgumentNullException(nameof(dynamoDbCore));
        }

        public async Task<IEnumerable<OnlineProfile>> AddSocialProfile(OnlineProfile onlineProfile, UserId userId)
        {
            var user = await GetUserById(userId);

            var onlineProfiles = user.OnlineProfiles?.ToList() ?? new List<OnlineProfile>();
            var existingProfile = onlineProfiles.FirstOrDefault(x =>
                x.Type == onlineProfile.Type
                && x.Profile.Equals(onlineProfile.Profile, StringComparison.OrdinalIgnoreCase));

            if (existingProfile != null)
            {
                // if the online profile was found, just update it (might be casing differences)
                existingProfile.Profile = onlineProfile.Profile;
            }
            else
            {
                onlineProfiles.Add(onlineProfile);
            }

            user.OnlineProfiles = onlineProfiles;

            UpdateItemRequest request = new UpdateItemRequest
            {
                TableName = tableName,
                Key = Mappers.UserModel.ToDbKey(user),
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#OnlineProfiles", FieldMappings.User.OnlineProfiles}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":OnlineProfiles", Mappers.UserModel.ToDbItem(user)[FieldMappings.User.OnlineProfiles]}
                },
                UpdateExpression = "SET #OnlineProfiles = :OnlineProfiles"
            };

            await dynamoDbCore.UpdateItem(request);

            return user.OnlineProfiles;

        }

        public async Task<IEnumerable<OnlineProfile>> DeleteSocialProfile(OnlineProfile profile, UserId userId)
        {
            logWriter.LogInformation($"{nameof(DeleteSocialProfile)}({nameof(profile.Type)}={profile.Type}, {nameof(profile.Profile)}={profile.Profile}, {nameof(userId)}={userId.Value})");
            var user = await GetUserById(userId);

            var onlineProfiles = user.OnlineProfiles?.ToList() ?? new List<OnlineProfile>();
            int profileIndex = onlineProfiles.FindIndex(x =>
                x.Type == profile.Type
                && x.Profile.Equals(profile.Profile, StringComparison.OrdinalIgnoreCase));
            if (profileIndex >= 0)
            {
                var dbItem = Mappers.UserModel.ToDbItem(user);
                string updateExpression = $"REMOVE OnlineProfiles[{profileIndex}]";
                UpdateItemRequest request = new UpdateItemRequest
                {
                    TableName = tableName,
                    Key = Mappers.UserModel.ToDbKey(user),
                    UpdateExpression = updateExpression
                };
                await dynamoDbCore.UpdateItem(request);
                onlineProfiles.RemoveAt(profileIndex);
                user.OnlineProfiles = onlineProfiles;
            }

            return onlineProfiles;
        }
        public async Task<UserModel> CreateUser(UserModel user)
        {
            logWriter.LogInformation($"{nameof(CreateUser)}({nameof(user.Id)} = '{user.Id}', {nameof(user.Name)} = '{user.Name}', {nameof(user.Email)} = '{user.Email}')");
            try
            {
                var item = Mappers.UserModel.ToDbItem(user);

                var request = new PutItemRequest(tableName, item);
                await dynamoDbCore.PutItem(request);

                logWriter.LogInformation($"User record created for '{user.Email}'");
                return user;
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(CreateUser)}({nameof(user.Email)} = '{user.Email}'):\n{ex.ToString()}");
                throw new Exception("Error when creating user.", ex);
            }
        }



        public Task<UserModel> FindUserByEmail(string email)
        {
            logWriter.LogInformation($"{nameof(FindUserByEmail)}({nameof(email)} = '{email}')");

            var request = new GetItemRequest(tableName, new Dictionary<string, AttributeValue>
            {
                {"PartitionKey", new AttributeValue(email.ToLowerInvariant())},
                {"SortKey", new AttributeValue("user")}
            });

            return dynamoDbCore.GetItem(request, Mappers.UserModel, UserModel.Null);
        }
        public async Task<UserModel> GetUserById(UserId userId)
        {
            var request = new QueryRequest(tableName);
            request.IndexName = "GSI1";
            request.KeyConditions = new Dictionary<string, Condition>
            {
                { "SortKey", dynamoDbCore.GetStringEqualsCondition("user") },
                { "GSI1PartitionKey", dynamoDbCore.GetStringEqualsCondition(userId.ToDbValue()) }
            };

            return (await dynamoDbCore.Query(request, Mappers.UserModel)).FirstOrDefault() ?? UserModel.Null;

        }
    }
}