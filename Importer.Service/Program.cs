using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Core.Modes;
using Importer.Module.Invafresh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Importer.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Logger.Info("Application starting");
                bool testingEnabled = IsTestingEnabled();

                if (testingEnabled)
                {
                    Logger.Info("Running in test mode");
                    await RunTestMode();
                }
                else
                {
                    Logger.Info("Running in service mode");
                    RunServiceMode();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal("An unhandled exception occurred in the main program", ex);
                Console.WriteLine("An error occurred. Check the log file for details.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                Logger.Info("Application shutting down");
            }
        }


        private static bool IsTestingEnabled()
        {
            // Check if the TestingEnabled setting is set to true in the settings file

            SettingsLoader settingsLoader = new SettingsLoader();
            return true;
            return settingsLoader.TestingEnabled;
        }

        private static async Task RunTestMode()
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
                    var processedProduct = await productProcessor.ProcessProduct(item);
                    processedItems.Add(processedProduct);
                    Logger.Debug($"Processed PLU: {processedProduct.PLU}, Description: {processedProduct.Description1}");
                    Console.WriteLine($"PLU: {processedProduct.PLU}, Description: {processedProduct.Description1}");
                });

                await Task.WhenAll(tasks);

                // Write processed items to a tab-delimited file
                string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProcessedItems.txt");
                WriteItemsToFile(processedItems, outputPath);

                Logger.Info($"Test mode completed successfully. Output written to: {outputPath}");
                Console.WriteLine($"Test mode completed. Output written to: {outputPath}");
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

        private static void RunServiceMode()
        {
            try
            {
                HostFactory.Run(x =>
                {
                    x.Service<ServiceMode>(s =>
                    {
                        s.ConstructUsing(name => new ServiceMode());
                        s.WhenStarted(async tc => await tc.Start());
                        s.WhenStopped(async tc => await tc.Stop());
                    });
                    x.RunAsLocalSystem();
                    x.SetDescription("This service will import data a Data Host into Shelf 2 Cart Merchandiser");
                    x.SetDisplayName("Shelf 2 Cart Importer Service");
                    x.SetServiceName("S2C_ImporterService");

                    x.OnException(ex =>
                    {
                        Logger.Error("An error occurred in the service", ex);
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while setting up the service", ex);
                Console.WriteLine($"An error occurred while setting up the service: {ex.Message}");
                Console.WriteLine("Check the log file for more details.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

    }
}
