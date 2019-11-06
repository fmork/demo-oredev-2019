using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace demunity.lib
{
    public class LocalEnvironment : IEnvironment
    {
        private readonly Lazy<Dictionary<string, string>> lazyEnvironmentMap;
        public LocalEnvironment()
        {
            lazyEnvironmentMap = new Lazy<Dictionary<string, string>>(GetEnvironment);
        }

        private Dictionary<string, string> GetEnvironment()
        {
            var path = Path.Combine(
                 Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                 "localenvironmentsettings.json");
            var environmentContent = File.ReadAllText(path);

            JObject deserialzedEnvironment = (JObject)JsonConvert.DeserializeObject(environmentContent);
            var result = new Dictionary<string, string>();
            foreach (var item in deserialzedEnvironment)
            {
                result.Add(item.Key, item.Value.Value<string>());
            }
            return result;

        }

        public string GetVariable(string key)
        {
            if (lazyEnvironmentMap.Value.TryGetValue(key, out string value))
            {
                return value;
            }
            return string.Empty;
        }
    }
}