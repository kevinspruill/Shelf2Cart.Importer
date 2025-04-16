using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Helpers
{
    public static class jsonLoader
    {
        public static Dictionary<string, object> LoadSettings(string path, string filename)
        {
            string jsonFilePath = Path.Combine(path, filename);
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            else
            {
                Logger.LogErrorEvent($"JSON file not found. Expected path: {jsonFilePath}");
                return new Dictionary<string, object>();
            }
        }

        public static T GetSetting<T>(string key, Dictionary<string, object> settings)
        {
            if (settings.TryGetValue(key, out object value))
            {
                if (typeof(T) == typeof(bool) && value is bool)
                {
                    return (T)value;
                }
                if (typeof(T) == typeof(List<string>) && value is Newtonsoft.Json.Linq.JArray jArray)
                {
                    return (T)(object)jArray.ToObject<List<string>>();
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return default(T);
        }
    }
}
