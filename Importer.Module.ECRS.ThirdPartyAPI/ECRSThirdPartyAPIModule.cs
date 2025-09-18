using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Main;
using Importer.Common.Models;
using Importer.Common.Models.TypeSettings;
using Importer.Common.QuartzJobs;
using Importer.Common.Registries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.ThirdPartyAPI
{
    public class ECRSThirdPartyAPIModule : IImporterModule
    {
        public string Name { get; set; } = "ECRS - Third Party API Client";
        public string Version { get; set; } = "5.7.155";
        public ImporterInstance ImporterInstance { get; set; }
        public string ImporterTypeData { get; set; }
        public tblProducts ProductTemplate { get; set; } = new tblProducts();
        public bool ProcessQueued { get; set; } = false;
        public bool Flush { get; set; }

        ICustomerProcess _customerProcess;
        ECRSThirdPartyAPIJSONParser parser = null;
        FileMonitor _importerType;

        public int GetPendingFileCount()
        {
            return _importerType?.GetQueuedFileCount() ?? 0;
        }
        public List<tblProducts> GetTblProductsDeleteList()
        {
            return new List<tblProducts>();
        }
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
        public void InitModule(ImporterInstance importerInstance)
        {

            ImporterInstance = importerInstance;
            _customerProcess = InstanceLoader.GetCustomerProcess(importerInstance.CustomerProcess);

            _importerType = new FileMonitor(this);

            // Register this module instance
            ImporterModuleRegistry.Modules[this.GetType().AssemblyQualifiedName] = this;

            ProductTemplate = ProductTemplate.CreateProductTemplate();

            SetupImporterType();
        }
        public void SetupImporterType()
        {
            // No need to set up the importer type here, as it's done in the constructor of FileMonitor
        }
        public async void StartModule()
        {
            // Start the file watcher
            if (_importerType != null)
            {
                _importerType.Start();
            }
            else
            {
                Logger.Error("File Watcher is not initialized.");
            }
        }
        public async Task<bool> TriggerProcess()
        {
            parser = new ECRSThirdPartyAPIJSONParser(ProductTemplate, _customerProcess);
            parser.ParseFile(ImporterTypeData.ToString());

            if (parser.PLURecords == null || parser.PLURecords.Count == 0)
            {
                Logger.Error("No PLU records found in the file.");
                return false;
            }
            else
            {
                await MainProcess.ProcessAsync(this);
                ProcessQueued = true;
                return true;
            }
        }

        public void StopModule()
        {
            // Stop the file watcher
            if (_importerType != null)
            {
                //_importerType.Stop();

                //This will stop the quartz scheduler
            }
            else
            {
                Logger.Error("Rest API Monitor is not initialized.");
            }
        }
    }
}
