using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Modules.GrocerySignage
{
    public class GrocerySignageModule : IImporterModule
    {
        public string Name { get; set; } = "GrocerySignage";
        public string Version { get; set; } = "2.0.0";
        public ImporterInstance ImporterInstance { get; set; }
        public tblProducts ProductTemplate { get; set; } = new tblProducts();
        public bool Flush { get; set; }
        public string ImporterTypeData { get; set; } = string.Empty;

        FilePollMonitor _importerType;

        public List<tblProducts> GetTblProductsDeleteList()
        {
            return null;
        }

        public List<tblProducts> GetTblProductsList()
        {
            return null;
        }

        public void InitModule(ImporterInstance importerInstance)
        {
            ImporterInstance = importerInstance;
            _importerType = new FilePollMonitor(this);

            SetupImporterType();
        }

        public void SetupImporterType()
        {
            // No specific setup required for Grocery Signage
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
                Logger.Error("File Watcher is not initialized.");
            }
        }

        public void TriggerProcess()
        {
            // Trigger process logic here, Will use custom Process for Grocery Signage DB
            // Will read either a Pipe Delimited or Excel file
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
                Logger.Error("File Polling is not initialized.");
            }
        }

        public int GetPendingFileCount()
        {
            return _importerType?.GetQueuedFileCount() ?? 0;
        }
    }
}
