using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Admin.Helpers
{
    public class AdminSettingsLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "Admin.Settings.json";
        private readonly string _settingsPath;
        private Dictionary<string, object> _settings;

        // Properties for specific settings
        public string AdminConsoleProcessingFolder => jsonLoader.GetSetting<string>("AdminConsoleProcessingFolder", _settings);
        public string AdminConsoleFileName => jsonLoader.GetSetting<string>("AdminConsoleFileName", _settings);

        public AdminSettingsLoader()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            _settings = jsonLoader.LoadSettings(_settingsPath, SETTINGS_FILE);
        }
    }
}
