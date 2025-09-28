using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Common.Services;
using Importer.Module.ECRS.ThirdPartyAPI.Helpers;
using Importer.Module.ECRS.ThirdPartyAPI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Importer.Module.ECRS.ThirdPartyAPI
{
    public class ECRSThirdPartyAPIJSONParser
    {
        //copied code from Generic JSON Parser
        private tblProducts ProductTemplate { get; set; }
        private ICustomerProcess _customerProcess { get; set; }
        public List<ItemData> PLURecords { get; private set; } = new List<ItemData>();
        public Dictionary<string, string> mappedFields { get; }

        public ECRSThirdPartyAPIJSONParser(tblProducts productTemplate, ICustomerProcess customerProcess = null)
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();

            // Initialize field mappings
            FieldMapLoader.Initialize();
            mappedFields = FieldMapLoader.FieldMap;
        }

        public void ParseFile(string jsonFilePath)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = { new StringEnumConverter() } // respects [EnumMember(Value="...")]
                };

                // read json file
                var jsonString = System.IO.File.ReadAllText(jsonFilePath);

                // Root is an array of items in this API.
                PLURecords = JsonConvert.DeserializeObject<List<ItemData>>(jsonString, settings);
            }
            catch (JsonException ex)
            {
                Logger.Error($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error: {ex.Message}");
            }
        }

        public List<tblProducts> ConvertPLURecordsToTblProducts()
        {
            var products = new List<tblProducts>();

            foreach (var pluItem in PLURecords)
            {
                // This uses the first store if multiple are present
                var product = ConvertPLURecordToTblproducts(pluItem);
                if (product != null)
                    products.Add(product);

                // Note: This code supports multiple stores per item, creating separate products.
                // TODO: Decide if this is desired behavior for ECRS Third Party API integration.
                // TODO: Also, add option to choose specific store.

                // If store-level data exists, create a product per store; otherwise single product
                
                //if (pluItem.Stores != null && pluItem.Stores.Count > 0)
                //{
                //    foreach (var store in pluItem.Stores)
                //    {
                //        var product = ConvertPLURecordToTblproducts(pluItem, store);
                //        if (!string.IsNullOrWhiteSpace(product.PLU))
                //            products.Add(product);
                //    }
                //}
                //else
                //{
                //    var product = ConvertPLURecordToTblproducts(pluItem, null);
                //    if (!string.IsNullOrWhiteSpace(product.PLU))
                //        products.Add(product);
                //}
            }

            return products;
        }

        private static readonly Dictionary<string, PropertyInfo> _productPropsByAttr = typeof(tblProducts)
            .GetProperties()
            .Select(p => new
            {
                Prop = p,
                Attr = (ImportDBFieldAttribute)p
                    .GetCustomAttributes(typeof(ImportDBFieldAttribute), false)
                    .FirstOrDefault()
            })
            .Where(x => x.Attr != null)
            .GroupBy(x => x.Attr.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Prop, StringComparer.OrdinalIgnoreCase);

        
        private tblProducts ConvertPLURecordToTblproducts(ItemData pluItem, StoreData store)
        {
            // Custom processing for the ECRS Third Party API Raw Data, before mapping and conversion to tblProduct record, uses ItemData object
            if (_customerProcess != null && _customerProcess.Name != "Importer Base Processor")
                pluItem = _customerProcess.DataFileCondtioning(pluItem);
            
            if (pluItem == null)
                return null;

            var product = ProductTemplate.Clone();

            foreach (var kvp in mappedFields)
            {
                var attrName = kvp.Key?.Trim();
                var sourcePropName = kvp.Value?.Trim();
                bool useStoreLevel = false;

                if (string.IsNullOrEmpty(attrName) || string.IsNullOrEmpty(sourcePropName))
                    continue;

                if (!_productPropsByAttr.TryGetValue(attrName, out var targetProp))
                    continue;

                // If SourcePropName Starts with "StoreData.", it indicates a store-level field
                if (sourcePropName.StartsWith("StoreData.", StringComparison.OrdinalIgnoreCase))
                {
                    sourcePropName = sourcePropName.Substring(10); // Remove "StoreData." prefix
                    // flag to indicate store-level property
                    useStoreLevel = true;

                    if (store == null)
                        continue; // No store data available
                }

                // Try to resolve value first from ItemData, then from StoreData
                object rawValue = GetDirectPropertyValue(pluItem, sourcePropName);

                if ((rawValue == null && store != null) || useStoreLevel)
                    rawValue = GetDirectPropertyValue(store, sourcePropName);

                if (rawValue == null)
                {
                    continue;
                }

                object finalValue = rawValue;
                var targetType = targetProp.PropertyType;
                var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

                try
                {
                    if (!underlying.IsInstanceOfType(rawValue))
                    {
                        if (underlying.IsEnum && rawValue is string sEnum)
                        {
                            finalValue = Enum.Parse(underlying, sEnum, true);
                        }
                        else if (underlying == typeof(bool))
                        {
                            if (rawValue is string sBool)
                                finalValue = sBool == "1" || sBool.Equals("true", StringComparison.OrdinalIgnoreCase) || sBool.Equals("Y", StringComparison.OrdinalIgnoreCase);
                            else
                                finalValue = Convert.ToBoolean(rawValue);
                        }
                        else if (underlying == typeof(DateTime) && rawValue is string sDate)
                        {
                            if (DateTime.TryParse(sDate, out var dt))
                                finalValue = dt;
                            else
                                Logger.Warn($"Invalid DateTime '{sDate}'");
                        }
                        else
                        {
                            finalValue = Convert.ChangeType(rawValue, underlying);
                        }
                    }
                    targetProp.SetValue(product, finalValue);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Mapping failed for '{attrName}' from '{sourcePropName}': {ex.Message}");
                }
            }

            // conditions to send a null product back, which will be skipped, this is independent of any customer processing
            if (product.PLU.Length < 1
                || (false)
                || (false)
                )
            {
                Logger.Warn($"Skipping product with PLU '{product.PLU}' due to missing required or malformed data");
                return null;
            }

            // Custom processing for the ECRS Third Party API Data, after mapping and convertion to tblProduct record
            // This can return null to skip the record
            if (_customerProcess != null && _customerProcess.Name != "Importer Base Processor")
                product = _customerProcess.DataFileCondtioning(product);

            return product;
        }

        // Uses first store if present.
        private tblProducts ConvertPLURecordToTblproducts(ItemData pluItem)
            => ConvertPLURecordToTblproducts(pluItem, pluItem.Stores?.FirstOrDefault());

        private tblProducts ConvertPLURecordToTblproducts(ItemData pluItem, string storeNumber)
        {
            // Find the store matching the given store number
            var store = pluItem.Stores?.FirstOrDefault(s => s.StoreNumber == storeNumber);
            return ConvertPLURecordToTblproducts(pluItem, store);
        }

        private object GetDirectPropertyValue(object source, string propName)
        {
            if (source == null || string.IsNullOrEmpty(propName))
                return null;

            // Get property from JsonProperty attribute if present
            var prop = source.GetType().GetProperties()
                .FirstOrDefault(p =>
                {
                    var jpAttr = (JsonPropertyAttribute)p
                        .GetCustomAttributes(typeof(JsonPropertyAttribute), false)
                        .FirstOrDefault();
                    return (jpAttr != null && jpAttr.PropertyName.Equals(propName, StringComparison.OrdinalIgnoreCase))
                        || p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase);
                });
            return prop?.GetValue(source);
        }
    }
}
