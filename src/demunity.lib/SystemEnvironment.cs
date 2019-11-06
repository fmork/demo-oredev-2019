using System;

namespace demunity.lib
{
    public class SystemEnvironment : IEnvironment
    {
        public string GetVariable(string key)
        {
            return Environment.GetEnvironmentVariable(key);
        }
    }
}