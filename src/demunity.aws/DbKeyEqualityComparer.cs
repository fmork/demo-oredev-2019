using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace demunity.aws
{
    public class DbKeyEqualityComparer : IEqualityComparer<Dictionary<string, AttributeValue>>
    {
        public bool Equals(Dictionary<string, AttributeValue> x, Dictionary<string, AttributeValue> y)
        {
            // if x and y are the same instance (or both are null), they are equal
            if (x == y)
            {
                return true;
            }

            // if one of x and y is null, they are not equal
            if (x == null || y == null)
            {
                return false;
            }

            // if x and y contains different items counts, they are not equal
            if (x.Count != y.Count)
            {
                return false;
            }


            return x.Keys.All(key => y.Keys.Contains(key) && IsSameAttributeValue(x[key], y[key]));
        }

        private bool IsSameAttributeValue(AttributeValue x, AttributeValue y)
        {
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.S == y.S;
        }

        public int GetHashCode(Dictionary<string, AttributeValue> obj)
        {

            int result = 0;
            foreach (var item in obj.OrderBy(x => x.Key))
            {
                var itemHashCode = item.Key.GetHashCode() ^ (item.Value?.S.GetHashCode() ?? 0);

                result = result == 0
                    ? itemHashCode
                    : result ^ itemHashCode;
            }

            return result;
        }


    }
}