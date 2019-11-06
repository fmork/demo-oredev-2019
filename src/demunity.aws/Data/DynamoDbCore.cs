using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data.DynamoDb;
using demunity.aws.Data.Mapping;
using demunity.lib;
using demunity.lib.Logging;
using Newtonsoft.Json;

namespace demunity.aws.Data
{
    public class DynamoDbCore : IDynamoDbCore
    {
        private const int MaxNumberOfBatchWriteRequestItems = 25;
        private readonly ILogWriter<DynamoDbCore> logWriter;
        private readonly IDynamoDbClientFactory dynamoDbClientFactory;

        public DynamoDbCore(
            IDynamoDbClientFactory dynamoDbClientFactory,
            ILogWriterFactory logWriterFactory)
        {
            if (logWriterFactory is null)
            {
                throw new ArgumentNullException(nameof(logWriterFactory));
            }

            logWriter = logWriterFactory.CreateLogger<DynamoDbCore>();

            this.dynamoDbClientFactory = dynamoDbClientFactory ?? throw new ArgumentNullException(nameof(dynamoDbClientFactory));
        }

        public async Task<IEnumerable<TResult>> Query<TResult>(QueryRequest request, IValueMapper<TResult> valueMapper)
        {
            var response = await PerformDbAction(request, (client, r) => client.QueryAsync(r));
            if (LogCapacityForRequest(request.ReturnConsumedCapacity))
            {
                logWriter.LogInformation($"Consumed DynamoDB capacity:\n{JsonConvert.SerializeObject(response.ConsumedCapacity)}");
            }
            return response.Items.Select(valueMapper.FromDbItem);
        }

        public Task<TResult> GetItem<TResult>(GetItemRequest request, IValueMapper<TResult> valueMapper)
        {
            return GetItem(request, valueMapper, default(TResult));
        }

        public async Task<TResult> GetItem<TResult>(GetItemRequest request, IValueMapper<TResult> valueMapper, TResult notFoundItem)
        {
            var response = await PerformDbAction(request, (client, r) => client.GetItemAsync(r));
            if (LogCapacityForRequest(request.ReturnConsumedCapacity))
            {
                logWriter.LogInformation($"Consumed DynamoDB capacity:\n{JsonConvert.SerializeObject(response.ConsumedCapacity)}");
            }
            return response.IsItemSet
                ? valueMapper.FromDbItem(response.Item)
                : notFoundItem;
        }

        public async Task PutItem(PutItemRequest request)
        {
            var response = await PerformDbAction(request, (client, r) => client.PutItemAsync(r));

            if (LogCapacityForRequest(request.ReturnConsumedCapacity))
            {
                logWriter.LogInformation($"Consumed DynamoDB capacity:\n{JsonConvert.SerializeObject(response.ConsumedCapacity)}");
            }
        }

        public async Task UpdateItem(UpdateItemRequest request)
        {

            var response = await PerformDbAction(request, (client, r) => client.UpdateItemAsync(r));
            if (LogCapacityForRequest(request.ReturnConsumedCapacity))
            {
                logWriter.LogInformation($"Consumed DynamoDB capacity:\n{JsonConvert.SerializeObject(response.ConsumedCapacity)}");
            }

        }

        public async Task DeleteItem(DeleteItemRequest request)
        {
            var response = await PerformDbAction(request, (client, r) => client.DeleteItemAsync(r));
            if (LogCapacityForRequest(request.ReturnConsumedCapacity))
            {
                logWriter.LogInformation($"Consumed DynamoDB capacity:\n{JsonConvert.SerializeObject(response.ConsumedCapacity)}");
            }
        }

        public async Task BatchWriteItem(BatchWriteItemRequest request)
        {
            int skip = 0;
            BatchWriteItemRequest batchRequest;
            var tasks = new List<Task<BatchWriteItemResponse>>();
            while ((batchRequest = GetNextBatch(request, skip)) != null)
            {
                skip += batchRequest.RequestItems.Count;
                tasks.Add(PerformDbAction(batchRequest, (client, r) => client.BatchWriteItemAsync(r)));
            }

            await Task.WhenAll(tasks);

            if (LogCapacityForRequest(request.ReturnConsumedCapacity))
            {
                var tasksArray = tasks.ToArray();
                for (int i = 0; i < tasksArray.Length; i++)
                {
                    logWriter.LogInformation($"Consumed DynamoDB capacity [{i}]:\n{JsonConvert.SerializeObject(tasksArray[i].Result.ConsumedCapacity)}");
                }
            }
        }



        public async Task<Dictionary<string, List<Dictionary<string, AttributeValue>>>> BatchGetItem(BatchGetItemRequest request)
        {
            var response = await PerformDbAction(request, (client, r) => client.BatchGetItemAsync(r));
            if (LogCapacityForRequest(request.ReturnConsumedCapacity))
            {
                logWriter.LogInformation($"Consumed DynamoDB capacity:\n{JsonConvert.SerializeObject(response.ConsumedCapacity)}");
            }
            return response.Responses;
        }

        public async Task<IEnumerable<TResult>> Scan<TResult>(ScanRequest request, IValueMapper<TResult> valueMapper)
        {
            var response = await PerformDbAction(request, (client, r) => client.ScanAsync(r));
            if (LogCapacityForRequest(request.ReturnConsumedCapacity))
            {
                logWriter.LogInformation($"Consumed DynamoDB capacity:\n{JsonConvert.SerializeObject(response.ConsumedCapacity)}");
            }
            return response.Items.Select(valueMapper.FromDbItem);

        }

        public Condition GetStringEqualsCondition(string value)
        {
            return GetStringCondition(ComparisonOperator.EQ, value);
        }

        public Condition GetStringBeginsWithCondition(string value)
        {
            return GetStringCondition(ComparisonOperator.BEGINS_WITH, value);
        }

        private static BatchWriteItemRequest GetNextBatch(BatchWriteItemRequest request, int skip)
        {
            var writeRequests = request.RequestItems.Skip(skip).Take(MaxNumberOfBatchWriteRequestItems).ToDictionary(x => x.Key, x => x.Value);
            return writeRequests.Any()
                ? new BatchWriteItemRequest { RequestItems = writeRequests, ReturnConsumedCapacity = request.ReturnConsumedCapacity }
                : null;
        }

        private static Condition GetStringCondition(ComparisonOperator op, string value)
        {
            return new Condition
            {
                ComparisonOperator = op,
                AttributeValueList = new List<AttributeValue> { new AttributeValue { S = value } }
            };
        }

        private async Task<TResponse> PerformDbAction<TRequest, TResponse>(TRequest request, Func<IAmazonDynamoDB, TRequest, Task<TResponse>> dbAction)
        {
            try
            {
                using (var client = dynamoDbClientFactory.GetClient(AwsRegion.EUWest1))
                {
                    return await dbAction(client, request);
                }
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(PerformDbAction)}\n{ex.ToString()}\nRequest:\n{JsonConvert.SerializeObject(request)}");
                throw;
            }
        }
        private async Task PerformDbAction<TRequest>(TRequest request, Func<IAmazonDynamoDB, TRequest, Task> dbAction)
        {
            try
            {
                using (var client = dynamoDbClientFactory.GetClient(AwsRegion.EUWest1))
                {
                    await dbAction(client, request);
                }
            }
            catch (Exception ex)
            {
                logWriter.LogError(ex, $"Error in {nameof(PerformDbAction)}\n{ex.ToString()}\nRequest:\n{JsonConvert.SerializeObject(request)}");
                throw;
            }
        }

        private static bool LogCapacityForRequest(ReturnConsumedCapacity returnConsumedCapacity)
        {
            return returnConsumedCapacity == ReturnConsumedCapacity.INDEXES || returnConsumedCapacity == ReturnConsumedCapacity.TOTAL;
        }
    }
}