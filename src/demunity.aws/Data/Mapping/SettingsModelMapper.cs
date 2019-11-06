using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using demunity.lib;
using demunity.lib.Data.Models;

namespace demunity.aws.Data.Mapping
{
    public class SettingsModelMapper : IValueMapper<SettingsModel>
    {
        public SettingsModel FromDbItem(Dictionary<string, AttributeValue> input)
        {
            var result = new SettingsModel();

            result.Domain = input.GetString(FieldMappings.Settings.Domain);
            result.CreatedTime = input.GetDateTimeOffset(FieldMappings.CreatedTime);
            result.Version = input.GetString(FieldMappings.Settings.Version);
            result.SettingObjectJson = input.GetString(FieldMappings.Settings.SettingObjectJson);

            return result;
        }

        public Dictionary<string, AttributeValue> ToDbItem(SettingsModel input)
        {
            var item = ToDbKey(input);
            item.Add(FieldMappings.Settings.Version, new AttributeValue(input.Version));
            item.Add(FieldMappings.Settings.SettingObjectJson, new AttributeValue(input.SettingObjectJson));
            item.Add(FieldMappings.CreatedTime, new AttributeValue(input.CreatedTime.ToString(Constants.DateTimeFormatWithMilliseconds)));
            item.Add(FieldMappings.RecordType, new AttributeValue("setting"));
            return item;
        }

        public Dictionary<string, AttributeValue> ToDbKey(SettingsModel input)
        {
            return new Dictionary<string, AttributeValue>
            {
                {FieldMappings.PartitionKey, new AttributeValue("setting")},
                {FieldMappings.SortKey, new AttributeValue(input.Domain)}
            };
        }
    }
}