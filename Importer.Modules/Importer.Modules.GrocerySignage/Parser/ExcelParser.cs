using DocumentFormat.OpenXml.Packaging;
using Importer.Common.Interfaces;
using Importer.Common.Modifiers;
using DocumentFormat.OpenXml.Spreadsheet;
using Importer.Modules.GrocerySignage.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Modules.GrocerySignage.Parser
{
    public class ExcelParser
    {
        private GrocerySignageItem _groceryTemplate { get; set; }
        private ICustomerProcess _customerProcess { get; set; }
        public List<Dictionary<string, string>> GroceryRecords { get; private set; } = new List<Dictionary<string, string>>();
        public List<Dictionary<string, string>> DeletedGroceryRecords { get; private set; } = new List<Dictionary<string, string>>();

        public ExcelParser(GrocerySignageItem groceryTemplate, ICustomerProcess customerProcess = null)
        {
            _groceryTemplate = groceryTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();
        }

        public void ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel file not found: {filePath}");
            }

            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
            {
                WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
                WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                // Get header row
                Row headerRow = sheetData.Elements<Row>().FirstOrDefault();
                if (headerRow == null)
                {
                    throw new InvalidOperationException("Excel file does not contain any data.");
                }

                // Extract header values
                List<string> headers = new List<string>();
                foreach (Cell cell in headerRow.Elements<Cell>())
                {
                    string headerValue = GetCellValue(cell, workbookPart);
                    headers.Add(headerValue);
                }

                // Process data rows
                bool isFirstRow = true;
                foreach (Row row in sheetData.Elements<Row>())
                {
                    // Skip header row
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        continue;
                    }

                    Dictionary<string, string> record = new Dictionary<string, string>();
                    int cellIndex = 0;

                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        // Handle empty cells by calculating the index based on the cell reference
                        int currentIndex = CellReferenceToIndex(cell.CellReference);

                        // Fill in any missing cells
                        while (cellIndex < currentIndex)
                        {
                            if (cellIndex < headers.Count)
                            {
                                record[headers[cellIndex]] = string.Empty;
                            }
                            cellIndex++;
                        }

                        // Get the actual cell value
                        if (cellIndex < headers.Count)
                        {
                            string cellValue = GetCellValue(cell, workbookPart);
                            record[headers[cellIndex]] = cellValue;
                        }

                        cellIndex++;
                    }

                    // Add the record if it's not empty (at least one field has a value)
                    if (record.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                    {
                        GroceryRecords.Add(record);
                    }
                }
            }
        }

        private string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell.CellValue == null)
            {
                return string.Empty;
            }

            string value = cell.CellValue.InnerText;

            // If the cell contains a shared string
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                SharedStringTablePart sharedStringPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                if (sharedStringPart != null)
                {
                    value = sharedStringPart.SharedStringTable.Elements<SharedStringItem>()
                        .ElementAt(int.Parse(value)).InnerText;
                }
            }
            else if (cell.DataType != null && cell.DataType.Value == CellValues.Boolean)
            {
                value = value == "1" ? "TRUE" : "FALSE";
            }

            return value;
        }

        private int CellReferenceToIndex(string cellReference)
        {
            // Extract the column letters (e.g., "A", "B", "AB")
            string columnReference = new string(cellReference.TakeWhile(c => !char.IsDigit(c)).ToArray());

            // Convert column letters to 0-based index
            int columnIndex = 0;
            foreach (char c in columnReference)
            {
                columnIndex = columnIndex * 26 + (c - 'A' + 1);
            }
            return columnIndex - 1;  // 0-based index
        }

        public List<MMScaleGrocery> ConvertGroceryRecordsToMMScaleGrocery()
        {
            var groceryItems = new List<MMScaleGrocery>();
            foreach (var groceryRecord in GroceryRecords)
            {
                var groceryItem = ConvertGroceryRecordToMMScaleGrocery(groceryRecord);
                groceryItems.Add(groceryItem);
            }
            return groceryItems;
        }

        public List<MMScaleGrocery> ConvertDeletedGroceryRecordsToMMScaleGrocery()
        {
            var groceryItems = new List<MMScaleGrocery>();
            foreach (var groceryRecord in DeletedGroceryRecords)
            {
                var groceryItem = ConvertGroceryRecordToMMScaleGrocery(groceryRecord);
                groceryItems.Add(groceryItem);
            }
            return groceryItems;
        }

        private MMScaleGrocery ConvertGroceryRecordToMMScaleGrocery(Dictionary<string, string> groceryRecord)
        {
            var groceryItem = new MMScaleGrocery();
            var properties = typeof(GrocerySignageItem).GetProperties();

            foreach (var property in properties)
            {
                // Direct mapping - Excel column name matches the property name
                string propertyName = property.Name;

                // Check if the field exists in the record
                bool fieldValueExists = groceryRecord.ContainsKey(propertyName);

                if (fieldValueExists)
                {
                    // Get the value from groceryRecord and set it to the groceryItem, converting it to the correct type
                    var value = groceryRecord[propertyName];
                    var propertyType = property.PropertyType;

                    if (propertyType == typeof(bool))
                    {
                        // Check if this is a boolean field and handle accordingly
                        // For boolean values, we assume "TRUE", "YES", "Y", "1" are true values
                        var isTrueValue = (value != null &&
                            (value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                             value.Equals("YES", StringComparison.OrdinalIgnoreCase) ||
                             value.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                             value.Equals("1")));

                        property.SetValue(groceryItem, isTrueValue);
                    }
                    else
                    {
                        try
                        {
                            // Handle empty strings for non-string types
                            if (string.IsNullOrEmpty(value) && propertyType != typeof(string))
                            {
                                // Use default value for value types, null for reference types
                                property.SetValue(groceryItem, propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null);
                            }
                            else
                            {
                                var convertedValue = Convert.ChangeType(value, propertyType);
                                property.SetValue(groceryItem, convertedValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error and continue with default values
                            Console.WriteLine($"Error converting value '{value}' to type {propertyType} for property {property.Name}: {ex.Message}");
                            property.SetValue(groceryItem, propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null);
                        }
                    }
                }
            }

            return groceryItem;
        }
    }

}