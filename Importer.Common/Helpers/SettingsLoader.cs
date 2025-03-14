using Importer.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Helpers
{
    public class SettingsLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "settings.json";
        private readonly string _settingsPath;
        private Dictionary<string, object> _settings;

        // Properties for specific settings
        public bool TestingEnabled => GetSetting<bool>("TestingEnabled");
        public string DeptColName => GetSetting<string>("DeptColName");

        // Properties for boolean settings
        public bool ButtonSameAsDesc => GetSetting<bool>("ButtonSameAsDesc");
        public bool Flush => GetSetting<bool>("Flush");
        public bool ProperCase => GetSetting<bool>("ProperCase");
        public bool DepartmentPLU => GetSetting<bool>("DepartmentPLU");
        public bool DescriptionCleanse => GetSetting<bool>("DescriptionCleanse");

        // Properties for ProperCase, AllCaps  and FindReplace fields
        public List<string> ProperCaseFields => GetSetting<List<string>>("ProperCaseFields");
        public List<string> FindReplaceFields => GetSetting<List<string>>("FindReplaceFields");
        public List<string> AllCapsFields => GetSetting<List<string>>("AllCapsFields");


        public SettingsLoader()
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

        public bool ShouldApplyProperCase(PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute<ImportDBFieldAttribute>();
            return ProperCase && attribute != null && ProperCaseFields.Contains(attribute.Name);
        }

        public bool ShouldApplyAllCaps(PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute<ImportDBFieldAttribute>();
            return attribute != null && AllCapsFields.Contains(attribute.Name);
        }

        public bool ShouldApplyFindReplace(PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute<ImportDBFieldAttribute>();
            return attribute != null && FindReplaceFields.Contains(attribute.Name);
        }

    }
}
