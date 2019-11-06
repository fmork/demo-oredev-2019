using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using demunity.lib;

namespace demunity.aws.Data.Mapping
{
    public static class DataItemExtensions
    {
        public static string GetString(this Dictionary<string, AttributeValue> item, string key)
        {
            return item.TryGetValue(key, out var value) ? value.S : string.Empty;
        }

        public static TResult GetValue<TResult>(this Dictionary<string, AttributeValue> item, string key, Func<string, TResult> transform)
        {
            return transform(GetString(item, key));
        }

        public static IEnumerable<TResult> GetList<TResult>(this Dictionary<string, AttributeValue> item, string key, Func<string, TResult> transform)
        {
            return item.TryGetValue(key, out var value)
                    ? value.L.Select(x => transform(x.S))
                    : new TResult[] { };
        }

        public static DateTimeOffset GetDateTimeOffset(this Dictionary<string, AttributeValue> item, string key)
        {
            return item.TryGetValue(key, out var value)
                ? DateTimeOffset.ParseExact(value.S, Constants.DateTimeFormatWithMilliseconds, null)
                : DateTimeOffset.MinValue;
        }

        public static int GetInt32(this Dictionary<string, AttributeValue> item, string key)
        {
            return item.TryGetValue(key, out var value)
                ? int.Parse(value.N)
                : default(int);
        }
    }
}