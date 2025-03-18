using Importer.Common.Helpers;
using Importer.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Helpers
{
    public class InvafreshSettingsLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "Invafresh.Settings.json";
        private readonly string _settingsPath;
        private Dictionary<string, object> _settings;

        // Properties for specific settings
        public int TareDigits => GetSetting<int>("TareDigits");
        public bool UseCustomMapping => GetSetting<bool>("UseCustomMapping");
        public bool UseLegacyNutritionFormat => GetSetting<bool>("UseLegacyNutritionFormat");
        public string CustomMapFileName => GetSetting<string>("CustomMapFileName");

        public InvafreshSettingsLoader()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            _settings = jsonLoader.LoadSettings(_settingsPath, SETTINGS_FILE);
        }
        
        private T GetSetting<T>(string key)
        {
            if (_settings.TryGetValue(key, out object value))
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
