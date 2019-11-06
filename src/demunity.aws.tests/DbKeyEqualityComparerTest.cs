using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using Shouldly;
using Xunit;

namespace demunity.aws.tests
{
    public class DbKeyEqualityComparerTest
    {
        [Fact]
        public void DetectsDuplicates()
        {
            var x = new Dictionary<string, AttributeValue>
            {
                {"key1", new AttributeValue("value1")},
                {"key2", new AttributeValue("value2")}
            };

            var y = new Dictionary<string, AttributeValue>
            {
                {"key1", new AttributeValue("value1")},
                {"key2", new AttributeValue("value2")}
            };

            var comparer = new DbKeyEqualityComparer();

            comparer.Equals(x, y).ShouldBeTrue();

        }

        [Fact]
        public void DetectsDuplicatesWithKeysInDifferentOrder()
        {
            var x = new Dictionary<string, AttributeValue>
            {
                {"key1", new AttributeValue("value1")},
                {"key2", new AttributeValue("value2")}
            };

            var y = new Dictionary<string, AttributeValue>
            {
                {"key2", new AttributeValue("value2")},
                {"key1", new AttributeValue("value1")}
            };

            var comparer = new DbKeyEqualityComparer();

            comparer.Equals(x, y).ShouldBeTrue();

        }

        [Fact]
        public void DetectsDifferentKeys()
        {
            var x = new Dictionary<string, AttributeValue>
            {
                {"key1", new AttributeValue("value1")},
                {"key2", new AttributeValue("value2")}
            };

            var y = new Dictionary<string, AttributeValue>
            {
                {"key1", new AttributeValue("value1")},
                {"key3", new AttributeValue("value3")}
            };

            var comparer = new DbKeyEqualityComparer();

            comparer.Equals(x, y).ShouldBeFalse();

        }

        [Fact]
        public void WorksWithLinqDistinct()
        {
            //Given
            var keys = new[]{
                new Dictionary<string, AttributeValue>
                {
                    {"key1", new AttributeValue("value1")},
                    {"key2", new AttributeValue("value2")}
                },

                new Dictionary<string, AttributeValue>
                {
                    {"key2", new AttributeValue("value2")},
                    {"key1", new AttributeValue("value1")}
                }
            };


            //When
            var distinctKeys = keys.Distinct(new DbKeyEqualityComparer());

            //Then
            distinctKeys.Count().ShouldBe(1);
        }

        
    }
}