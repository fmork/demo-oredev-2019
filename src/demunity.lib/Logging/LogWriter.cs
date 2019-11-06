using System;
using Microsoft.Extensions.Logging;

namespace demunity.lib.Logging
{
    public class LogWriter<T> : ILogWriter<T>
    {
        private readonly ILogger<T> logger;

        public LogWriter(ILogger<T> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            this.logger = logger;
        }
        public void LogCritical(string text)
        {
            logger.LogCritical(text);
        }

        public void LogError(Exception ex, string text)
        {
            logger.LogError(ex, text);
        }

        public void LogInformation(string text)
        {
            logger.LogInformation(text);
        }

        public void LogWarning(string text)
        {
            logger.LogWarning(text);
        }
    }
}