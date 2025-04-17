using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Module.Generic.Helpers;
using Importer.Module.Generic.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Generic.Parser
{
    public class JsonParser : IParser
    {
        private tblProducts ProductTemplate { get; set; }
        private ICustomerProcess _customerProcess { get; set; }
        public List<Dictionary<string, string>> PLURecords { get; private set; } = new List<Dictionary<string, string>>();
        public List<Dictionary<string, string>> DeletedPLURecords { get; private set; } = new List<Dictionary<string, string>>();

        public JsonParser(tblProducts productTemplate, ICustomerProcess customerProcess = null)
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();
        }

        public void ParseFile(string filePath)
        {
            //read the file
            var jsonData = File.ReadAllText(filePath);
            //deserialize into a List<Dictionary<string, object>> before sending to PLURecords
            var deserializedJson = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonData);

            foreach (var record in deserializedJson)
            {
                PLURecords.Add(record.ToDictionary(k => k.Key, v => v.Value.ToString()));
            }
        }
        public List<tblProducts> ConvertPLURecordsToTblProducts()
        {
            var products = new List<tblProducts>();
            foreach (var pluItem in PLURecords)
            {
                var product = ConvertPLURecordToTblproducts(pluItem);
                products.Add(product);
            }
            return products;
        }
        public List<tblProducts> ConvertPLUDeleteRecordsToTblProducts()
        {
            var products = new List<tblProducts>();
            foreach (var pluItem in DeletedPLURecords)
            {
                var product = ConvertPLURecordToTblproducts(pluItem);
                products.Add(product);
            }
            return products;
        }
        private tblProducts ConvertPLURecordToTblproducts(Dictionary<string, string> pluItem)
        {
            var product = ProductTemplate.Clone();

            // Create a dictionary to map the fields
            var mappedFields = FieldMapLoader.FieldMap;
            foreach (var field in mappedFields)
            {

                var propertyWithAttribute = typeof(tblProducts).GetProperties()
                    .FirstOrDefault(prop =>
                    {
                        var attr = prop.GetCustomAttributes(typeof(ImportDBFieldAttribute), false)
                            .Cast<ImportDBFieldAttribute>()
                            .FirstOrDefault();
                        return attr != null && attr.Name == field.Key;
                    });

                if (propertyWithAttribute != null)
                {
                    // check pluItem to see if the field.Key exists
                    bool fieldValueExists = pluItem.ContainsKey(field.Key);

                    if (fieldValueExists)
                    {
                        // Get the value from pluItem and set it to the product, converting it to the correct type

                        var value = pluItem[field.Key];
                        var propertyType = propertyWithAttribute.PropertyType;

                        if (propertyType == typeof(bool))
                        {
                            // The field in BooleanVals matches the field name, the value is what is constitutes a true value
                            var trueValues = BooleanMapLoader.BooleanVals[field.Key];
                            var isTrue = trueValues == value;
                            propertyWithAttribute.SetValue(product, isTrue);
                        }
                        else
                        {
                            var convertedValue = Convert.ChangeType(value, propertyType);
                            propertyWithAttribute.SetValue(product, convertedValue);
                        }

                    }
                }
            }

            return product;
        }
    }
}
