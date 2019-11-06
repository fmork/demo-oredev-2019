using System;
using demunity.lib.Logging;
using Xunit.Abstractions;

namespace demunity.testtools
{
    public class TestOutputHelperLogger<T> : ILogWriter<T>
    {
        private readonly ITestOutputHelper outputHelper;

        public TestOutputHelperLogger(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }
        public void LogCritical(string text)
        {
            PrintLog("Critical", text);
        }

        public void LogError(Exception ex, string text)
        {
            PrintLog("Error", text);
        }

        public void LogInformation(string text)
        {
            PrintLog("Info", text);
        }

        public void LogWarning(string text)
        {
            PrintLog("Warning", text);
        }

        private void PrintLog(string level, string text)
        {
            outputHelper.WriteLine($"[{typeof(T).Name}]{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff")}\t{level}\t{text}");
        }
    }
}