using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Main
{
    public static class MainProcess
    {
        public static async Task ProcessAsync(IImporterModule importerModule)
        {
            ProductProcessor productProcessor = new ProductProcessor(null);
            var productTemplate = productProcessor.CreateProductTemplate();

            var items = importerModule.GetTblProductsList(productTemplate);

            var _customerProcess = InstanceLoader.GetCustomerProcess(importerModule.ImporterInstance.CustomerProcess);

            Logger.Info($"Total items retrieved: {items.Count}");
            Console.WriteLine($"Total items retrieved: {items.Count}");

            List<tblProducts> processedItems = new List<tblProducts>();

            // Process each item using tasks
            var tasks = items.Select(async item =>
            {
                item = _customerProcess.PreProductProcess(item);

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
                    WriteTimeToFile();
                }
                else if (_customerProcess.ForceUpdate)
                {
                    Logger.Info($"Force update triggered from {_customerProcess.Name} Custom Process, writing to ImportLog.txt");
                    WriteTimeToFile();
                }
                else
                {
                    Logger.Info("No new products to import or update.");
                }

                _customerProcess.PostProductProcess();
            }
            else
            {
                Logger.Info("No new products to import.");
            }
        }

        private static void WriteTimeToFile()
        {
            string path = @"C:\Program Files\MM_Label\ProcessDatabase\ImportLog.txt";
            string time = DateTime.Now.ToString();
            System.IO.File.WriteAllText(path, time);
        }
    }
}
