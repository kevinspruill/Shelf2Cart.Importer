using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Main;
using Importer.Common.Models;
using Importer.Module.Generic.Interfaces;
using Importer.Module.Generic.Parser;
using Importer.Module.Invafresh.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Generic
{
    public class GenericModule : IImporterModule
    {
        public string Name { get; set; } = "Generic";
        public string Version { get; set; } = "2.0.0";
        public ImporterInstance ImporterInstance { get; set; }
        public tblProducts ProductTemplate { get; set; } = new tblProducts();
        public bool Flush { get; set; }
        public string ImporterTypeData { get; set; } = string.Empty;

        ICustomerProcess _customerProcess;
        FilePollMonitor _importerType;
        IParser parser = null;

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
                Logger.Error("Failed to convert PLU Delete records to tblProducts");
            }

            return productsToDelete;
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

            _importerType = new FilePollMonitor(this);

            //Need to add the ProductTemplate earlier than Invafresh module, as it's used in parser
            ProductProcessor productProcessor = new ProductProcessor(_customerProcess);
            var productTemplate = productProcessor.CreateProductTemplate();

            ProductTemplate = productTemplate;

            SetupImporterType();
        }
        public void SetupImporterType()
        {
            // No need to set up the importer type here, as it's done in the constructor of FilePollMonitor
        }
        public void StartModule()
        {
            // Start the file watcher
            if (_importerType != null)
            {
                _importerType.Start();
            }
            else
            {
                Logger.Error("File Polling is not initialized.");
            }
        }
        public void TriggerProcess()
        {

            GenericSettingsLoader Settings = new GenericSettingsLoader();
            var parserSetting = Settings.Parser;

            switch (parserSetting)
            {
                case "JSON":
                    parser = new JsonParser(ProductTemplate, _customerProcess);
                    break;
                default:
                    parser = new TextParser(ProductTemplate, _customerProcess, Settings.FieldDelimiter);
                    break;
            }

            parser.ParseFile(ImporterTypeData.ToString());
            MainProcess.ProcessAsync(this).GetAwaiter().GetResult();
        }
        public void StopModule()
        {
            // Stop the file watcher
            if (_importerType != null)
            {
                _importerType.Stop();
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
