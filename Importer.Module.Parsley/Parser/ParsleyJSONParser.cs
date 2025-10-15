using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Common.Services;
using Importer.Module.Parsley.Helpers;
using Importer.Module.Parsley.Models;
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

namespace Importer.Module.Parsley.Parser
{
    public class ParsleyJSONParser
    {
        //copied code from Generic JSON Parser
        private tblProducts ProductTemplate { get; set; }
        private ICustomerProcess _customerProcess { get; set; }
        public List<Dictionary<string, string>> PLURecords { get; private set; } = new List<Dictionary<string, string>>();
        List<MenuItemDetails> menuItemsToUpdate = new List<MenuItemDetails>();
        public ParsleySettingsLoader Settings { get; set; } = new ParsleySettingsLoader();
        MerchandiserAPIClient _restClient;

        public string APIKey { get; set; } = string.Empty; //Set this to your API Key

        public ParsleyJSONParser(tblProducts productTemplate, ICustomerProcess customerProcess = null)
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();
            _restClient = new MerchandiserAPIClient();
            //Set API Key
            _restClient.APIClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.ApiKey);

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

            }

            foreach (var item in filteredItems)
            {
                if (!string.IsNullOrWhiteSpace(item.ItemNumber))
                    menuItemsToUpdate.Add(GetMenuItemDetails(item.Id).Result);
            }
        }

        public async Task<MenuItemDetails> GetMenuItemDetails(int id)
        {
            _restClient.APIClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.ApiKey);

            var jsonData = await _restClient.GetAsync($"https://app.parsleycooks.com/api/public/menu_items/{id}");
            var deserializedJson = JsonConvert.DeserializeObject<MenuItemDetails>(jsonData);
            Logger.Info($"Retrieved full Menu Item Details for id {id} - {deserializedJson.Name}");
            return deserializedJson;
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
        public List<tblProducts> ConvertPLUDeleteRecordsToTblProducts()
        {   //We do not use deletes in Parsley, but necessary for interface
            var products = new List<tblProducts>();
            return products;
        }
        private tblProducts ConvertPLURecordToTblproducts(Dictionary<string, string> pluItem)
        {
            var product = ProductTemplate.Clone();

            product.Description1 = GetValue(pluItem, "name");

            //barcode is based on the PLU
            if (!string.IsNullOrWhiteSpace(GetValue(pluItem, "itemNumber")) && GetValue(pluItem, "itemNumber").Length == 5)
            {
                product.PLU = GetValue(pluItem, "itemNumber");
                product.Barcode = "2" + GetValue(pluItem, "itemNumber");
                product.Description11 = string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(GetValue(pluItem, "itemNumber")) && GetValue(pluItem, "itemNumber").Length == 12)
            {   //some items have a longer UPC, so Barcode will be zeroed out and Description11 will get the entire thing
                product.PLU = GetValue(pluItem, "itemNumber").Substring(4);
                product.Barcode = "000000";
                product.Description11 = GetValue(pluItem, "itemNumber");
            }

            ////TODO Confirm the ServingSize vs nutritionServingSize and how we care about weight
            product.NFServingSize = $"{GetValue(pluItem, "servingSizeAmount")} {GetValue(pluItem, "servingSizeUom")}";
            //product.NFServingSize = GetValue(pluItem, "nutritionServingSize");

            if (!string.IsNullOrWhiteSpace(GetValue(pluItem, "shelfLife").ToString()))
                product.ShelfLife = (int.Parse(GetValue(pluItem, "shelfLife"))-1).ToString();

            product = MapNutrientInfo(pluItem, product);

            if (GetValue(pluItem, "allergenString") != null && !string.IsNullOrWhiteSpace(GetValue(pluItem, "allergenString")))
                product.Description10 = $"CONTAINS: {GetValue(pluItem, "allergenString")}";

            if (GetValue(pluItem, "ingredients") != null && !string.IsNullOrWhiteSpace(GetValue(pluItem, "ingredients")))
                product.Ingredients = $"INGREDIENTS: {GetValue(pluItem, "ingredients")}";

            //TODO Need to fill this in
            product = MapCustomTags(pluItem, product);

            //Convert non-required fields
            product.Description2 = GetValue(pluItem, "subtitle");
            var tempPrice = GetValue(pluItem, "price");
            if (tempPrice != null && !string.IsNullOrWhiteSpace(tempPrice))
                product.Price = GetValue(pluItem, "price");
            else
                Logger.Warn($"Invalid price for {product.PLU}, using default");

                product.Description14 = GetValue(pluItem, "heatingInstructionOven");
            product.Description14 += GetValue(pluItem, "heatingInstructionMicrowave");

            ////TODO Confirm that IsPackaged is Scaleable
            //product.Scaleable = bool.Parse(GetValue(pluItem, "isPackaged"));

            return product;
        }

        public tblProducts MapCustomTags(Dictionary<string, string> pluItem, tblProducts product)
        {
            //TODO loop through the custom tags
            return product;
        }

        public tblProducts MapNutrientInfo(Dictionary<string, string> pluItem, tblProducts product)
        {
            //TODO set all the nutrient info, I need to know more about the names that are used by Ya Hala
            foreach (var item in pluItem)
            {
                switch (item.Key)
                {
                    case "calories":
                        product.NFCalories = item.Value;
                        break;
                    case "totalFat":
                        product.NFTotalFatG = item.Value;
                        break;
                    case "saturatedFat":
                        product.NFSatFatG = item.Value;
                        break;
                    case "cholesterol":
                        product.NFCholesterolMG = item.Value;
                        break;
                    case "sodium":
                        product.NFSodiumMG = item.Value;
                        break;
                    case "total_carbohydrate":
                        product.NFTotCarboG = item.Value;
                        break;
                    case "dietary_fiber":
                        product.NFDietFiber = item.Value;
                        break;
                    case "total_sugars":
                        product.NFSugars = item.Value;
                        break;
                    case "protein":
                        product.NFProtein = item.Value;
                        break;
                    case "vitamin_a":
                        product.NFVitA = item.Value;
                        break;
                    case "vitamin_c":
                        product.NFVitC = item.Value;
                        break;
                    case "calcium":
                        product.NFCalciummcg = item.Value;
                        break;
                    case "iron":
                        product.NFIronmcg = item.Value;
                        break;
                    case "added_sugar":
                        product.NFSugarsAddedG = item.Value;
                        break;
                    case "vitamin_d":
                        product.NFVitDmcg = item.Value;
                        break;
                    case "potassium":
                        product.NFPotassiummcg = item.Value;
                        break;
                    //TODO determine where we will place "phosphorus", as maybe NF9 to NF20
                    default:
                        break;
                }
            }
            return product;
        }

        public string GetValue(Dictionary<string, string> pluItem, string key)
        {
            if (pluItem.TryGetValue(key, out string value))
                return value;
            else
                return string.Empty;
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
                record.Add("description", item.Description);

                //TODO Confirm the ServingSize vs nutritionServingSize and how we care about weight
                record.Add("servingSizeAmount", item.NutritionalInfo.ServingSize.Amount.ToString());
                record.Add("servingSizeUom", item.NutritionalInfo.ServingSize.Uom);
                record.Add("nutritionServingSize", item.NutritionalInfo.NutritionServingSize);
                record.Add("servingsPerPackage", item.NutritionalInfo.ServingsPerPackage.ToString());

                if (item.ShelfLife != null)
                    record.Add("shelfLife", item.ShelfLife.ToString());

                //No need for a whole other method, we can just combine value and unit to make our value
                foreach (var nutrient in item.NutritionalInfo.Nutrients.Values)
                {
                    record.Add(nutrient.Name, nutrient.Value.ToString()); //not including the unit as we add that
                }

                //Ya Hala fills out an allergen string but they only want their "Top 9 Allergens" so we're not using it
                //keeping this here and commented out though as another customer using Parsley might actually use all the allergens
                /*if (item.NutritionalInfo.AllergenString != null && !string.IsNullOrWhiteSpace(item.NutritionalInfo.AllergenString))
                {
                    record.Add("allergenString", item.NutritionalInfo.AllergenString.Replace("CEREALS CONTAINING GLUTEN", "GLUTEN"));
                }
                else
                {*/
                    var tempAllergenString = "";
                    if (item.NutritionalInfo.Allergens.CerealsGluten != null && (bool)item.NutritionalInfo.Allergens.CerealsGluten)
                        tempAllergenString += "GLUTEN, ";
                    if (item.NutritionalInfo.Allergens.CrustaceanShellfish != null && !string.IsNullOrWhiteSpace(item.NutritionalInfo.Allergens.CrustaceanShellfish))
                        tempAllergenString += $"{item.NutritionalInfo.Allergens.CrustaceanShellfish.ToUpper()}, ";
                    if (item.NutritionalInfo.Allergens.Eggs != null && (bool)item.NutritionalInfo.Allergens.Eggs)
                        tempAllergenString += "EGGS, ";
                    if (item.NutritionalInfo.Allergens.Fish != null && !string.IsNullOrWhiteSpace(item.NutritionalInfo.Allergens.Fish))
                        tempAllergenString += "FISH, ";
                    if (item.NutritionalInfo.Allergens.Milk != null && (bool)item.NutritionalInfo.Allergens.Milk)
                        tempAllergenString += "MILK, ";
                    if (item.NutritionalInfo.Allergens.Peanuts != null && (bool)item.NutritionalInfo.Allergens.Peanuts)
                        tempAllergenString += "PEANUTS, ";
                    if (item.NutritionalInfo.Allergens.SesameSeeds != null && (bool)item.NutritionalInfo.Allergens.SesameSeeds)
                        tempAllergenString += "SESAME SEEDS, ";
                    if (item.NutritionalInfo.Allergens.Soybeans != null && (bool)item.NutritionalInfo.Allergens.Soybeans)
                        tempAllergenString += "SOYBEANS, ";
                    if (item.NutritionalInfo.Allergens.TreeNuts != null && !string.IsNullOrWhiteSpace(item.NutritionalInfo.Allergens.TreeNuts))
                        tempAllergenString += $"{item.NutritionalInfo.Allergens.TreeNuts.ToUpper()}, ";

                    if (tempAllergenString.Length > 0)
                    {
                        tempAllergenString = tempAllergenString.Substring(0, tempAllergenString.Length - 2);
                        record.Add("allergenString", tempAllergenString);
                    }
                    else 
                    {
                        record.Add("allergenString", string.Empty);
                    }
                //}

                record.Add("ingredients", item.NutritionalInfo.Ingredients);
                if (item.CustomTags != null)
                {
                    foreach (var tag in item.CustomTags)
                    {
                        //TODO We might add an S2C prefix to ensure we only grab certain custom tags
                        record.Add(tag.Key, tag.Value.ToString());
                    }
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
    }
}
