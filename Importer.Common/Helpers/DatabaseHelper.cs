﻿using Dapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using Importer.Common.Models;

namespace Importer.Common.Helpers
{
    public static class DatabaseHelper
    {
        private static readonly string _connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Program Files\\MM_Label_Import\\System Database\\MM_IMPORT_DB.mdb;";

        public static IEnumerable<tblProducts> GetProducts()
        {
            using (OleDbConnection connection = new OleDbConnection(_connectionString))
            {
                connection.Open();

                // Get schema information
                var schemaTable = connection.GetSchema("Columns", new[] { null, null, "tblProducts", null });

                // Build the SQL query dynamically
                var columns = string.Join(", ", schemaTable.Rows.OfType<DataRow>().Select(row =>
                {
                    var columnName = row["COLUMN_NAME"].ToString();
                    return columnName.Contains(" ") ? $"[{columnName}] AS {columnName.Replace(" ", "")}" : columnName;
                }));

                string query = $"SELECT {columns} FROM tblProducts";

                var items = connection.Query<tblProducts>(query).ToList();

                return items;
            }
        }
        public static void Insert(tblProducts item)
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();

                var properties = typeof(tblProducts).GetProperties()
                    .Where(p => p.GetCustomAttribute<ImportDBFieldAttribute>() != null);

                var columnNames = string.Join(", ", properties.Select(p => $"[{p.GetCustomAttribute<ImportDBFieldAttribute>().Name}]"));
                var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                string query = $"INSERT INTO tblProducts ({columnNames}) VALUES ({parameterNames})";

                string queryWithValues = GetQueryWithValues(query, item);

                Console.WriteLine("Insert Query:");
                Console.WriteLine(queryWithValues);

                connection.Execute(query, item);
            }
        }
        private static string GetQueryWithValues(string query, object parameters)
        {
            foreach (var property in parameters.GetType().GetProperties())
            {
                var parameterValue = property.GetValue(parameters);
                string valueString;

                if (parameterValue == null && property.PropertyType.Name == "String")
                {
                    valueString = "''";
                }
                else if (parameterValue is string || parameterValue is DateTime)
                {
                    valueString = $"'{parameterValue}'";
                }
                else if (parameterValue is bool)
                {
                    valueString = (bool)parameterValue ? "1" : "0";
                }
                else
                {
                    valueString = parameterValue.ToString();
                }

                query = query.Replace($"@{property.Name} ", valueString);
            }
            return query;
        }
        public static void Update(tblProducts item)
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();

                var properties = typeof(tblProducts).GetProperties()
                    .Where(p => p.GetCustomAttribute<ImportDBFieldAttribute>() != null);

                var setClause = string.Join(", ", properties
                    .Where(p => p.Name != nameof(tblProducts.PLU)) // Assuming PLU is the primary key
                    .Select(p => $"[{p.GetCustomAttribute<ImportDBFieldAttribute>().Name}] = @{p.Name} "));

                string query = $"UPDATE tblProducts SET {setClause} WHERE [PLU] = '{item.PLU}'"; // Assuming PLU is the primary key

                string queryWithValues = GetQueryWithValues(query, item);

                Console.WriteLine("Update Query:");
                Console.WriteLine(queryWithValues);

                connection.Execute(queryWithValues);
            }
        }

        /// <summary>
        /// Performs a bulk insert or update operation on the tblProducts table using a specified primary key field.
        /// 
        /// Performance:
        /// - Generally faster than individual inserts/updates, especially for large datasets.
        /// - Performance improves with larger batch sizes.
        /// - May consume more memory as it loads all data into a DataTable.
        /// 
        /// Use Case:
        /// - Best for scenarios where you need to insert/update a large number of records quickly.
        /// - Allows specifying a custom field to use as the primary key for update operations.
        /// 
        /// Limitations:
        /// - May not provide detailed feedback on which records were inserted vs updated.
        /// - Could potentially overwrite data if not used carefully.
        /// </summary>
        /// <param name="products">List of tblProducts to insert or update</param>
        /// <param name="primaryKeyField">Name of the field to use as the primary key for updates</param>
        public static bool BulkInsertOrUpdate(List<tblProducts> products, string primaryKeyField = "PLU")
        {
            int updated = 0;
            int inserted = 0;

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();

                    // Create a DataTable with the structure matching the database fields
                    DataTable dt = new DataTable("tblProducts_DT");
                    var properties = typeof(tblProducts).GetProperties()
                        .Where(p => p.GetCustomAttribute<ImportDBFieldAttribute>() != null);

                    // Create a mapping from property names to DataTable column names
                    Dictionary<string, string> propertyToColumnName = new Dictionary<string, string>();
                    Dictionary<string, string> columnNameToFieldName = new Dictionary<string, string>();
                    Dictionary<string, string> fieldNameToPropertyName = new Dictionary<string, string>();

                    // Create OleDbDataAdapter with a SELECT command to fetch existing records
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter($"SELECT * FROM tblProducts", connection))
                    {
                        // Fill the DataTable schema from the database
                        adapter.FillSchema(dt, SchemaType.Source);

                        // Map properties to DataTable columns
                        foreach (var prop in properties)
                        {
                            var attr = prop.GetCustomAttribute<ImportDBFieldAttribute>();
                            string fieldName = attr.Name; // Original field name (may contain spaces)
                            string columnName = fieldName; // Use actual column name from the database

                            if (!dt.Columns.Contains(columnName))
                            {
                                throw new Exception($"Column '{columnName}' not found in DataTable.");
                            }

                            propertyToColumnName[prop.Name] = columnName;
                            columnNameToFieldName[columnName] = fieldName;
                            fieldNameToPropertyName[fieldName] = prop.Name;
                        }

                        // Use actual primary key field name
                        string primaryKeyColumn = primaryKeyField;
                        dt.PrimaryKey = new DataColumn[] { dt.Columns[primaryKeyColumn] };

                        // Assign UpdateCommand and InsertCommand to the adapter
                        adapter.UpdateCommand = CreateCustomUpdateCommand(connection, dt, primaryKeyField, columnNameToFieldName);
                        adapter.InsertCommand = CreateCustomInsertCommand(connection, dt, columnNameToFieldName);

                        // Fill the DataTable with existing data from the database
                        adapter.Fill(dt);

                        // Process each product
                        foreach (var product in products)
                        {
                            // Get the primary key property based on the field name
                            var pkProp = properties.FirstOrDefault(p => p.GetCustomAttribute<ImportDBFieldAttribute>().Name == primaryKeyField);
                            if (pkProp == null)
                            {
                                throw new Exception($"Primary key field '{primaryKeyField}' not found in tblProducts properties.");
                            }

                            object pkValue = pkProp.GetValue(product);

                            // Get the column name for the primary key
                            string pkPropertyName = pkProp.Name;
                            string pkColumnName = propertyToColumnName[pkPropertyName];

                            // Create a DataRow array to find matching rows based on the primary key
                            string filterExpression;
                            if (pkValue is string)
                            {
                                filterExpression = $"[{pkColumnName}] = '{pkValue}'";
                            }
                            else if (pkValue is DateTime dateValue)
                            {
                                filterExpression = $"[{pkColumnName}] = #{dateValue:MM/dd/yyyy HH:mm:ss}#";
                            }
                            else
                            {
                                filterExpression = $"[{pkColumnName}] = {pkValue}";
                            }

                            DataRow[] existingRows = dt.Select(filterExpression);

                            if (existingRows.Length > 0)
                            {
                                DataRow row = existingRows[0];
                                bool hasChanges = false;

                                foreach (var prop in properties)
                                {
                                    string columnName = propertyToColumnName[prop.Name];
                                    var newValue = prop.GetValue(product) ?? DBNull.Value;
                                    var oldValue = row[columnName];

                                    if (!ValuesAreEqual(oldValue, newValue))
                                    {
                                        // Log the differences for debugging
                                        Logger.Debug($"Value changed for column '{columnName}': Old='{oldValue}' ({oldValue?.GetType()}), New='{newValue}' ({newValue?.GetType()})");
                                        row[columnName] = newValue;
                                        hasChanges = true;
                                    }
                                }

                                if (hasChanges)
                                {
                                    updated++;
                                }
                                else
                                {
                                    // Accept changes to reset RowState if no changes were made
                                    row.AcceptChanges();
                                }
                            }
                            else
                            {
                                // Add new row
                                DataRow row = dt.NewRow();
                                foreach (var prop in properties)
                                {
                                    string columnName = propertyToColumnName[prop.Name];
                                    row[columnName] = prop.GetValue(product) ?? DBNull.Value;
                                }
                                dt.Rows.Add(row);
                                inserted++;
                            }
                        }

                        var modifiedRows = dt.GetChanges(DataRowState.Modified);
                        int modifiedRowCount = modifiedRows?.Rows.Count ?? 0;
                        Logger.Info($"{modifiedRowCount} rows are marked as modified.");

                        // Perform bulk insert/update
                        adapter.Update(dt);
                    }

                    Logger.Info($"Bulk operation completed. {updated} records updated, {inserted} records inserted.");

                    // return true if any records were updated or inserted, otherwise false
                    return updated + inserted > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during bulk insert/update: {ex.Message}", ex);
                return false;
            }
        }
        private static OleDbCommand CreateCustomUpdateCommand(OleDbConnection connection,DataTable dt,string primaryKeyField,Dictionary<string, string> columnNameToFieldName)
        {
            string primaryKeyColumn = primaryKeyField; // Use actual primary key field name

            // Build the SET clause with ? placeholders
            var updateFields = dt.Columns.Cast<DataColumn>()
                .Where(col => col.ColumnName != primaryKeyColumn)
                .Select(col => $"[{col.ColumnName}] = ?");

            // Build the UPDATE query
            string updateQuery = $@"UPDATE tblProducts SET {string.Join(", ", updateFields)} WHERE [{primaryKeyField}] = ?";

            OleDbCommand command = new OleDbCommand(updateQuery, connection);

            // Add parameters in the same order as they appear in the query
            foreach (DataColumn col in dt.Columns.Cast<DataColumn>().Where(c => c.ColumnName != primaryKeyColumn))
            {
                command.Parameters.Add(new OleDbParameter()
                {
                    OleDbType = GetOleDbType(col.DataType, col.ColumnName),
                    SourceColumn = col.ColumnName,
                    SourceVersion = DataRowVersion.Current
                });
            }

            // Add parameter for the primary key (use Original version for WHERE clause)
            DataColumn pkCol = dt.Columns[primaryKeyColumn];
            command.Parameters.Add(new OleDbParameter()
            {
                OleDbType = GetOleDbType(pkCol.DataType, pkCol.ColumnName),
                SourceColumn = pkCol.ColumnName,
                SourceVersion = DataRowVersion.Original
            });

            return command;
        }
        private static OleDbCommand CreateCustomInsertCommand(OleDbConnection connection,DataTable dt,Dictionary<string, string> columnNameToFieldName)
        {
            var insertFields = dt.Columns.Cast<DataColumn>()
                .Select(col => $"[{columnNameToFieldName[col.ColumnName]}]");
            var insertValues = dt.Columns.Cast<DataColumn>()
                .Select(col => "?");

            string insertQuery = $@"
        INSERT INTO tblProducts ({string.Join(", ", insertFields)})
        VALUES ({string.Join(", ", insertValues)})";

            OleDbCommand command = new OleDbCommand(insertQuery, connection);

            // Add parameters in the same order as they appear in the VALUES clause
            foreach (DataColumn col in dt.Columns)
            {
                OleDbType DBtype = GetOleDbType(col.DataType, col.ColumnName);
                command.Parameters.Add(new OleDbParameter()
                {
                    OleDbType = DBtype,
                    SourceColumn = col.ColumnName
                });
            }

            return command;
        }
        private static bool ValuesAreEqual(object oldValue, object newValue)
        {
            // Convert DBNull to null
            if (oldValue == DBNull.Value) oldValue = null;
            if (newValue == DBNull.Value) newValue = null;

            // Check for nulls
            if (oldValue == null && newValue == null)
                return true;
            if (oldValue == null || newValue == null)
                return false;

            // Handle DateTime comparison with special case for default dates
            if (oldValue is DateTime oldDate && newValue is DateTime newDate)
            {
                // Normalize default dates
                if (IsDefaultDate(oldDate)) oldDate = DateTime.MinValue;
                if (IsDefaultDate(newDate)) newDate = DateTime.MinValue;

                return oldDate.Equals(newDate);
            }

            // Handle string comparison
            if (oldValue is string oldStr && newValue is string newStr)
            {
                oldStr = oldStr.Trim();
                newStr = newStr.Trim();
                return string.Equals(oldStr, newStr, StringComparison.InvariantCultureIgnoreCase);
            }

            // Handle numeric comparison
            if (IsNumericType(oldValue) && IsNumericType(newValue))
            {
                decimal oldDecimal = Convert.ToDecimal(oldValue);
                decimal newDecimal = Convert.ToDecimal(newValue);
                return oldDecimal == newDecimal;
            }

            // Handle Boolean comparison
            if (oldValue is bool oldBool && newValue is bool newBool)
            {
                return oldBool == newBool;
            }

            // Attempt to convert and compare
            try
            {
                var convertedNewValue = Convert.ChangeType(newValue, oldValue.GetType());
                return oldValue.Equals(convertedNewValue);
            }
            catch
            {
                // If conversion fails, consider values not equal
                return false;
            }
        }
        // Helper method to check for default dates
        private static bool IsDefaultDate(DateTime date)
        {
            // Check for .NET default date or Access default date
            return date == DateTime.MinValue || date == new DateTime(1899, 12, 30);
        }
        private static bool IsNumericType(object value)
        {
            return value is byte || value is sbyte ||
                   value is short || value is ushort ||
                   value is int || value is uint ||
                   value is long || value is ulong ||
                   value is float || value is double ||
                   value is decimal;
        }
        private static OleDbType GetOleDbType(Type type, string columnName)
        {
            if (type == typeof(string))
                return OleDbType.VarWChar; // Use VarWChar for Unicode support
            if (type == typeof(int) || type == typeof(short))
                return OleDbType.Integer;
            if (type == typeof(long))
                return OleDbType.BigInt;
            if (type == typeof(decimal))
                return OleDbType.Decimal;
            if (type == typeof(double))
                return OleDbType.Double;
            if (type == typeof(DateTime))
                return OleDbType.Date;
            if (type == typeof(bool))
                return OleDbType.Boolean;
            if (type == typeof(byte[]))
                return OleDbType.Binary;
            // Add other type mappings as needed

            throw new Exception($"Unsupported data type '{type}' for column '{columnName}'.");
        }
        public static void SetFieldsToNonIndexedExceptPLU()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();

                    // Retrieve index schema information for the specified table
                    DataTable indexes = connection.GetSchema("Indexes", new string[] { null, null, null, null, "tblProducts" });

                    // Dictionary to hold index names and their associated columns
                    var indexColumns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                    foreach (DataRow row in indexes.Rows)
                    {
                        string indexName = row["INDEX_NAME"].ToString();
                        string columnName = row["COLUMN_NAME"].ToString();

                        if (!indexColumns.ContainsKey(indexName))
                        {
                            indexColumns[indexName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        }

                        indexColumns[indexName].Add(columnName);
                    }

                    foreach (var index in indexColumns)
                    {
                        var indexName = index.Key;
                        var columns = index.Value;

                        // If the index is solely on [PLU], keep it
                        if (columns.Count == 1 && columns.Contains("PLU"))
                        {
                            continue; // Skip dropping this index
                        }
                        else
                        {
                            // Drop the index

                            Logger.Info($"Dropping index on Field: {indexName}");

                            string sql = $"DROP INDEX [{indexName}] ON [tblProducts]";
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting fields to non-indexed: {ex.Message}", ex);
            }

        }

        /// <summary>
        /// Performs a bulk upsert (insert or update) operation on the tblProducts table using individual SQL statements within a transaction.
        /// 
        /// Performance:
        /// - Generally slower than BulkInsertOrUpdate for very large datasets.
        /// - Performance can be good for small to medium-sized datasets.
        /// - Uses less memory than BulkInsertOrUpdate as it processes records individually.
        /// 
        /// Use Case:
        /// - Provides more control over the insert/update process for each record.
        /// - Allows for custom logic to be applied on a per-record basis.
        /// - Useful when you need to know exactly which records were inserted vs updated.
        /// 
        /// Advantages:
        /// - More flexible than BulkInsertOrUpdate, allowing for complex upsert logic.
        /// - Can provide better error handling and reporting on a per-record basis.
        /// - Uses a database transaction for better data integrity.
        /// 
        /// Limitations:
        /// - May be slower than BulkInsertOrUpdate for very large datasets.
        /// - Requires more careful management of database connections and transactions.
        /// </summary>
        /// <param name="products">List of tblProducts to upsert</param>
        public static void BulkUpsert(List<tblProducts> products)
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var properties = typeof(tblProducts).GetProperties()
                            .Where(p => p.GetCustomAttribute<ImportDBFieldAttribute>() != null)
                            .ToList();

                        string updateFields = string.Join(", ", properties
                            .Where(p => p.Name != "PLU")
                            .Select(p => $"{p.GetCustomAttribute<ImportDBFieldAttribute>().Name} = @{p.Name}"));

                        string insertFields = string.Join(", ", properties
                            .Select(p => p.GetCustomAttribute<ImportDBFieldAttribute>().Name));

                        string insertValues = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                        string upsertQuery = $@"
                            UPDATE tblProducts 
                            SET {updateFields}
                            WHERE PLU = @PLU;

                            IF @@ROWCOUNT = 0
                            BEGIN
                                INSERT INTO tblProducts ({insertFields})
                                VALUES ({insertValues});
                            END";

                        foreach (var product in products)
                        {
                            connection.Execute(upsertQuery, product, transaction);
                        }

                        transaction.Commit();
                        Console.WriteLine($"Bulk upsert completed. {products.Count} records processed.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during bulk upsert: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public static List<tblProducts> BulkDelete(List<tblProducts> products)
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {                        
                        foreach (var product in products)
                        {
                            string deleteQuery = $"DELETE FROM tblProducts WHERE PLU = {product.PLU}";
                            connection.Execute(deleteQuery, transaction);
                        }
                        transaction.Commit();
                        Logger.Info($"Bulk delete completed. {products.Count} records deleted.");
                        return products;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error($"Error during bulk delete: {ex.Message}");
                        return new List<tblProducts>();
                    }
                }
            }
        }

        public static Dictionary<string, string> GetJoinedFields()
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                // TODO: Get the joined fields from the database
            }

            return new Dictionary<string, string>();
        }
        public static List<tblProducts> DeserializeJsonToProduct(string json, tblProducts productTemplate)
        {
            var products = new List<tblProducts>();
            var jsonArray = JArray.Parse(json);

            int _invalidItems = 0;

            foreach (JObject item in jsonArray)
            {
                // if PLU starts with any non-numeric character, it is a deleted item
                if (!char.IsDigit(item["PLU"].ToString()[0])) { Logger.Warn($"PLU:{item["PLU"].ToString().Trim()} is invalid, Skipping..."); _invalidItems++; continue; }

                // if PLU is 0, it is an invalid item
                if (item["PLU"].ToString().Trim() == "0") { Logger.Warn("0 is invalid PLU number, Skipping..."); _invalidItems++; continue; }

                // PLU is a required field, so skip any items without a PLU
                if (item["PLU"] == null) { Logger.Warn("Null PLU Found, Skipping..."); _invalidItems++; continue; }

                // if PromoPrice is greater than 0, set the Price to PromoPrice
                if (item["PromoPrice"] != null && Convert.ToDecimal(item["PromoPrice"]) > 0)
                {
                    item["Price"] = item["PromoPrice"];
                    Logger.Trace($"PromoPrice found for PLU: {item["PLU"]}, Price set to: {item["Price"]}");
                }

                // change ' to ` in all fields
                foreach (var field in item.Properties())
                {
                    if (field.Value.Type == JTokenType.String)
                    {
                        field.Value = field.Value.ToString().Replace("'", "`");
                    }
                }

                products.Add(ParseJsonToProducts(item.ToString(), productTemplate));
            }

            if (_invalidItems > 0) { Logger.Warn($"{_invalidItems} invalid items found and skipped."); }

            return products;
        }
        private static tblProducts ParseJsonToProducts(string json, tblProducts productTemplate)
        {
            var mappings = GetJoinedFields();
            var jsonObject = JObject.Parse(json);
            var product = new tblProducts(); // Create a new instance for each product

            // Copy default values from the template
            foreach (var prop in typeof(tblProducts).GetProperties())
            {
                prop.SetValue(product, prop.GetValue(productTemplate));
            }

            foreach (var mapping in mappings)
            {
                var jsonField = mapping.Key;
                var productField = mapping.Value;

                // if the product field has a comma in it, it needs to be split into multiple fields
                if (productField.Contains(","))
                {
                    var fields = productField.Split(',');
                    foreach (var field in fields)
                    {
                        var property = typeof(tblProducts).GetProperty(field);
                        if (property != null && jsonObject[jsonField] != null)
                        {
                            var value = jsonObject[jsonField].ToObject(property.PropertyType);
                            // if the property is a string, we need to trim leading/trailing whitespace
                            if (property.PropertyType == typeof(string))
                            {
                                value = IntialFindReplace(value);
                            }
                            property.SetValue(product, value);
                        }
                    }
                }
                else
                {
                    var property = typeof(tblProducts).GetProperty(productField);
                    if (property != null && jsonObject[jsonField] != null)
                    {
                        var value = jsonObject[jsonField].ToObject(property.PropertyType);
                        // if the property is a string, we need to trim leading/trailing whitespace
                        if (property.PropertyType == typeof(string))
                        {
                            value = IntialFindReplace(value);
                        }
                        property.SetValue(product, value);
                    }
                }
            }

            return product;
        }

        private static readonly Dictionary<string, string> _replaceRules = new Dictionary<string, string>
        {
            { "''", "\"" },
            { "Ʌ", "a" },
            { "\\n", "<section>" },
            { "\\", "" }
        };
        
        private static object IntialFindReplace(object value)
        {
            string stringValue = value.ToString().Trim();
            foreach (var rule in _replaceRules)
            {
                stringValue = stringValue.Replace(rule.Key, rule.Value);
            }
            return stringValue;
        }

        public static tblProducts DeserializeXMLToProduct(string xmlContent)
        {
            var _fieldMappings = GetJoinedFields();

            var serializer = new XmlSerializer(typeof(tblProducts));
            using (var reader = new StringReader(xmlContent))
            {
                var product = (tblProducts)serializer.Deserialize(reader);

                var doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                foreach (var mapping in _fieldMappings)
                {
                    var property = typeof(tblProducts).GetProperty(mapping.Key);
                    if (property != null)
                    {
                        var element = doc.SelectSingleNode($"//{mapping.Value}");
                        if (element != null)
                        {
                            var value = Convert.ChangeType(element.InnerText, property.PropertyType);
                            property.SetValue(product, value);
                        }
                    }
                }

                return product;
            }
        }
        public static tblProducts GetProductByPLU(string pLU)
        {
            // lookup the product in the database
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                string query = $"SELECT * FROM tblProducts WHERE PLU = '{pLU}'";
                var item = connection.QueryFirstOrDefault<tblProducts>(query);
                return item;
            }

        }
        public static List<CleanseRule> LoadCleanseRules()
        {
            var rules = new List<CleanseRule>();
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT SearchText, InsertField, CleanseType FROM DescCleanse ORDER BY ABS([ID])";
                var items = connection.Query<CleanseRule>(query).ToList();
                rules.AddRange(items);
            }
            return rules;
        }
        public static List<ReplaceRule> LoadReplaceRules()
        {
            var rules = new List<ReplaceRule>();
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT Value_Find, Value_Replace, Value_ID FROM REPLACE_VALUES ORDER BY Value_ID";
                var items = connection.Query<ReplaceRule>(query).ToList();
                rules.AddRange(items);
            }
            return rules;
        }
        public static Dictionary<string, int> LoadDepartmentPadding()
        {
            var paddingDict = new Dictionary<string, int>();

            using (var connection = new OleDbConnection(_connectionString))
            {
                var results = connection.Query<DepartmentPadding>("SELECT DataDeptNum, ValueToAddToPLU FROM System_DeptNum");

                paddingDict = new Dictionary<string, int>();
                foreach (var result in results)
                {
                    paddingDict[result.DataDeptNum] = result.ValueToAddToPLU;
                }
            }

            return paddingDict;
        }
        public static List<StockDescription> LoadStockDescriptions()
        {
            var stockDescriptions = new List<StockDescription>();
            using (var connection = new OleDbConnection(_connectionString))
            {
                stockDescriptions = connection.Query<StockDescription>("SELECT StockNum, LblName FROM TblStockDescrip").ToList();
            }

            return stockDescriptions;
        }
        public static List<string> GetPLUsToDelete(string deleteField)
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                var sql = $"SELECT PLU FROM tblProducts WHERE [{deleteField}] = UCASE('DELETE')";
                return connection.Query<string>(sql).AsList();
            }
        }
        public static int DeleteRecords(string deleteField)
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                var sql = $"DELETE FROM tblProducts WHERE [{deleteField}] = UCASE('DELETE')";
                return connection.Execute(sql);
            }
        }
        public static string GetBooleanVal(string mBooleanField)
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                var sql = "SELECT TrueVal FROM BooleanVals WHERE MMFieldName = @MBooleanField";
                return connection.QueryFirstOrDefault<string>(sql, new { MBooleanField = mBooleanField });
            }
        }
        public static List<BooleanVal> GetAllBooleanVals()
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                var sql = "SELECT MMFieldName, TrueVal FROM BooleanVals";
                return connection.Query<BooleanVal>(sql).AsList();
            }
        }
        public static List<NumberFormatModel> GetAllNumberFormats()
        {
            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                var sql = @"SELECT MMField, FormatNumber, DecimalCount 
                        FROM NumberFormat 
                        WHERE FormatNumber = True";
                return connection.Query<NumberFormatModel>(sql).AsList();
            }
        }
        public static void DeleteAllProducts()
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();
                    connection.Execute("DELETE * FROM tblProducts");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing table: {ex.Message}");
            }
        }
        public static int ExecuteSQLCommand(string sqlCommand)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();
                    return connection.Execute(sqlCommand);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error Executing Command: {ex.Message} \n Command: {sqlCommand}");
                return -1;
            }
        }

        /// <summary>
        /// Updates multiple fields in the tblProducts table using a dictionary of primary key values and field updates.
        /// The dictionary key is the primary key value, and the value is a dictionary of field names and new values.   
        /// </summary>
        /// <param name="fieldsToUpdate"> Dictionary of primary key values and field updates </param>
        /// <param name="primaryKeyField"> Name of the primary key field </param>
        /// <returns> Tuple with the number of records updated and a boolean indicating if default values were updated </returns>
        public static Tuple<int, bool> UpdateFieldsByDictionary(Dictionary<string, Dictionary<string, object>> fieldsToUpdate, string primaryKeyField)
        {
            int updated = 0;
            bool defaultValuesUpdated = false;
            if (fieldsToUpdate == null || fieldsToUpdate.Count == 0)
            {
                Logger.Warn("No fields to update.");
                return Tuple.Create(0, false);
            }

            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();

                    // Create a DataTable with the structure matching the database fields
                    DataTable dt = new DataTable("tblProducts_DT");
                    var properties = typeof(tblProducts).GetProperties()
                        .Where(p => p.GetCustomAttribute<ImportDBFieldAttribute>() != null);

                    // Create a mapping from property names to DataTable column names
                    Dictionary<string, string> propertyToColumnName = new Dictionary<string, string>();
                    Dictionary<string, string> columnNameToFieldName = new Dictionary<string, string>();

                    // Create OleDbDataAdapter with a SELECT command to fetch existing records
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter($"SELECT * FROM tblProducts", connection))
                    {
                        // Fill the DataTable schema from the database
                        adapter.FillSchema(dt, SchemaType.Source);

                        // Disable constraints to avoid issues during data load
                        dt.Constraints.Clear();

                        // Map properties to DataTable columns
                        foreach (var prop in properties)
                        {
                            var attr = prop.GetCustomAttribute<ImportDBFieldAttribute>();
                            string fieldName = attr.Name; // Original field name (may contain spaces)
                            string columnName = fieldName; // Use actual column name from the database

                            if (!dt.Columns.Contains(columnName))
                            {
                                throw new Exception($"Column '{columnName}' not found in DataTable.");
                            }

                            propertyToColumnName[prop.Name] = columnName;
                            columnNameToFieldName[columnName] = fieldName;
                        }

                        // Use actual primary key field name
                        string primaryKeyColumn = primaryKeyField;
                        dt.PrimaryKey = new DataColumn[] { dt.Columns[primaryKeyColumn] };

                        // Fill the DataTable with existing data from the database
                        adapter.Fill(dt);

                        foreach (var entry in fieldsToUpdate)
                        {
                            string primaryKeyValue = entry.Key;
                            var fields = entry.Value;

                            // Create a DataRow array to find matching rows based on the primary key
                            string filterExpression = $"[{primaryKeyColumn}] = '{primaryKeyValue}'";
                            DataRow[] existingRows = dt.Select(filterExpression);

                            if (existingRows.Length > 0)
                            {
                                DataRow row = existingRows[0];
                                bool hasChanges = false;

                                foreach (var field in fields)
                                {
                                    string columnName = field.Key;
                                    var newValue = field.Value ?? GetDefaultValue(dt.Columns[columnName].DataType);
                                    var oldValue = row[columnName];

                                    if (!ValuesAreEqual(oldValue, newValue))
                                    {
                                        if (oldValue == null || oldValue.ToString() == "0" || string.IsNullOrEmpty(oldValue.ToString()))
                                        {
                                            // If the old value is null or empty, set defaultValuesUpdated to true
                                            defaultValuesUpdated = true;
                                        }

                                        // Log the differences for debugging
                                        Logger.Trace($"Value changed for column '{columnName}': Old='{oldValue}' ({oldValue?.GetType()}), New='{newValue}' ({newValue?.GetType()})");
                                        row[columnName] = newValue;
                                        hasChanges = true;
                                    }
                                }

                                if (hasChanges)
                                {
                                    updated++;
                                }
                                else
                                {
                                    // Accept changes to reset RowState if no changes were made
                                    row.AcceptChanges();
                                }
                            }
                        }

                        var modifiedRows = dt.GetChanges(DataRowState.Modified);
                        int modifiedRowCount = modifiedRows?.Rows.Count ?? 0;
                        Logger.Info($"{modifiedRowCount} rows are marked as modified.");

                        // Enable constraints and catch any exceptions
                        try
                        {
                            dt.Constraints.Clear();
                            dt.Constraints.AddRange(dt.Constraints.Cast<Constraint>().ToArray());
                        }
                        catch (ConstraintException ex)
                        {
                            Logger.Error($"ConstraintException: {ex.Message}");
                            foreach (DataRow row in dt.GetErrors())
                            {
                                Logger.Error($"Row Error: {row.RowError}");
                                foreach (DataColumn col in row.GetColumnsInError())
                                {
                                    Logger.Error($"Column Error: {col.ColumnName} - {row.GetColumnError(col)}");
                                }
                            }
                            throw;
                        }

                        // Assign UpdateCommand to the adapter
                        adapter.UpdateCommand = CreateCustomUpdateCommand(connection, dt, primaryKeyField, columnNameToFieldName);

                        // Perform bulk update
                        adapter.Update(dt);
                    }

                    Logger.Info($"Update operation completed. {updated} records updated.");

                    // return true if any records were updated, otherwise false
                    return Tuple.Create(updated, defaultValuesUpdated);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during update operation: {ex.Message}");
                Logger.Trace(ex.ToString());
                Logger.Trace(ex.StackTrace);
                return Tuple.Create(0, false);
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(string))
                return string.Empty;
            if (type == typeof(int) || type == typeof(short) || type == typeof(long) || type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                return Activator.CreateInstance(type);
            if (type == typeof(bool))
                return false;
            if (type == typeof(DateTime))
                return DateTime.MinValue;
            if (type == typeof(byte[]))
                return new byte[0];
            // Add other type mappings as needed

            throw new Exception($"Unsupported data type '{type}'.");
        }

        public static int GetRecordCount(string table)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();
                    string sql = $"SELECT COUNT(*) FROM {table}";
                    var count = connection.ExecuteScalar<int>(sql);
                    return count;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting record count: {ex.Message}");
                return -1;
            }
        }
    }
}
