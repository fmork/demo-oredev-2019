using System;

namespace demunity.lib
{
    public interface ISystemTime
    {
        DateTimeOffset UtcNow { get; }
    }
}