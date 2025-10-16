using Importer.Common.Helpers;
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
        public GenericSettingsLoader Settings { get; } = new GenericSettingsLoader();
        public Dictionary<string, string> mappedFields { get; }
        public Dictionary<string, string> booleanVals { get; }

        public JsonParser(tblProducts productTemplate, ICustomerProcess customerProcess = null)
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();

            FieldMapLoader.Initialize();
            mappedFields = FieldMapLoader.FieldMap;

            BooleanMapLoader.Initialize();
            booleanVals = BooleanMapLoader.BooleanVals;
        }

        public void ParseFile(string filePath)
        {
            //read the file
            var jsonData = File.ReadAllText(filePath);
            
            //deserialize into a List<Dictionary<string, object>> before sending to PLURecords, use Settings.JSONPath as the root if specified
            var deserializedJson = string.IsNullOrWhiteSpace(Settings.JSONPath) ?
                JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonData) :
                JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(jsonData)?[Settings.JSONPath];

            foreach (var record in deserializedJson)
            {
                var tmpRecord = _customerProcess.DataFileCondtioning(record); //in the case of Vallarta/Logile, we need to set barcode correctly
                PLURecords.Add(tmpRecord.ToDictionary(k => k.Key, v => UnicodeConverter.ToAscii(v.Value.ToString())));
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
            try
            {
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
                        bool fieldValueExists = pluItem.ContainsKey(field.Value);

                        if (fieldValueExists)
                        {
                            // Get the value from pluItem and set it to the product, converting it to the correct type

                            var value = pluItem[field.Value];
                            var propertyType = propertyWithAttribute.PropertyType;

                            if (propertyType == typeof(bool))
                            {
                                // The field in BooleanVals matches the field name, the value is what is constitutes a true value
                                var trueValues = booleanVals[field.Key];
                                var isTrue = trueValues == value;
                                propertyWithAttribute.SetValue(product, isTrue);
                            }
                            else
                            {
                                if (!(propertyType == typeof(DateTime?) && string.IsNullOrWhiteSpace(value)))
                                {
                                    var convertedValue = Convert.ChangeType(value, propertyType);
                                    //if there is a null or whitespace then we go with our default values
                                    if (propertyType != typeof(string)
                                        || (propertyType == typeof(string) && !String.IsNullOrWhiteSpace((string)convertedValue)))
                                        propertyWithAttribute.SetValue(product, convertedValue);
                                }
                                else
                                {
                                    Logger.Trace($"DateTime? Field {field.Key} is blank, skipping");
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error converting PLU Record to TblProducts - {ex.Message}");
            }
            return product;
        }
    }
}
