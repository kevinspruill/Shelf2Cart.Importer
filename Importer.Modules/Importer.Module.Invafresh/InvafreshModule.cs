using Importer.Common;
using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Main;
using Importer.Common.Models;
using Importer.Module.Invafresh.Helpers;
using Importer.Module.Invafresh.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh
{
    public class InvafreshModule : IImporterModule
    {
        public string Name { get; set; } = "Invafresh";
        public string Version { get; set; } = "3.0.0";        
        public InvafreshSettingsLoader Settings { get; set; } = new InvafreshSettingsLoader();
        public Dictionary<string, object> TypeSettings { get; set; } = new Dictionary<string, object>();
        public ImporterInstance ImporterInstance { get; set; }
        public tblProducts ProductTemplate { get; set; } = new tblProducts();
        public bool Flush { get; set; }
        public string ImporterTypeData { get; set; } = string.Empty;

        ICustomerProcess _customerProcess;        
        FileWatcher _importerType;
        HostchngParser parser = null;

        public List<tblProducts> GetTblProductsList()
        {
            var products = new List<tblProducts>();
            var convertedRecords = parser.ConvertPLURecordsToTblProducts();

            if (convertedRecords != null)
            {
                products.AddRange(convertedRecords);
            }
            else
            {
                Logger.Error("Failed to convert PLU records to tblProducts");
            }

            return products;

        }
        public List<tblProducts> GetTblProductsDeleteList()
        {
            var productsToDelete = new List<tblProducts>();
            var deletedRecords = parser.ConvertPLUDeleteRecordsToTblProducts();

            if (deletedRecords != null)
            {
                productsToDelete.AddRange(deletedRecords);
            }
            else
            {
                Logger.Error("Failed to convert PLU records to tblProducts");
            }

            return productsToDelete;
        }
        public void InitModule(ImporterInstance importerInstance)
        {

            ImporterInstance = importerInstance;
            _customerProcess = InstanceLoader.GetCustomerProcess(importerInstance.CustomerProcess);

            _importerType = new FileWatcher(this);

            SetupImporterType();
        }
        public void SetupImporterType()
        {
            if (_importerType != null)
            {
                _importerType.ApplySettings(ImporterInstance.TypeSettings);
            }
            else
            {
                Logger.Error("File Watcher is not initialized.");
            }
        }
        public void StartModule()
        {
            // Start the file watcher
            if (_importerType != null)
            {
                _importerType.InitializeFileWatcher();
                _importerType.ToggleFileWatcher();
            }
            else
            {
                Logger.Error("File Watcher is not initialized.");
            }
        }
        public void TriggerProcess()
        {
            var parser = new HostchngParser(ProductTemplate, _customerProcess);
            parser.ParseFile(ImporterTypeData.ToString());
            MainProcess.ProcessAsync(this).GetAwaiter().GetResult();
        }
        public void StopModule()
        {
            // Stop the file watcher
            if (_importerType != null)
            {
                _importerType.ToggleFileWatcher();
            }
            else
            {
                Logger.Error("File Watcher is not initialized.");
            }
        }
        public int GetPendingFileCount()
        {
            return _importerType?.GetQueuedFileCount() ?? 0;
        }
    }
}
