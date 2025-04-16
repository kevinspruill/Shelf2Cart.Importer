using Importer.Common.Helpers;
using Importer.Common.Models;
using Newtonsoft.Json.Linq;
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
        public int TareDigits => jsonLoader.GetSetting<int>("TareDigits", _settings);
        public bool UseCustomMapping => jsonLoader.GetSetting<bool>("UseCustomMapping", _settings);
        public bool UseLegacyNutritionFormat => jsonLoader.GetSetting<bool>("UseLegacyNutritionFormat", _settings);
        public string CustomMapFileName => jsonLoader.GetSetting<string>("CustomMapFileName", _settings);

        public InvafreshSettingsLoader()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            _settings = jsonLoader.LoadSettings(_settingsPath, SETTINGS_FILE);
        }

    }
}
