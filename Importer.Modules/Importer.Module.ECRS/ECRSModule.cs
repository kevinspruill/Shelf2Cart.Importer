using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Main;
using Importer.Common.Models;
using Importer.Common.Models.TypeSettings;
using Importer.Common.QuartzJobs;
using Importer.Common.Registries;
using Importer.Module.Parsley.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS
{
    public class ECRSModule : IImporterModule
    {
        public string Name { get; set; } = "ECRS";
        public string Version { get; set; } = "2.0.0";
        public ImporterInstance ImporterInstance { get; set; }
        public string ImporterTypeData { get; set; }
        public tblProducts ProductTemplate { get; set; } = new tblProducts();
        public bool Flush { get; set; }

        ICustomerProcess _customerProcess;
        ECRSJSONParser parser = null;
        SchedulerService _importerType;

        public int GetPendingFileCount()
        {
            return 0; //temporary
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

            _importerType = new SchedulerService(this);

            // Register this module instance
            ImporterModuleRegistry.Modules[this.GetType().AssemblyQualifiedName] = this;

            ProductTemplate = ProductTemplate.CreateProductTemplate();

            SetupImporterType();
        }
        public void SetupImporterType()
        {
            // Use SchedulerService to set up the GetAPIJob
            _importerType.ScheduleJob<GetAPIJob>("ECRS", new TimeSpan(1, 0, 0));
        }
        public async void StartModule()
        {
            // Start the file watcher
            if (_importerType != null)
            {
                //this will start the quartz scheduler for the GetAPIJob
                await _importerType.StartSchedulerAsync();
            }
            else
            {
                Logger.Error("Rest API Monitor is not initialized.");
            }
        }
        public async void TriggerProcess()
        {
            parser = new ECRSJSONParser(ProductTemplate, _customerProcess);
            parser.APIKey = ((SchedulerServiceSettings)ImporterInstance.TypeSettings).ApiKey.ToString();

            await MainProcess.ProcessAsync(this);
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
