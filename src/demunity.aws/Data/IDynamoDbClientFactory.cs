using Amazon.DynamoDBv2;
using demunity.lib;

namespace demunity.aws.Data.DynamoDb
{
    public interface IDynamoDbClientFactory
	{
	    IAmazonDynamoDB GetClient(AwsRegion region);
	}
}