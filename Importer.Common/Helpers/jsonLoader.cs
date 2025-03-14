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
    }
}
