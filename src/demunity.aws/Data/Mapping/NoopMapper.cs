using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace demunity.aws.Data.Mapping
{
    public class NoopMapper : IValueMapper<Dictionary<string, AttributeValue>>
    {
        public Dictionary<string, AttributeValue> FromDbItem(Dictionary<string, AttributeValue> input)
        {
            return input;
        }

        public Dictionary<string, AttributeValue> ToDbItem(Dictionary<string, AttributeValue> input)
        {
            return input;
        }

        public Dictionary<string, AttributeValue> ToDbKey(Dictionary<string, AttributeValue> input)
        {
            return input
                .Where(pair =>
                    pair.Key.Equals(FieldMappings.PartitionKey, StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Equals(FieldMappings.SortKey, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}