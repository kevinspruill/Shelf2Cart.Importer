using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Common.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.ThirdPartyAPI
{
    public class ECRSJSONParser
    {
        //copied code from Generic JSON Parser
        private tblProducts ProductTemplate { get; set; }
        private ICustomerProcess _customerProcess { get; set; }
        public List<Dictionary<string, string>> PLURecords { get; private set; } = new List<Dictionary<string, string>>();

        public ECRSJSONParser(tblProducts productTemplate, ICustomerProcess customerProcess = null)
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();
        }

        public bool ParseFile(string jsonString)
        {
            try
            {
                return true;
            }
            catch (JsonException ex)
            {
                Logger.Error($"JSON parsing error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error: {ex.Message}");
                return false;
            }
        }

        public List<tblProducts> ConvertPLURecordsToTblProducts()
        {
            var products = new List<tblProducts>();
            foreach (var pluItem in PLURecords)
            {
                var product = ConvertPLURecordToTblproducts(pluItem);
                if (!string.IsNullOrWhiteSpace(product.PLU)) //items without a PLU are not meant for sale
                    products.Add(product);
            }
            return products;
        }

        private tblProducts ConvertPLURecordToTblproducts(Dictionary<string, string> pluItem)
        {
            var product = ProductTemplate.Clone();

            product.Description1 = GetValue(pluItem, "name");

            return product;
        }

        public string GetValue(Dictionary<string, string> pluItem, string key)
        {
            if (pluItem.TryGetValue(key, out string value))
                return value;
            else
                return string.Empty;
        }
    }
}
