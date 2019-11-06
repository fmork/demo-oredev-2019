using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace demunity.aws.Data.Mapping
{
	public interface IValueMapper<T>
    {

        Dictionary<string, AttributeValue> ToDbKey(T input);
        Dictionary<string, AttributeValue> ToDbItem(T input);
        T FromDbItem(Dictionary<string, AttributeValue> input);
    }
}