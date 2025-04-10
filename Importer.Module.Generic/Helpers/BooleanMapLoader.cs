using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Generic.Helpers
{
    public static class BooleanMapLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private static string _mapFileName;
        private static readonly string _mapPath;

        public static Dictionary<string, string> BooleanVals;

        static BooleanMapLoader()
        {
            _mapFileName = "Generic.BooleanVals.json";
            _mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            BooleanVals = jsonLoader.LoadSettings(_mapPath, _mapFileName)
                                  .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        }
    }
}
