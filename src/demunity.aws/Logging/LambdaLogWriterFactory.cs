using Amazon.Lambda.Core;
using demunity.lib.Logging;

namespace demunity.aws.Logging
{
	public class LambdaLogWriterFactory : ILogWriterFactory
	{
		private readonly ILambdaLogger lambdaLogger;

		public LambdaLogWriterFactory(ILambdaLogger lambdaLogger)
		{
			this.lambdaLogger = lambdaLogger;
		}

		public ILogWriter<T> CreateLogger<T>()
		{
			return new LambdaLogWriter<T>(lambdaLogger);
		}
	}
}