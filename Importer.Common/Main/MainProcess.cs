using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
            SettingsLoader Settings = new SettingsLoader();
            DatabaseHelper DatabaseHelper = new DatabaseHelper(DatabaseType.ImportDatabase);

            var _customerProcess = InstanceLoader.GetCustomerProcess(importerModule.ImporterInstance.CustomerProcess);

            ProductProcessor productProcessor = new ProductProcessor(_customerProcess);

            var items = importerModule.GetTblProductsList();
            var deleteItems = importerModule.GetTblProductsDeleteList();

            if (Settings.Flush || importerModule.Flush)
            {
                DatabaseHelper.DeleteAllProducts();
                Logger.Info("Flushed all products from the database.");
            }
            else if (deleteItems != null && deleteItems.Count > 0)
            {
                DatabaseHelper.BulkDelete(deleteItems);
                Logger.Info($"Deleted {deleteItems.Count} items from the database.");
            }

            Logger.Info($"Total items retrieved: {items.Count}");
            List<tblProducts> processedItems = new List<tblProducts>();

            // Process each item using tasks
            var tasks = items.Select(async item =>
            {
                // replace apostrophes in all string fields with ticks
                item.ReplaceApostrophes();

                // apply custom process before product processing
                item = _customerProcess.PreProductProcess(item);

                // format price to 2 decimal places, parsing first
                try
                {
                    item.Price = string.IsNullOrEmpty(item.Price) ? string.Empty : decimal.Parse(item.Price).ToString("0.00");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message, ex);
                }
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

                // Only write to ImportLog.txt if there are no more files to process
                if (importerModule.GetPendingFileCount() == 0)
                {
                    // Write ImportLog.txt to trigger ProcessDatabase if recordsUpdated is true
                    if (recordsUpdated)
                    {
                        Logger.Info($"Records updated, writing to ImportLog.txt");
                        await TriggerProcessDatabase();
                    }
                    else if (deleteItems.Count > 0 || Settings.Flush)
                    {
                        Logger.Info($"Records deleted or flush triggered, writing to ImportLog.txt");
                        await TriggerProcessDatabase();
                    }
                    else if (_customerProcess.ForceUpdate)
                    {
                        Logger.Info($"Force update triggered from {_customerProcess.Name} Custom Process, writing to ImportLog.txt");
                        await TriggerProcessDatabase();
                    }
                }
                else
                {
                    Logger.Info("Skipping ImportLog.txt generation - more files queued for processing");
                }

                _customerProcess.PostProductProcess();
            }
            else
            {
                Logger.Info("No new products to import.");
            }
        }

        private static async Task TriggerProcessDatabase()
        {
            string path = @"C:\Program Files\MM_Label\ProcessDatabase\ImportLog.txt";
            string time = DateTime.Now.ToString();

            // Use Task.Run to ensure the method runs asynchronously
            await Task.Run(() => File.WriteAllText(path, time));

            ProcessDatabase processDatabase = new ProcessDatabase();
            await processDatabase.ProcessImport();
        }
    }
}
