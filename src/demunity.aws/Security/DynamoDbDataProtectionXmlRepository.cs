using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data.DynamoDb;
using demunity.lib;
using demunity.lib.Tasks;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace demunity.aws.Security
{
    public class DynamoDbDataProtectionXmlRepository : IXmlRepository
    {
        private static readonly List<string> allAttributes = new string[] { "Id", "FriendlyName", "Xml" }.ToList();
        private readonly string tableName;
        private readonly ILogger<DynamoDbDataProtectionXmlRepository> logger;
        private readonly IDynamoDbClientFactory dynamoDbClientFactory;

        public DynamoDbDataProtectionXmlRepository(
            ILoggerFactory loggerFactory,
            IEnvironment environment,
            IDynamoDbClientFactory dynamoDbClientFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (dynamoDbClientFactory == null)
            {
                throw new ArgumentNullException(nameof(dynamoDbClientFactory));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            tableName = environment.GetVariable(Constants.EnvironmentVariables.DataProtectionTableName);
            logger = loggerFactory.CreateLogger<DynamoDbDataProtectionXmlRepository>();
            this.dynamoDbClientFactory = dynamoDbClientFactory;
        }


        public IReadOnlyCollection<XElement> GetAllElements()
        {

            try
            {
                logger.LogInformation($"GetAllElements()");

                using (var dbClient = dynamoDbClientFactory.GetClient(AwsRegion.EUWest1))
                {
                    var response = dbClient.ScanAsync(tableName, allAttributes).SafeGetResult();

                    return response.Items
                        .Select(item => TryParseKeyXml(item["Xml"].S))
                        .ToList()
                        .AsReadOnly();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(DynamoDbDataProtectionXmlRepository)}.{nameof(GetAllElements)}: {ex.ToString()}");
                throw;
            }
        }



        public void StoreElement(XElement element, string friendlyName)
        {
            logger.LogInformation($"{nameof(StoreElement)}(friendlyName=\"{friendlyName}\")");

            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>{
				{"Id", new AttributeValue(Guid.NewGuid().ToString())},
				{"FriendlyName", new AttributeValue(friendlyName)},
				{"Xml", new AttributeValue(element.ToString(SaveOptions.DisableFormatting))},
			};

            try
            {
                using (var dbClient = dynamoDbClientFactory.GetClient(AwsRegion.EUWest1))
                {
                    var response = dbClient.PutItemAsync(tableName, item).SafeGetResult();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(StoreElement)}\nItem:\n{JsonConvert.SerializeObject(item)}\n{ex.ToString()}");
                throw;
            }
        }



        private XElement TryParseKeyXml(string xml)
        {
            try
            {
                return XElement.Parse(xml);
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"Error in {nameof(TryParseKeyXml)}:\n{e.ToString()}");
                return null;
            }
        }

    }
}
