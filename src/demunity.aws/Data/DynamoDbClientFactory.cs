using Amazon;
using Amazon.DynamoDBv2;
using demunity.lib;

namespace demunity.aws.Data.DynamoDb
{
    public class DynamoDbClientFactory : IDynamoDbClientFactory
    {
        private readonly string localDbEndpoint;

        public DynamoDbClientFactory(string localDbEndpoint)
        {
            this.localDbEndpoint = localDbEndpoint;
        }

        public IAmazonDynamoDB GetClient(AwsRegion region)
        {
            if (string.IsNullOrEmpty(localDbEndpoint))
            {
                RegionEndpoint regionEndpoint = GetRegionEndpoint(region);
                return new AmazonDynamoDBClient(regionEndpoint);
            }
            else
            {
                AmazonDynamoDBConfig config = new AmazonDynamoDBConfig
                {
                    ServiceURL = localDbEndpoint,
                };
                return new AmazonDynamoDBClient(config);
            }
        }

        private static RegionEndpoint GetRegionEndpoint(AwsRegion region)
        {
            switch (region)
            {
                case AwsRegion.EUWest1:
                    return RegionEndpoint.EUWest1;
                default:
                    return RegionEndpoint.EUWest1;
            }
        }
    }
}