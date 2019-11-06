using System;

namespace demunity.lib.Logging
{
    public interface ILogWriter
    {
        void LogCritical(string text);
        void LogWarning(string text);
        void LogInformation(string text);
        void LogError(Exception ex, string text);

    }
    public interface ILogWriter<T> : ILogWriter
    {
    }
}