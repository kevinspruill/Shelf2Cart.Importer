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

        public async Task<MenuItemDetails> GetMenuItemDetails(int id)
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

            //TODO Accidentally hardcode mapped in ConvertMenuItem to PLURecord, so putting my accidental mapping here
            //record.PLU = item.Id.ToString();
            //record.Description1 = item.Name;

            ////TODO Confirm the ServingSize vs nutritionServingSize and how we care about weight
            //record.NetWt = $"{item.NutritionalInfo.ServingSize.Amount} {item.NutritionalInfo.ServingSize.Uom}";
            //record.NFServingSize = item.NutritionalInfo.NutritionServingSize;

            //SetNutrientInfo(record, item);

            ////TODO allergensString seems unreliable according to the example, we should
            //record.Description11 = "Contains: ";
            //if ((bool)item.NutritionalInfo.Allergens.Milk)
            //    record.Description11 += "Milk, ";
            //if ((bool)item.NutritionalInfo.Allergens.Eggs)
            //    record.Description11 += "Eggs, ";
            //if ((bool)item.NutritionalInfo.Allergens.Wheat)
            //    record.Description11 += "Wheat, ";
            //if ((bool)item.NutritionalInfo.Allergens.Peanuts)
            //    record.Description11 += "Peanuts, ";
            //if ((bool)item.NutritionalInfo.Allergens.Soybeans)
            //    record.Description11 += "Soybeans, ";
            //if ((bool)item.NutritionalInfo.Allergens.Molluscs)
            //    record.Description11 += "Molluscs, ";
            //if ((bool)item.NutritionalInfo.Allergens.CerealsGluten)
            //    record.Description11 += "Gluten, ";
            //if ((bool)item.NutritionalInfo.Allergens.Celery)
            //    record.Description11 += "Celery, ";
            //if ((bool)item.NutritionalInfo.Allergens.Mustard)
            //    record.Description11 += "Mustard, ";
            //if ((bool)item.NutritionalInfo.Allergens.SesameSeeds)
            //    record.Description11 += "Sesame Seeds, ";
            //if ((bool)item.NutritionalInfo.Allergens.SulphurDioxideSulphites)
            //    record.Description11 += "Sulphur Dioxide Sulphites, ";
            //if ((bool)item.NutritionalInfo.Allergens.Lupin)
            //    record.Description11 += "Lupin, ";
            //if (item.NutritionalInfo.Allergens.Fish.Length > 0)
            //    record.Description11 += item.NutritionalInfo.Allergens.Fish;
            //if (item.NutritionalInfo.Allergens.CrustaceanShellfish.Length > 0)
            //    record.Description11 += item.NutritionalInfo.Allergens.CrustaceanShellfish;
            //if (item.NutritionalInfo.Allergens.TreeNuts.Length > 0)
            //    record.Description11 += item.NutritionalInfo.Allergens.TreeNuts;

            //if (record.Description11 == "Contains: ")
            //    record.Description11 = string.Empty;
            //else
            //    record.Description11 = record.Description11.Substring(0, record.Description11.Length - 3);
            ////end allergen 

            //record.Ingredients = item.NutritionalInfo.Ingredients;
            //foreach (var tag in item.CustomTags)
            //{
            //    //TODO Use the tags
            //}

            ////Convert non-required fields
            //record.Description2 = item.Subtitle;
            //record.Price = item.Price.ToString();


            //if (item.HeatingInstructionOven.Length > 0)
            //    record.Description14 += item.HeatingInstructionOven + "\n\n";

            //if (item.HeatingInstructionMicrowave.Length > 0)
            //    record.Description14 += item.HeatingInstructionMicrowave;

            ////TODO Confirm that IsPackaged is Scaleable
            //record.Scaleable = item.NutritionalInfo.IsPackaged;
            //end accidental hardcode map

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
            foreach (var item in menuItemsToUpdate)
            {
                Dictionary<string, string> record = new Dictionary<string, string>();
                //Convert required fields
                record.Add("id", item.Id.ToString());
                record.Add("name", item.Name);

                //TODO Confirm the ServingSize vs nutritionServingSize and how we care about weight
                record.Add("servingSizeAmount", item.NutritionalInfo.ServingSize.Amount.ToString());
                record.Add("servingSizeUom", item.NutritionalInfo.ServingSize.Uom);
                record.Add("nutritionServingSize", item.NutritionalInfo.NutritionServingSize);
                record.Add("servingsPerPackage", item.NutritionalInfo.ServingsPerPackage.ToString());

                SetNutrientInfo(record, item);

                //TODO allergensString seems unreliable according to the example, and these will always either be true/non-empty or null
                if (item.NutritionalInfo.Allergens.Milk != null)
                    record.Add("allergensMilk", item.NutritionalInfo.Allergens.Milk.ToString());
                if (item.NutritionalInfo.Allergens.Eggs != null)
                    record.Add("allergensEggs", item.NutritionalInfo.Allergens.Eggs.ToString());
                if (item.NutritionalInfo.Allergens.Wheat != null)
                    record.Add("allergensWheat", item.NutritionalInfo.Allergens.Wheat.ToString());
                if (item.NutritionalInfo.Allergens.Peanuts != null)
                    record.Add("allergensPeanuts", item.NutritionalInfo.Allergens.Peanuts.ToString());
                if (item.NutritionalInfo.Allergens.Soybeans != null)
                    record.Add("allergensSoybeans", item.NutritionalInfo.Allergens.Soybeans.ToString());
                if (item.NutritionalInfo.Allergens.Molluscs != null)
                    record.Add("allergensMolluscs", item.NutritionalInfo.Allergens.Molluscs.ToString());
                if (item.NutritionalInfo.Allergens.CerealsGluten != null)
                    record.Add("allergensCerealsGluten", item.NutritionalInfo.Allergens.CerealsGluten.ToString());
                if (item.NutritionalInfo.Allergens.Celery != null)
                    record.Add("allergensCelery", item.NutritionalInfo.Allergens.Celery.ToString());
                if (item.NutritionalInfo.Allergens.Mustard != null)
                    record.Add("allergensMustard", item.NutritionalInfo.Allergens.Mustard.ToString());
                if (item.NutritionalInfo.Allergens.SesameSeeds != null)
                    record.Add("allergensSesameSeeds", item.NutritionalInfo.Allergens.SesameSeeds.ToString());
                if (item.NutritionalInfo.Allergens.SulphurDioxideSulphites != null)
                    record.Add("allergensSulphurDioxideSulphites", item.NutritionalInfo.Allergens.SulphurDioxideSulphites.ToString());
                if (item.NutritionalInfo.Allergens.Lupin != null)
                    record.Add("allergensLupin", item.NutritionalInfo.Allergens.Lupin.ToString());
                if (item.NutritionalInfo.Allergens.Fish != null)
                    record.Add("allergensFish", item.NutritionalInfo.Allergens.Fish);
                if (item.NutritionalInfo.Allergens.CrustaceanShellfish != null)
                    record.Add("allergensCrustaceanShellfish", item.NutritionalInfo.Allergens.CrustaceanShellfish);
                if (item.NutritionalInfo.Allergens.TreeNuts != null)
                    record.Add("allergensTreeNuts", item.NutritionalInfo.Allergens.TreeNuts);

                //end allergen 

                record.Add("Ingredients", item.NutritionalInfo.Ingredients);
                foreach (var tag in item.CustomTags)
                {
                    //TODO We might add an S2C prefix to ensure we only grab certain custom tags
                    record.Add(tag.Key, tag.Value.ToString());
                }
                
                //Convert non-required fields
                record.Add("subtitle", item.Subtitle);
                record.Add("price", item.Price.ToString());
                
                
                if (item.HeatingInstructionOven != null)
                    record.Add("heatingInstructionOven", item.HeatingInstructionOven);

                if (item.HeatingInstructionMicrowave != null)
                    record.Add("heatingInstructionMicrowave", item.HeatingInstructionMicrowave);

                record.Add("isPackaged", item.NutritionalInfo.IsPackaged.ToString());

                //adding these to the record in case we have use for them
                record.Add("characteristicsMeat", item.NutritionalInfo.Characteristics.Meat.ToString());
                record.Add("characteristicsPork", item.NutritionalInfo.Characteristics.Pork.ToString());
                record.Add("characteristicsCorn", item.NutritionalInfo.Characteristics.Corn.ToString());
                record.Add("characteristicsPoultry", item.NutritionalInfo.Characteristics.Poultry.ToString());

                record.Add("itemNumber", item.ItemNumber);
                record.Add("managementItemNumber", item.ManagementItemNumber);
                record.Add("lastModified", item.LastModified.ToString());
                record.Add("isSubrecipe", item.IsSubrecipe.ToString()); //pretty sure that we can disregard if this is true

                PLURecords.Add(record);
            }

        }
        //end copied code

        public Dictionary<string, string> SetNutrientInfo(Dictionary<string, string> record, MenuItemDetails item)
        {
            foreach (var nutrient in item.NutritionalInfo.Nutrients.Values)
            {
                //TODO Match the name of the nutrients present with one that we have, though if they do
                switch (nutrient)
                {

                    default:
                        break;
                }
            }
            return record;
        }
    }
}
