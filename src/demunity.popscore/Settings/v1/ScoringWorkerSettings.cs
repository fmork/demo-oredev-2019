using System;
using System.Collections.Generic;

namespace demunity.popscore.Settings.v1
{
    public class ScoringWorkerSettings
    {
        public IEnumerable<ThresholdConfiguration> ThresholdConfigurations { get; set; }
    }

    public class ThresholdConfiguration
    {
        public TimeSpan LookbackSpan { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTimeOffset LatestRunTime { get; set; }
    }
}