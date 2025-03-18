using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Module.Invafresh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Modes
{
    public static class TestingMode
    {
        public static async Task RunTestMode()
        {

            try
            {
                Logger.Info("Starting test mode");

                ProductProcessor productProcessor = new ProductProcessor(null);
                var productTemplate = productProcessor.CreateProductTemplate();

                IImporterModule importerModule = new InvafreshModule();
                importerModule.TriggerValue = "D:\\811-Master_Export.txt";
                importerModule.Initialize();

                var items = importerModule.GetTblProductsList(productTemplate);

                Logger.Info($"Total items retrieved: {items.Count}");
                Console.WriteLine($"Total items retrieved: {items.Count}");

                List<tblProducts> processedItems = new List<tblProducts>();

                // Process each item using tasks
                var tasks = items.Select(async item =>
                {
                    //item = _customerProcess.PreProductProcess(item);

                    // format price to 2 decimal places, parsing first
                    item.Price = decimal.Parse(item.Price).ToString("0.00");
                    item.SalePrice = string.IsNullOrEmpty(item.SalePrice) ? string.Empty : decimal.Parse(item.SalePrice).ToString("0.00");

                    var processedProduct = await productProcessor.ProcessProduct(item);
                    if (processedProduct.PLU != "0")
                    {
                        // look through the list for a matching PLU
                        var existingProduct = processedItems.FirstOrDefault(p => p.PLU == processedProduct.PLU);

                        if (existingProduct == null)
                        {
                            processedItems.Add(processedProduct);
                            Logger.Debug($"Processed PLU: {processedProduct.PLU}, Description: {processedProduct.Description1} {processedProduct.Description2}");
                        }
                        else
                        {
                            Logger.Warn($"Duplicate PLU found: {processedProduct.PLU}");
                        }
                    }
                });

                await Task.WhenAll(tasks);

                // log all duplicates PLUs
                var duplicates = processedItems.GroupBy(p => p.PLU).Where(g => g.Count() > 1).Select(g => g.Key);
                foreach (var duplicate in duplicates)
                {
                    Logger.Warn($"Duplicate PLU found: {duplicate}");
                }

                // Set all fields to non-indexed except PLU
                DatabaseHelper.SetFieldsToNonIndexedExceptPLU();

                // only if there are products to insert
                if (processedItems.Count > 0)
                {
                    // Use bulk operation
                    var recordsUpdated = DatabaseHelper.BulkInsertOrUpdate(processedItems);

                    // Write ImportLog.txt to trigger ProcessDatabase if recordsUpdated is true
                    if (recordsUpdated)
                    {
                        Logger.Info($"Records updated, writing to ImportLog.txt");
                        //WriteTimeToFile();
                    }
                    //else if (_customerProcess.ForceUpdate)
                    //{
                    //    Logger.Info($"Force update triggered from {_customerProcess.Name} Custom Process, writing to ImportLog.txt");
                    //    WriteTimeToFile();
                    //}
                    else
                    {
                        Logger.Info("No new products to import or update.");
                    }

                    //_customerProcess.PostProductProcess();
                }
                else
                {
                    Logger.Info("No new products to import.");
                }


                // Write processed items to a tab-delimited file
                //string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProcessedItems.txt");
                //WriteItemsToFile(processedItems, outputPath);

                //Logger.Info($"Test mode completed successfully. Output written to: {outputPath}");
                //Console.WriteLine($"Test mode completed. Output written to: {outputPath}");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();

            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred during test mode", ex);
                Console.WriteLine($"An error occurred during test mode: {ex.Message}");
                Console.WriteLine("Check the log file for more details.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
        private static void WriteItemsToFile(List<tblProducts> items, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write headers
                    var properties = typeof(tblProducts).GetProperties()
                        .Where(p => p.GetCustomAttribute<ImportDBFieldAttribute>() != null)
                        .OrderBy(p => p.GetCustomAttribute<ImportDBFieldAttribute>().Name);

                    writer.WriteLine(string.Join("\t", properties.Select(p => p.GetCustomAttribute<ImportDBFieldAttribute>().Name)));

                    // Write data
                    foreach (var item in items)
                    {
                        var values = properties.Select(p => p.GetValue(item)?.ToString() ?? "");
                        writer.WriteLine(string.Join("\t", values));
                    }
                }

                Logger.Info($"Items successfully written to file: {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error writing items to file: {filePath}", ex);
            }
        }
    }
}
