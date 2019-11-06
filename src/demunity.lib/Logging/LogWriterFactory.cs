using System;
using Microsoft.Extensions.Logging;

namespace demunity.lib.Logging
{
    public class AspnetCoreLogWriterFactory : ILogWriterFactory
    {
        private readonly ILoggerFactory loggerFactory;

        public AspnetCoreLogWriterFactory(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }
        public ILogWriter<T> CreateLogger<T>()
        {
            return new LogWriter<T>(loggerFactory.CreateLogger<T>());
        }
    }
}