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
    public class DefaultValueLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string DEFAULT_VALUES_FILE = "defaultValues.json";
        private readonly string _settingsPath;
        private Dictionary<string, object> _defaultValues;

        // Properties for specific default values
        public string DefCatNum => GetDefaultValue<string>("DefCatNum");
        public string DefPrice => GetDefaultValue<string>("DefPrice");
        public string DefShelfLife => GetDefaultValue<string>("DefShelfLife");
        public string DefNetWt => GetDefaultValue<string>("DefNetWt");
        public string DefBarcodeVal => GetDefaultValue<string>("DefBarcodeVal");
        public string DefBarType => GetDefaultValue<string>("DefBarType");        
        public string DefLabelName => GetDefaultValue<string>("DefLabelName");

        public DefaultValueLoader()
        {
            _settingsPath = Path.Combine(Directory.GetCurrentDirectory(), SETTINGS_FOLDER);
            LoadDefaultValues();
        }

        private void LoadDefaultValues()
        {
            string jsonFilePath = Path.Combine(_settingsPath, DEFAULT_VALUES_FILE);
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                _defaultValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            else
            {
                throw new FileNotFoundException($"Default values JSON file not found. Expected path: {jsonFilePath}");
            }
        }
        private T GetDefaultValue<T>(string key)
        {
            if (_defaultValues.TryGetValue(key, out object value))
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

        public tblProducts ApplyDefaultValues(tblProducts product)
        {
            product.CategoryNum = DefCatNum ?? product.CategoryNum;
            product.Price = DefPrice ?? product.Price;
            product.ShelfLife = DefShelfLife ?? product.ShelfLife;
            product.NetWt = DefNetWt ?? product.NetWt;
            product.Barcode = DefBarcodeVal ?? product.Barcode;
            product.BarType = DefBarType ?? product.BarType;
            product.LblName = DefLabelName ?? product.LblName;
            // Additional properties can be set here based on the default values

            return product;
        }

    }
}
