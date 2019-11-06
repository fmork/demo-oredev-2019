using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using demunity.lib;
using demunity.lib.Data;
using demunity.lib.Data.Models;

namespace demunity.aws.Data
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly string tableName;
        private readonly IDynamoDbCore dynamoDbCore;

        public SettingsRepository(ISystem system, IDynamoDbCore dynamoDbCore)
        {
            if (system is null)
            {
                throw new System.ArgumentNullException(nameof(system));
            }
            tableName = system.Environment.GetVariable(Constants.EnvironmentVariables.DynamoDbTableName);
            this.dynamoDbCore = dynamoDbCore ?? throw new System.ArgumentNullException(nameof(dynamoDbCore));
        }
        public Task<SettingsModel> GetSettings(string settingsDomain)
        {
            GetItemRequest request = new GetItemRequest
            {
                TableName = tableName,
                Key = Mappers.Settings.ToDbKey(new SettingsModel { Domain = settingsDomain })
            };

            return dynamoDbCore.GetItem(request, Mappers.Settings);
        }

        public Task SetSettings(string settingsDomain, SettingsModel settings)
        {
            PutItemRequest request = new PutItemRequest
            {
                TableName = tableName,
                Item = Mappers.Settings.ToDbItem(settings)
            };

            return dynamoDbCore.PutItem(request);
        }
    }
}