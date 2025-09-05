using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Module.ECRS.Enums;
using Microsoft.Win32.SafeHandles;
using SimpleImpersonation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using UncleG.CustomerProcess.Models;

namespace UncleG.CustomerProcess
{
    public class UncleGProcess : ICustomerProcess
    {
        public string Name => "Uncle G's";
        public DepartmentPLUPadder _departmentPLUPadder = new DepartmentPLUPadder();
        public Dictionary<string, object> Settings { get; set; }
        public Dictionary<string, object> CategoryDictionary { get; set; } = new Dictionary<string, object>();
        public bool ForceUpdate { get; set; } = false; // Flag to force update to ProcessDatabase

        public ECRSQueryType QueryType = ECRSQueryType.Full; // Default to Full, will be set in PreProductProcess based on ImporterInstance

        private bool _updateHierarchy = false; // Flag to indicate if the hierarchy should be updated

        private DatabaseHelper DatabaseHelper = new DatabaseHelper(DatabaseType.ImportDatabase);

        public UncleGProcess()
        {
            _departmentPLUPadder = new DepartmentPLUPadder();
            // get current directory
            string currentDirectory = Directory.GetCurrentDirectory();

            // load Settings json file, from current directory
            Settings = jsonLoader.LoadSettings(Path.Combine(Directory.GetCurrentDirectory(), "Customer.Settings"), "UncleG.Settings.json");

        }

        public void ProcessData()
        {

        }
        
        public void Initialize()
        {

        }

        public void Stop()
        {

        }

        public void PreQueryRoutine()
        {
            Logger.Info($"{Name} PreQueryRoutine");
        }

        public T DataFileCondtioning<T>(T ImportData = null) where T : class
        {
            Logger.Info($"{Name} DataFileCondtioning");
            return ImportData;
        }

        public tblProducts PreProductProcess(tblProducts product)
        {
            var item = product;

            // Handle the double new lien in the Ingredients field
            item.Ingredients = item.Ingredients.Replace("\n\n", "<S>");

            // if originalPLU starts with "3838100" remove it so we can get the right department padding
            var pluString = product.PLU;
            if (pluString.StartsWith("3838100") && pluString.Length == 12)
            {
                Logger.Trace($"3838100 PLU found, removing prefix");

                // remove the first 7 characters
                pluString = pluString.Substring(7);

                product.PLU = pluString;
            }

            // update CategoryNum from the CategoryDictionary, where Description 11 is the key
            item.CategoryNum = CategoryDictionary.ContainsKey(item.Description11) ? CategoryDictionary[item.Description11].ToString() : "0";
            Logger.Trace($"Category Num: {item.CategoryNum} for {item.Description11}");

            return item;
        }

        public void PreProductProcess()
        {
            Logger.Info($"{Name} PreProductProcess");

            if (QueryType == ECRSQueryType.Full)
            {
                // If the query type is FULL, we need to update the hierarchy
                _updateHierarchy = true;
            }

            Logger.Info("Creating Department Hierarchy");
            CategoryDictionary = BuildDepartmentHierarchy(LoadFromCsv(Settings["csvPath"].ToString()).Result);
        }

        public tblProducts ProductProcessor(tblProducts product)
        {
            Logger.Trace($"{Name} ProductProcessor: {product.Description1} {product.Description2}");

            // if originalPLU starts with "3838100" remove it and check digit, then pad with 1
            var pluString = product.Description11;
            if (pluString.StartsWith("3838100") && pluString.Length == 12)
            {
                Logger.Trace($"3838100 item found with PLU {pluString}, removing prefix and check digit, then padding with 1");

                // remove the first 7 characters
                pluString = pluString.Substring(7);

                // remove the last digit
                pluString = pluString.Substring(0, pluString.Length - 1);

                //get the department padding from the already padded but incorrect PLU
                var deptPadding = product.PLU.Substring(0, 2);

                product.PLU = deptPadding + "001" + pluString;
            }

            var _tempProduct = product;

            _tempProduct = ProcessIngredientSplitter(_tempProduct);
            _tempProduct = ProcessNetWt(_tempProduct);

            // if Description2 is empty, and Description5 is not empty, copy Description5 to Button2
            if (string.IsNullOrEmpty(_tempProduct.Description2) && !string.IsNullOrEmpty(_tempProduct.Description5))
            {
                _tempProduct.Button2 = _tempProduct.Description5;
            }

            return _tempProduct;
        }

        public void PostProductProcess()
        {
            Logger.Info($"{Name} PostProductProcess");
        }

        public void PostQueryRoutine()
        {
            Logger.Info($"{Name} PostQueryRoutine");
        }

        #region Custom Methods

        private tblProducts ProcessNetWt(tblProducts tempProduct)
        {
            string _weight = tempProduct.NetWt.ToUpper().Trim();
            // if the product has a weight, and it has "OZ" or "FZ" in the field, extract the numeric value out of the field
            if (_weight.Contains("OZ") || _weight.Contains("FZ"))
            {
                // extract the numeric value from the field
                _weight = _weight.Replace("OZ", "").Replace("FZ", "").Trim().Trim('.').Trim();

                tempProduct.NetWt = _weight;
            }
            else
            {
                tempProduct.NetWt = "";
            }

            return tempProduct;
        }
        public tblProducts ProcessIngredientSplitter(tblProducts product)
        {
            var _tempProduct = product;

            // remove any leading and trailing " and spaces
            _tempProduct.Ingredients = _tempProduct.Ingredients.Trim().Trim('"');

            // if the ingredients array has ":"
            if (_tempProduct.Ingredients.Contains(":"))
            {
                string[] _ingredients = _tempProduct.Ingredients.Split(new string[] { "<S>" }, StringSplitOptions.RemoveEmptyEntries);

                if (_ingredients.Length > 1)
                {
                    foreach (string _ingredient in _ingredients)
                    {
                        _tempProduct = ProcessIngredientSection(_tempProduct, _ingredient);
                    }
                }
                else
                {
                    _tempProduct = ProcessIngredientSection(_tempProduct, _tempProduct.Ingredients);
                }

            }

            _tempProduct.Ingredients = _tempProduct.Ingredients.Replace("<S>", "");

            return _tempProduct;
        }
        private tblProducts ProcessIngredientSection(tblProducts _tempProduct, string _ingredient)
        {
            // Handle if the ingredient section does not have a colon
            if (!_ingredient.Contains(":"))
            {
                // create a switch statement to determine which ingredient section is being processed
                switch (_ingredient.ToUpper())
                {
                    case "FREEZE IF NOT USING IMMEDIATELY":
                    case "KEEP REFRIGERATED":
                        _tempProduct.Description12 = _ingredient;
                        break;
                    case "PRINTTIME":
                        _tempProduct.Description7 = _ingredient;
                        break;
                    default:
                        // append the ingredient section to the Description14 field
                        _tempProduct.Description14 += " " + _ingredient;
                        break;
                }
                _tempProduct.Ingredients = _tempProduct.Ingredients.Replace(_ingredient, "");

                return _tempProduct;
            }

            string[] _ingredientHeaders = _ingredient.Split(new string[] { ":" }, StringSplitOptions.None);
            // create a switch statement to determine which part of the ingredient is being processed
            switch (_ingredientHeaders[0])
            {
                case "INGREDIENTS":
                    _tempProduct.Ingredients = _ingredient;
                    break;
                case "ALLERGENS":
                    _tempProduct.Description10 = _ingredient;
                    _tempProduct.Ingredients = _tempProduct.Ingredients.Replace(_ingredient, "");
                    break;
                default:
                    // append the ingredient section to the Description14 field
                    _tempProduct.Description14 += " " + _ingredient;
                    _tempProduct.Ingredients = _tempProduct.Ingredients.Replace(_ingredient, "");
                    break;
            }

            return _tempProduct;
        }
        public tblProducts ProcessBarcode(tblProducts product)
        {
            var _tempProduct = product;

            // If the product.plu is 5 digits, it can be copied to the barcode field
            if (_tempProduct.PLU.ToString().Trim().Length == 5)
            {
                _tempProduct.Barcode = _tempProduct.PLU.ToString().Trim();
                // If the barcode is less than 5 digits, it should be padded with zeros
                while (_tempProduct.Barcode.Trim().Length < 5)
                {
                    _tempProduct.Barcode = "0" + _tempProduct.Barcode;
                }

                // copy the barcode to the to Description11 field
                _tempProduct.Description11 = _tempProduct.Barcode.Trim();

            }
            else if (_tempProduct.PLU.ToString().Trim().Length > 5)
            {
                // If the product.plu is more than 5 digits, the barcode field should be empty
                _tempProduct.Description11 = _tempProduct.PLU.ToString().Trim();
            }

            return _tempProduct;
        }

        /// <summary>
        /// Loads RawRow objects from a CSV file containing a header row.
        /// Assumes columns are: Item_ID, Item_Name1, Major_Dept, Category, Sub_Category
        /// and each row has 5 comma-separated fields.
        /// </summary>
        /// <param name="csvPath">Full path to the CSV file</param>
        /// <returns>List of RawRow objects parsed from the CSV</returns>
        public async Task<List<RawRow>> LoadFromCsv(string csvPath)
        {
            var results = new List<RawRow>();

            // Network share credentials
            string networkPath = csvPath;
            string domain = Settings["domain"].ToString();
            string username = Settings["username"].ToString();
            string password = Settings["password"].ToString();

            string localCopyPath = Path.Combine(Directory.GetCurrentDirectory(), "UncleG.Departments.csv");

            try
            {
                UserCredentials credentials = new UserCredentials(domain, username, password);

                using (SafeAccessTokenHandle userHandle = credentials.LogonUser(LogonType.NewCredentials))
                {
                    await WindowsIdentity.RunImpersonated(userHandle, async () =>
                    {
                        Logger.Info("Impersonation successful. Attempting to access network share...");

                        try
                        {
                            if (File.Exists(localCopyPath))
                            {
                                DateTime networkFileDate = File.GetLastWriteTime(csvPath);
                                DateTime localFileDate = File.GetLastWriteTime(localCopyPath);

                                if (networkFileDate > localFileDate)
                                {
                                    // compare contents of the two files  using File.ReadAllText
                                    string networkFileContent = File.ReadAllText(csvPath);
                                    string localFileContent = File.ReadAllText(localCopyPath);

                                    if (networkFileContent != localFileContent)
                                    {
                                        // if the contents are different, copy the network file to the local path
                                        File.Copy(csvPath, localCopyPath, true);
                                        Logger.Info("Network file is newer and different. Copied to local path.");
                                        _updateHierarchy = true;
                                    }
                                    else
                                    {
                                        Logger.Info("Network file is newer but identical. No action taken.");
                                    }
                                }
                                else
                                {
                                    Logger.Info("Local file is up to date. No action taken.");
                                }
                            }
                            else
                            {
                                File.Copy(csvPath, localCopyPath, true);
                                Logger.Info("Copied network file to local path.");
                                _updateHierarchy = true;
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Logger.Error("UnauthorizedAccessException: " + ex.Message);
                        }
                        catch (IOException ex)
                        {
                            Logger.Error("IOException: " + ex.Message);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Exception: " + ex.Message);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error copying file: " + ex.Message);
            }

            // Read the CSV file
            using (var reader = new StreamReader(localCopyPath))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) return results;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = ParseCsvLine(line);

                    if (parts.Length < 6)
                    {
                        continue;
                    }

                    var row = new RawRow
                    {
                        ItemID = parts[0].Trim(),
                        Item_Name1 = parts[1].Trim(),
                        Department__ = parts[2].Trim(),
                        Major_Dept = parts[3].Trim(),
                        Category = parts[4].Trim(),
                        Sub_Category = parts[5].Trim()
                    };

                    results.Add(row);
                }
            }

            return results;
        }
        private string[] ParseCsvLine(string line)
        {
            var parts = new List<string>();
            var currentPart = new StringBuilder();
            bool insideQuotes = false;

            foreach (var ch in line)
            {
                if (ch == '"')
                {
                    insideQuotes = !insideQuotes;
                }
                else if (ch == ',' && !insideQuotes)
                {
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                }
                else
                {
                    currentPart.Append(ch);
                }
            }

            parts.Add(currentPart.ToString());
            return parts.ToArray();
        }

        /// <summary>
        /// Builds a department hierarchy from a list of RawRow objects.
        /// </summary>
        /// <param name="rows">List of RawRow objects</param>
        public Dictionary<string, object> BuildDepartmentHierarchy(List<RawRow> rows)
        {
            try
            {

                if (rows == null || rows.Count == 0)
                {
                    Logger.Error("No data to process.");
                    return null;
                }


                // --- Unique ID counters (like auto-increment) ---
                int majorDeptIdCounter = 1000;
                int categoryIdCounter = 2000;
                int subCategoryIdCounter = 3000;

                // --- Dictionaries to check if we already created a record for a (name/parent) combination ---
                var majorDeptDict = new Dictionary<string, int>();                     // Key = MajorDeptName
                var categoryDict = new Dictionary<Tuple<int, string>, int>();          // Key = (MajorDeptId, CategoryName)
                var subCategoryDict = new Dictionary<Tuple<int, int, string>, int>();  // Key = (MajorDeptId, CategoryId, SubCategoryName)

                // --- Final lists that represent tables ---
                var majorDepts = new List<MajorDept>();
                var categories = new List<Category>();
                var subCategories = new List<SubCategory>();
                var items = new List<Item>();

                // Iterate over each raw CSV/table row
                foreach (var row in rows)
                {
                    // If any critical piece is missing, skip or handle accordingly
                    if (string.IsNullOrEmpty(row.Major_Dept) ||
                        string.IsNullOrEmpty(row.Category) ||
                        string.IsNullOrEmpty(row.Sub_Category))
                    {
                        // For now, continue. Later, we might want to log or handle this differently.
                        continue;
                    }

                    // 1) Insert/find MajorDept
                    if (!majorDeptDict.ContainsKey(row.Major_Dept))
                    {
                        majorDeptIdCounter++;
                        majorDeptDict[row.Major_Dept] = majorDeptIdCounter;

                        majorDepts.Add(new MajorDept
                        {
                            MajorDeptId = majorDeptIdCounter,
                            MajorDeptName = row.Major_Dept
                        });
                    }
                    int majorDeptId = majorDeptDict[row.Major_Dept];

                    // 2) Insert/find Category 
                    //    Use a composite key so the same category name in different MajorDepts gets a different CategoryId
                    var catKey = new Tuple<int, string>(majorDeptId, row.Category);

                    if (!categoryDict.ContainsKey(catKey))
                    {
                        categoryIdCounter++;
                        categoryDict[catKey] = categoryIdCounter;

                        categories.Add(new Category
                        {
                            CategoryId = categoryIdCounter,
                            MajorDeptId = majorDeptId,
                            CategoryName = row.Category
                        });
                    }
                    int categoryId = categoryDict[catKey];

                    // 3) Insert/find SubCategory
                    //    Use (MajorDeptId, CategoryId, SubCategoryName) as the composite key
                    var subCatKey = new Tuple<int, int, string>(majorDeptId, categoryId, row.Sub_Category);

                    if (!subCategoryDict.ContainsKey(subCatKey))
                    {
                        subCategoryIdCounter++;
                        subCategoryDict[subCatKey] = subCategoryIdCounter;

                        subCategories.Add(new SubCategory
                        {
                            SubCategoryId = subCategoryIdCounter,
                            CategoryId = categoryId,
                            SubCategoryName = row.Sub_Category
                        });
                    }
                    int subCategoryId = subCategoryDict[subCatKey];

                    // 4) Finally create the Item record referencing SubCategoryId

                    if (long.TryParse(row.ItemID, out long itemNumber))
                    {
                        items.Add(new Item
                        {
                            ItemId = itemNumber,
                            SubCategoryId = subCategoryId,
                            ItemNumber = row.ItemID.Trim(),
                            ItemName = row.Item_Name1.Trim(),
                            ItemDept = row.Major_Dept.Trim()
                        });
                    }
                }

                // check the 3 database tables to see if they are empty
                // if they are empty, then we need to update the hierarchy

                // Get the record count from the database
                var majorDeptCount = DatabaseHelper.GetRecordCount("TblDepartments");
                var categoryCount = DatabaseHelper.GetRecordCount("TblClasses");
                var subCategoryCount = DatabaseHelper.GetRecordCount("tblCategories");

                // If any of the tables are not empty, we need to update the hierarchy
                if (majorDeptCount == 0 || categoryCount == 0 || subCategoryCount == 0) { _updateHierarchy = true; }

                if (_updateHierarchy)
                {
                    Logger.Info("Updating department hierarchy in the database...");

                    // At this point we have 4 in-memory Lists that mirror 4 tables:
                    // majorDepts, categories, subCategories, items

                    DatabaseHelper.ExecuteSQLCommand("DELETE * FROM TblDepartments WHERE DeptNum > 999");
                    // TblDepartments Insert SQL Statement
                    foreach (var majorDept in majorDepts)
                    {
                        string insertMajorDept = $"INSERT INTO TblDepartments (DeptNum, DeptName) VALUES ({majorDept.MajorDeptId}, '{majorDept.MajorDeptName}')";
                        DatabaseHelper.ExecuteSQLCommand(insertMajorDept);
                        Logger.Trace($"Inserted MajorDept: {majorDept.MajorDeptName} with ID: {majorDept.MajorDeptId}");
                    }

                    DatabaseHelper.ExecuteSQLCommand("DELETE * FROM TblClasses WHERE ClassNum > 1999");
                    // Class Insert SQL Statement
                    foreach (var category in categories)
                    {
                        string insertCategory = $"INSERT INTO TblClasses (ClassNum, DeptNum, Class) VALUES ({category.CategoryId}, {category.MajorDeptId}, '{category.CategoryName}')";
                        DatabaseHelper.ExecuteSQLCommand(insertCategory);
                        Logger.Trace($"Inserted Class: {category.CategoryName} with ID: {category.CategoryId}");
                    }

                    DatabaseHelper.ExecuteSQLCommand("DELETE * FROM tblCategories WHERE CategoryNum > 2999");
                    // Category Insert SQL Statement
                    foreach (var subCategory in subCategories)
                    {
                        string insertSubCategory = $"INSERT INTO tblCategories (CategoryNum, ClassNum, Category) VALUES ({subCategory.SubCategoryId}, {subCategory.CategoryId}, '{subCategory.SubCategoryName}')";
                        DatabaseHelper.ExecuteSQLCommand(insertSubCategory);
                        Logger.Trace($"Inserted Category: {subCategory.SubCategoryName} with ID: {subCategory.SubCategoryId}");
                    }

                    Dictionary<string, Dictionary<string, object>> UpdateDict = new Dictionary<string, Dictionary<string, object>>();

                    foreach (var item in items)
                    {
                        if (long.TryParse(item.ItemNumber, out long _))
                        {
                            UpdateDict.Add(item.ItemNumber, new Dictionary<string, object>
                            {
                                { "CategoryNum", item.SubCategoryId }
                            });
                        }
                    }

                    var result = DatabaseHelper.UpdateFieldsByDictionary(UpdateDict, "Description 11");
                    ForceUpdate = result.Item2;
                }

                Dictionary<string, object> itemUpdateDict = new Dictionary<string, object>();

                foreach (var item in items)
                {
                    if (long.TryParse(item.ItemNumber, out long _))
                    {
                        itemUpdateDict.Add(item.ItemNumber, item.SubCategoryId);
                    }
                }


                // Log the counts
                Logger.Trace($"TblDepartments Count: {majorDepts.Count}");
                Logger.Trace($"TblClasses Count: {categories.Count}");
                Logger.Trace($"tblCategories Count: {subCategories.Count}");
                Logger.Trace($"tblProducts Updated: {items.Count}");

                return itemUpdateDict;

            }
            catch (Exception ex)
            {
                Logger.Error("Error building department hierarchy: " + ex.Message);
                return null;
            }
        }

        #endregion
    }
}
