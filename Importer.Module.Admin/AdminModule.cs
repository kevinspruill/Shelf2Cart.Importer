using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Main;
using Importer.Common.Models;
using Importer.Module.Admin.Helpers;
using Importer.Module.Admin.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Admin
{
    public class AdminModule : IImporterModule
    {
        public string Name { get; set; } = "Admin";
        public string Version { get; set; } = "1.0.0";
        public ImporterInstance ImporterInstance { get; set; }
        public tblProducts ProductTemplate { get; set; } = new tblProducts();
        public bool ProcessQueued { get; set; } = false;
        public bool Flush { get; set; }
        public string ImporterTypeData { get; set; } = string.Empty;

        FileMonitor _importerType;
        string AdminConsoleFolder = string.Empty;
        string AdminConsoleFileName = "AdminConsole.mdb";

        public int GetPendingFileCount()
        {
            return 0; // Admin module does not handle files, so return 0
        }

        public List<tblProducts> GetTblProductsDeleteList()
        {
            return null; // Admin module does not handle product deletions, so return null
        }

        public List<tblProducts> GetTblProductsList()
        {
            return null; // Admin module does not handle product listings, so return null
        }

        public void InitModule(ImporterInstance importerInstance)
        {
            ImporterInstance = importerInstance;
            _importerType = new FileMonitor(this);
                
            AdminSettingsLoader Settings = new AdminSettingsLoader();
            AdminConsoleFolder = Settings.AdminConsoleProcessingFolder;
            AdminConsoleFileName = Settings.AdminConsoleFileName;
        }

        public void SetupImporterType()
        {
            // No need to set up the importer type here, as it's done in the constructor of FileMonitor
        }

        public void StartModule()
        {
            Logger.Info("Starting Admin Module...");

            // Start the file watcher
            if (_importerType != null)
            {
                _importerType.Start();
                Logger.Info("File Polling started successfully.");
            }
            else
            {
                Logger.Error("File Polling is not initialized.");
            }
        }

        public void StopModule()
        {
            Logger.Info("Stopping Admin Module...");

            // Stop the file watcher
            if (_importerType != null)
            {
                _importerType.Stop();
                Logger.Info("File Watcher stopped successfully.");
            }
            else
            {
                Logger.Error("File Watcher is not initialized.");
            }
        }

        public async Task<bool> TriggerProcess()
        {
            try
            {
                Console.WriteLine("Triggering process in Admin Module.");

                if (string.IsNullOrEmpty(ImporterTypeData))
                {
                    Logger.Error("No data provided for processing in Admin Module.");
                    return false;
                }
                else
                {
                    Logger.Info($"Processing file: {ImporterTypeData} in Admin Importer Module.");

                    AdminFileHandler fileHandler = new AdminFileHandler();

                    bool isUpdatePackage = fileHandler.IsZipFile(ImporterTypeData);

                    if (isUpdatePackage)
                    {
                        Logger.Info("The provided data is an update package (zip file).");
                        fileHandler.UnzipFile(ImporterTypeData, AdminConsoleFolder);

                        Logger.Info($"Unzipped update package to: {AdminConsoleFolder}, now moving to sync files");

                        // Synchronize files from the unzipped folder to the Admin Console folder
                        fileHandler.SyncFolders(Path.Combine(AdminConsoleFolder, "MM_Label"), "C:\\Program Files\\MM_Label\\"); // corrected closing parenthesis and added string literal
                    }
                    else
                    {
                        Logger.Info("The provided data is mdb file, moving to copy to AdminDatabase location");
                        File.Copy(ImporterTypeData, Path.Combine(AdminConsoleFolder, AdminConsoleFileName), true); // true to overwrite if exists
                        Logger.Info($"Copied mdb file to: {Path.Combine(AdminConsoleFolder, AdminConsoleFileName)}");
                    }



                    ProcessDatabase processDatabase = new ProcessDatabase();
                    await processDatabase.ProcessImport();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while processing in Admin Module: {ex.Message}");

                return true;
            }
            finally
            {
                ImporterTypeData = string.Empty; // Clear the data after processing
                Logger.Info("Admin Module process completed.");
            }

        }
    }
}
