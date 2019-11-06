using System;
using Amazon.Lambda.Core;
using demunity.lib;
using demunity.lib.Logging;

namespace demunity.aws.Logging
{
	public class LambdaLogWriter<T> : ILogWriter<T>
	{
		private readonly ILambdaLogger logger;

		public LambdaLogWriter(ILambdaLogger logger)
		{
			this.logger = logger;
		}
		public void LogCritical(string text)
		{
			WriteLog("Critical", text);
		}

		public void LogError(Exception ex, string text)
		{
			WriteLog("Error", $"{text}\n\nError details:\n{ex.ToString()}");
		}

		public void LogInformation(string text)
		{
			WriteLog("Information", text);
		}

		public void LogWarning(string text)
		{
			WriteLog("Warning", text);
		}

		private void WriteLog(string level, string text)
		{
			logger.LogLine($"{DateTimeOffset.UtcNow.ToString(Constants.DateTimeFormatWithMilliseconds)}\t[{level}]\t{typeof(T).Namespace}.{typeof(T).Name}\t{text}");
		}
	}
}