using Importer.Common.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.Models
{
    public class ECRSFields
    {
        public int Dept { get; set; }
        public int PLU { get; set; }
        public decimal Price { get; set; }
        public decimal PromoPrice { get; set; }
        public string NetWt { get; set; }
        public int ShelfLife { get; set; }
        public string Ingredients { get; set; }
        public string Description1 { get; set; }
        public string Description2 { get; set; }
        public string Description3 { get; set; }
        public string Description4 { get; set; }
        public string UserAssigned1 { get; set; }
        public string UserAssigned2 { get; set; }
        public string UserAssigned3 { get; set; }
        public string UserAssigned4 { get; set; }
        public int UserAssigned5 { get; set; }
        public int UserAssigned6 { get; set; }
        public int UserAssigned7 { get; set; }
        public string Scaleable { get; set; }
        public decimal Tare { get; set; }
        public int CategoryNum { get; set; }
        public int Logo1 { get; set; }
        public int Logo2 { get; set; }
        public int Logo3 { get; set; }
        public int Logo4 { get; set; }
        public int Logo5 { get; set; }
        public string CountryOfOrigin { get; set; }
        public string Brand { get; set; }
        public string SubDepartment { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Variety { get; set; }

        public static Dictionary<string, string> GetJoinedFields()
        {
            var _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
            string jsonFilePath = Path.Combine(_settingsPath, "FieldMap.json");

            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            else
            {
                Logger.Error($"JSON file not found. Expected path: {jsonFilePath}");
                return new Dictionary<string, string>();
            }
        }
    }
}
