using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Generic.Helpers
{
    public static class FieldMapLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private static string _mapFileName;
        private static readonly string _mapPath;

        public static Dictionary<string, string> FieldMap;

        static FieldMapLoader()
        {
            _mapFileName = "Generic.FieldMap.json";
            _mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            FieldMap = jsonLoader.LoadSettings(_mapPath, _mapFileName)
                                  .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        }
    }
}
