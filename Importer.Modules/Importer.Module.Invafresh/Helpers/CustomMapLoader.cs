using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Helpers
{
    public static class CustomMapLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private static string _mapFileName;
        private static readonly string _mapPath;

        public static Dictionary<string, string> CustomMap;

        static CustomMapLoader()
        {
            InvafreshSettingsLoader Settings = new InvafreshSettingsLoader();
            _mapFileName = Settings.CustomMapFileName;
            _mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            CustomMap = jsonLoader.LoadSettings(_mapPath, _mapFileName)
                                  .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        }
    }
}
