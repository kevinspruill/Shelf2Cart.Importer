using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Module.Generic.Helpers;
using Importer.Module.Generic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Generic.Parser
{
    public class TextParser : IParser
    {
        private string _fieldDelimiter { get; set; }
        private string _recordSeparator { get; set; }
        private tblProducts ProductTemplate { get; set; }
        private ICustomerProcess _customerProcess { get; set; }
        public List<Dictionary<string, string>> PLURecords { get; private set; } = new List<Dictionary<string, string>>();
        public List<Dictionary<string, string>> DeletedPLURecords { get; private set; } = new List<Dictionary<string, string>>();

        public Dictionary<string, string> mappedFields { get; }
        public Dictionary<string, string> booleanVals { get; }

        public TextParser(tblProducts productTemplate, ICustomerProcess customerProcess = null, string fieldDelimiter = "\t", string recordSeparator = "\r\n")
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();
            _fieldDelimiter = fieldDelimiter;
            _recordSeparator = recordSeparator;

            FieldMapLoader.Initialize();
            mappedFields = FieldMapLoader.FieldMap;

            BooleanMapLoader.Initialize();
            booleanVals = BooleanMapLoader.BooleanVals;
        }

        public void ParseFile(string filePath)
        {
            // read file as a tab delimited file with a header, add each record to a dictionary

            var lines = System.IO.File.ReadAllText(filePath).Split(new[] { _recordSeparator }, StringSplitOptions.RemoveEmptyEntries);
            // get the header
            var header = lines[0].Split(_fieldDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            // get the records
            try
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    var record = lines[i].Split(_fieldDelimiter.ToCharArray());
                    var recordDict = new Dictionary<string, string>();
                    for (int j = 0; j < header.Length; j++)
                    {//TODO Configurable line feed removal
                        recordDict[header[j]] = record[j].Trim();
                    }
                    PLURecords.Add(recordDict);
                    Console.WriteLine("Parsed line number " + i + " - " + recordDict["inv_receiptalias"].ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public List<tblProducts> ConvertPLURecordsToTblProducts()
        {
            var products = new List<tblProducts>();
            foreach (var pluItem in PLURecords)
            {
                var product = ConvertPLURecordToTblproducts(pluItem);
                if (IsValidProduct(product))
                {
                    products.Add(product);
                    Logger.Info($"Added {product.PLU} - {product.Description1} {product.Description2}".Trim());
                }
            }
            return products;
        }

        private bool IsValidProduct(tblProducts product)
        {
            if (string.IsNullOrWhiteSpace(product.PLU))
            {
                Logger.Warn("Empty PLU, skipping record...");
                return false;
            }

            return true;
        }

        public List<tblProducts> ConvertPLUDeleteRecordsToTblProducts()
        {
            var products = new List<tblProducts>();
            foreach (var pluItem in DeletedPLURecords)
            {
                var product = ConvertPLURecordToTblproducts(pluItem);
                if (IsValidProduct(product))
                    products.Add(product);
            }
            return products;
        }
        private tblProducts ConvertPLURecordToTblproducts(Dictionary<string, string> pluItem)
        {
            var product = ProductTemplate.Clone();

            // Create a dictionary to map the fields
            
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
                            //Logger.Trace($"Converting {field.Value} to {field.Key}");
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
                                var convertedValue = Convert.ChangeType(value, propertyType);
                                //if there is a null or whitespace then we go with our default values
                                if (propertyType != typeof(string)
                                    || (propertyType == typeof(string) && !String.IsNullOrWhiteSpace((string)convertedValue)))
                                    propertyWithAttribute.SetValue(product, convertedValue);
                            }

                        }
                    }
                }
            } catch (Exception ex)
            {
                Logger.Error(ex.InnerException.Message);
            }

            return product;
        }
    }
}
