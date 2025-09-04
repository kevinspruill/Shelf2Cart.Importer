using Importer.Common.Helpers;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Main
{
    public class ProcessDatabase
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "ProcessDatabaseSettings.json";
        private readonly string _settingsPath;
        private Dictionary<string, object> _settings;
        private DatabaseHelper ProcessingDatabaseHelper;
        private DatabaseHelper ImportDatabaseHelper;
        private DatabaseHelper ResidentDatabaseHelper;

        // Properties for specific settings
        public string ResidentDatabase => jsonLoader.GetSetting<string>("ResidentDatabase", _settings);
        public string ProcessingDatabase => jsonLoader.GetSetting<string>("ProcessDatabase", _settings);
        public string ImporterDatabase => jsonLoader.GetSetting<string>("ImporterDatabase", _settings);
        public string AdminConsoleDatabase => jsonLoader.GetSetting<string>("AdminConsoleDatabase", _settings);

        // Boolean settings
        public bool UseLegacy => jsonLoader.GetSetting<bool>("UseLegacy", _settings);
        public bool UseAdminConsoleDatabase => jsonLoader.GetSetting<bool>("UseAdminConsoleDatabase", _settings);
        public bool UseBaseDatabase => jsonLoader.GetSetting<bool>("UseBaseDatabase", _settings);
        public bool ImportTables => jsonLoader.GetSetting<bool>("ImportTables", _settings);
        public bool ImportLocalEdits => jsonLoader.GetSetting<bool>("ImportLocalEdits", _settings);
        public bool KeepLocalItems => jsonLoader.GetSetting<bool>("KeepLocalItems", _settings);

        public ProcessDatabase()
        {
            // Action 2: Load Config
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            _settings = jsonLoader.LoadSettings(_settingsPath, SETTINGS_FILE);

            // Initialize DatabaseHelper
            ResidentDatabaseHelper = new DatabaseHelper(DatabaseType.ResidentDatabase);
            ProcessingDatabaseHelper = new DatabaseHelper(DatabaseType.ProcessDatabase);
            ImportDatabaseHelper = new DatabaseHelper(DatabaseType.ImportDatabase);
        }

        public async Task ProcessImport()
        {
            try
            {
                if (!GetProcessDatabaseFile())
                {
                    Logger.Error("Failed to copy database.");
                    return;
                }
                
                Logger.Info("Processing database refreshed from Source successfully.");

                ProcessingDatabaseHelper.DeleteAllProducts();
                Logger.Trace("Cleared tblProducts from Processing Database.");

                List<tblProducts> importTblProducts = ImportDatabaseHelper.GetProducts().ToList();

                importTblProducts = ImportDatabaseHelper.PopulatePageNum<tblProducts>(importTblProducts);

                Logger.Trace($"Retrieved {importTblProducts.Count} products from Import Database. Moving to process...");
                if (!ProcessingDatabaseHelper.BulkInsertOrUpdate(importTblProducts))
                {
                    Logger.Error("Failed to upsert into tblProducts.");
                    return;
                }
                Logger.Info($"Completed Bulk Upsert into tblProducts");

                if (ImportLocalEdits)
                {
                    if (!ProcessingDatabaseHelper.UpdateLocalEdits())
                    {
                        Logger.Error("Failed to update Local Edits.");
                        return;
                    }
                    Logger.Info($"Completed update of Local Edits");                    
                }

                if (KeepLocalItems)
                {
                    if (!ProcessingDatabaseHelper.InsertLocalItems())
                    {
                        Logger.Error("Failed to insert Local Items.");
                        return;
                    }
                    Logger.Info($"Completed insert of Local Items");
                }
                else if (!KeepLocalItems)
                {
                    if (!ProcessingDatabaseHelper.DeleteLocalItems())
                    {
                        Logger.Error("Failed to delete Local Items.");
                        return;
                    }
                    Logger.Info($"Completed deletion of Local Items");
                }

                if (ImportTables)
                    ProcessingDatabaseHelper.InsertHierarchyTables(UseLegacy);

                CopyProcessDatabaseToResident();

                return;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing import - {ex.Message}");
            }
        }

        private bool GetProcessDatabaseFile()
        {
            try
            {
                // if the AdminConsole database exists, and the UseAdminConsoleDatabase is true, use that, or if BaseDatabase is true and exists, use that, otherwise use the ResidentDatabase
                string sourcePath = UseAdminConsoleDatabase && File.Exists(AdminConsoleDatabase) ? AdminConsoleDatabase :
                                    UseBaseDatabase && File.Exists(ImporterDatabase) ? ImporterDatabase :
                                    ResidentDatabase;

                // destination path is set in the settings file
                string destPath = ProcessingDatabase;

                File.Copy(sourcePath, destPath, true);
                Logger.Info("Database copied successfully from " + sourcePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error copying database: {ex.Message}");
                return false;
            }
        }

        private bool CopyProcessDatabaseToResident()
        {
            try
            {
                // if the AdminConsole database exists, and the UseAdminConsoleDatabase is true, use that, or if BaseDatabase is true and exists, use that, otherwise use the ResidentDatabase
                string sourcePath = ProcessingDatabase;

                // destination path is set in the settings file
                string destPath = ResidentDatabase;

                File.Copy(sourcePath, destPath, true);
                Logger.Info("Database copied successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error copying database: {ex.Message}");
                return false;
            }
        }
    }
}
