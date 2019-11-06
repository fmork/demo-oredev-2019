using System;

namespace demunity.lib.Data.Models
{
    public class SettingsModel
    {
        public string Domain { get; set; }
        public string Version { get; set; }
        public string SettingObjectJson { get; set; }
        public DateTimeOffset CreatedTime { get; set; }

    }
}