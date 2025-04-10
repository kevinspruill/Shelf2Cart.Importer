using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Module.Generic.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Generic.Parser
{
    public class TextParser
    {
        private string _fieldDelimiter { get; set; }
        private tblProducts ProductTemplate { get; set; }
        private ICustomerProcess _customerProcess { get; set; }
        public List<Dictionary<string, string>> PLURecords { get; private set; } = new List<Dictionary<string, string>>();
        public List<Dictionary<string, string>> DeletedPLURecords { get; private set; } = new List<Dictionary<string, string>>();

        public TextParser(tblProducts productTemplate, ICustomerProcess customerProcess = null)
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();
        }

        public void ParseFile(string filePath)
        {
            // read file as a tab delimited file with a header, add each record to a dictionary
            var lines = System.IO.File.ReadAllLines(filePath);
            // get the header
            var header = lines[0].Split(_fieldDelimiter.ToCharArray());
            // get the records
            for (int i = 1; i < lines.Length; i++)
            {
                var record = lines[i].Split(_fieldDelimiter.ToCharArray());
                var recordDict = new Dictionary<string, string>();
                for (int j = 0; j < header.Length; j++)
                {
                    recordDict[header[j]] = record[j];
                }
                PLURecords.Add(recordDict);
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
