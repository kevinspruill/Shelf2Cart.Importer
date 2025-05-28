using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Module.Parsley.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley.Parser
{
    public class ParsleyJSONParser
    {
        //copied code from Generic JSON Parser
        private tblProducts ProductTemplate { get; set; }
        private ICustomerProcess _customerProcess { get; set; }
        public List<Dictionary<string, string>> PLURecords { get; private set; } = new List<Dictionary<string, string>>();
        List<MenuItemDetails> menuItemsToUpdate = new List<MenuItemDetails>();
        RestAPIMonitor _restClient;

        public ParsleyJSONParser(tblProducts productTemplate, ICustomerProcess customerProcess = null)
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();
            _restClient = new RestAPIMonitor(new ParsleyModule());
        }

        public void ParseMenuItemSimpleList(string jsonString)
        {
            //Every time this method is called, in this process we will get the list of Menu Items (placed into MenuItemSimpleModel),
            //filter that list based on LastModifiedDate, and then for each record in the filtered list we query menu items by ID to
            //get MenuItemFullModel added to menuItemsToUpdate. Then we actually add to PLURecords using multiple methods

            //read the file
            var jsonData = jsonString;
            //deserialize into a List<Dictionary<string, object>> before sending to PLURecords
            var deserializedJson = JsonConvert.DeserializeObject<List<MenuItemSimple>>(jsonData);

            List<MenuItemSimple> filteredItems = new List<MenuItemSimple>();
            foreach (var record in deserializedJson)
            {
                //TODO Check the LastModifiedTime of each record, and if it is past last check or if full load is triggered THEN
                //we add to filteredItems as below
                filteredItems.Add(record);

                //PLURecords.Add(record.ToDictionary(k => k.Key, v => v.Value.ToString()));
            }

            foreach (var item in filteredItems)
            {
                menuItemsToUpdate.Add(GetMenuItemDetails(item.Id).Result);

            }
        }

        public async Task<MenuItemDetails> GetMenuItemDetails(string id)
        {
            var jsonData = await _restClient.QueryEndpoint(); //TODO Refactor QueryEndpoint to send parameters
            var deserializedJson = JsonConvert.DeserializeObject<MenuItemDetails>(jsonData);

            return deserializedJson;
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
        {   //We do not use deletes in Parsley, but necessary for interface
            var products = new List<tblProducts>();
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
                            //if there is a null or whitespace then we go with our default values
                            if (propertyType != typeof(string)
                                || (propertyType == typeof(string) && !String.IsNullOrWhiteSpace((string)convertedValue)))
                                propertyWithAttribute.SetValue(product, convertedValue);
                        }

                    }
                }
            }

            return product;
        }

        public void ConvertMenuItemsToPLURecords()
        {
            //TODO This is where we convert MenuItemFullModel to a List<Dictionary<string, string>> PLURecords for use in
            //ConvertPLURecordsToTblProducts. See how Invafresh Module does it, think about how we can expand it later but not
            //go crazy right now (how Invafresh uses hardcoding and also a custom mapper)
        }
        //end copied code
    }
}
