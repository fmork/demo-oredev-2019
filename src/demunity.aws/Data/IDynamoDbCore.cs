using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using demunity.aws.Data.Mapping;

namespace demunity.aws.Data
{
    public interface IDynamoDbCore
    {
        Task PutItem(PutItemRequest request);
        Task<TResult> GetItem<TResult>(GetItemRequest request, IValueMapper<TResult> valueMapper);
        Task<TResult> GetItem<TResult>(GetItemRequest request, IValueMapper<TResult> valueMapper, TResult notFoundItem);
        Task<IEnumerable<TResult>> Query<TResult>(QueryRequest request, IValueMapper<TResult> valueMapper);
        Task UpdateItem(UpdateItemRequest request);
        Task DeleteItem(DeleteItemRequest deleteCommentRequest);
        Task BatchWriteItem(BatchWriteItemRequest hashtagsRequests);
        Task<IEnumerable<TResult>> Scan<TResult>(ScanRequest request, IValueMapper<TResult> valueMapper);
        Task<Dictionary<string, List<Dictionary<string, AttributeValue>>>> BatchGetItem(BatchGetItemRequest request);
        Condition GetStringEqualsCondition(string value);
        Condition GetStringBeginsWithCondition(string value);
    }
}