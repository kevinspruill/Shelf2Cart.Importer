using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Helpers
{
    public class GenericSettingsLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "Generic.Settings.json";
        private readonly string _settingsPath;
        private Dictionary<string, object> _settings;

        // Properties for specific settings
        public string FieldDelimiter => jsonLoader.GetSetting<string>("FieldDelimiter", _settings);
        public string Parser => jsonLoader.GetSetting<string>("Parser", _settings);
        public string RecordSeparator => jsonLoader.GetSetting<string>("RecordSeparator", _settings);

        public GenericSettingsLoader()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            _settings = jsonLoader.LoadSettings(_settingsPath, SETTINGS_FILE);
        }
    }
}
