using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Main;
using Importer.Common.Models;
using Importer.Common.Services;
using Importer.Module.Parsley.Models;
using Importer.Module.Parsley.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley
{
    public class ParsleyModule : IImporterModule
    {
        public string Name { get; set; } = "Parsley";
        public string Version { get; set; } = "1.0.0";
        public ImporterInstance ImporterInstance { get; set; }
        public string ImporterTypeData { get; set; }
        public tblProducts ProductTemplate { get; set; }
        public bool Flush { get; set; }

        ICustomerProcess _customerProcess;
        RestAPIMonitor _importerType;
        ParsleyJSONParser parser = null;
        SchedulerService schedulerService;

        public int GetPendingFileCount()
        {
            throw new NotImplementedException();
        }

        public List<tblProducts> GetTblProductsDeleteList()
        {
            throw new NotImplementedException();
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

            _importerType = new RestAPIMonitor(this);

            SetupImporterType();
        }
        public void SetupImporterType()
        {
            // Use SchedulerService to set up the RestAPIMonitor
            schedulerService = new SchedulerService();
            schedulerService.ScheduleJob<RestAPIMonitor>("Parsley", new TimeSpan(0,5,0));
        }
        public async void StartModule()
        {
            // Start the file watcher
            if (_importerType != null)
            {
                //this will start the quartz scheduler for the RestAPIMonitor
                await schedulerService.StartSchedulerAsync();
            }
            else
            {
                Logger.Error("Rest API Monitor is not initialized.");
            }
        }
        public async void TriggerProcess()
        {
            parser = new ParsleyJSONParser(ProductTemplate, _customerProcess);

            parser.ParseMenuItemSimpleList(ImporterTypeData.ToString());
            parser.ConvertMenuItemsToPLURecords();

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
