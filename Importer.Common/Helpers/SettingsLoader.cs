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
        public bool TestingEnabled => jsonLoader.GetSetting<bool>("TestingEnabled", _settings);
        public string DeptColName => jsonLoader.GetSetting<string>("DeptColName", _settings);

        // Properties for boolean settings
        public bool ButtonSameAsDesc => jsonLoader.GetSetting<bool>("ButtonSameAsDesc", _settings);
        public bool Flush => jsonLoader.GetSetting<bool>("Flush", _settings);
        public bool ProperCase => jsonLoader.GetSetting<bool>("ProperCase", _settings);
        public bool DepartmentPLU => jsonLoader.GetSetting<bool>("DepartmentPLU", _settings);
        public bool DescriptionCleanse => jsonLoader.GetSetting<bool>("DescriptionCleanse", _settings);

        // Properties for ProperCase, AllCaps  and FindReplace fields
        public List<string> ProperCaseFields => jsonLoader.GetSetting<List<string>>("ProperCaseFields", _settings);
        public List<string> FindReplaceFields => jsonLoader.GetSetting<List<string>>("FindReplaceFields", _settings);
        public List<string> AllCapsFields => jsonLoader.GetSetting<List<string>>("AllCapsFields", _settings);


        public SettingsLoader()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            _settings = jsonLoader.LoadSettings(_settingsPath, SETTINGS_FILE);
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
