using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley.Helpers
{
    public class ParsleySettingsLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "Parsley.Settings.json";
        private readonly string _settingsPath;
        private Dictionary<string, object> _settings;

        // Properties for specific settings
        public string Endpoint => jsonLoader.GetSetting<string>("Endpoint", _settings);
        public string ApiKey => jsonLoader.GetSetting<string>("ApiKey", _settings);

        public ParsleySettingsLoader()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            _settings = jsonLoader.LoadSettings(_settingsPath, SETTINGS_FILE);
        }

    }
}
