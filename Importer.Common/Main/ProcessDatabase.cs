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
        private DatabaseHelper AdminDatabaseHelper;
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
            AdminDatabaseHelper = new DatabaseHelper(DatabaseType.AdminConsoleDatabase);
            ProcessingDatabaseHelper = new DatabaseHelper(DatabaseType.ProcessDatabase);
            ImportDatabaseHelper = new DatabaseHelper(DatabaseType.ImportDatabase);
        }

        public async Task ProcessImport()
        {
            // Copy Resident/AdminConsole/Base Database to Processing Database
            if (!GetProcessDatabaseFile())
            {
                Logger.LogErrorEvent("Failed to copy database.");
                return;
            }

            Logger.LogInfoEvent("Processing database copied successfully.");

            List<tblProducts> importTblProducts = ImportDatabaseHelper.GetProducts().ToList();

            //TODO All of the below steps should happen in memory, and anything to do with the database should be put in DatabaseHelper
            //Basic idea is doing all this in memory and then committing once per database table

            // Action 10: Import data from Importer to Processing (tblProducts)
            ProcessingDatabaseHelper.DeleteAllProducts();
            if (UseAdminConsoleDatabase)
                ProcessingDatabaseHelper.BulkInsertOrUpdate(AdminDatabaseHelper.GetProducts().ToList());
            else
                ProcessingDatabaseHelper.BulkInsertOrUpdate(ResidentDatabaseHelper.GetProducts().ToList());

            ProcessingDatabaseHelper.BulkInsertOrUpdate(importTblProducts);

            // Action 17: Update Local Edits Before Applying tblProducts to Processing Database

            //TODO DatabaseHelper needs the ability to get the Edit_Fields from LocalEditFields, as well as the ability to
            //insert just what we need from the Edit_Fields

            //Can make one method for importing tables like this and send the table types
            // Actions 11-14: Import Tables (tblDepartments, tblClasses, tblCategories)

            //Can make one method for UpdatePageNum, and I just send the table type (like classes, categories, products)
            // Actions 19-22: If Legacy is enabled, Update PageNum (make these as part of the above methods)

            // Action 25: Copy to Resident Database

            // Purge Log Files


            return;
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
                Logger.LogInfoEvent("Database copied successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogErrorEvent($"Error copying database: {ex.Message}");
                return false;
            }
        }

    }
}
