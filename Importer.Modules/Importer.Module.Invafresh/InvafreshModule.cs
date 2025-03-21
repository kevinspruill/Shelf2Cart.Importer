using Importer.Common;
using Importer.Common.Helpers;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
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

        private FileImport _fileImport;

        public string Name { get; set; } = "Invafresh Importer Module";
        public string Version { get; set; } = "3.0.0";
        public bool IsEnabled { get; set; } = true;
        public ImporterType Type { get; set; } = ImporterType.File;
        public ImporterTrigger Trigger { get; set; } = ImporterTrigger.Auto;
        public string TriggerValue { get; set; } = string.Empty;
        public InvafreshSettingsLoader Settings { get; set; } = new InvafreshSettingsLoader();

        public void Execute()
        {
            // Not implemented  
        }

        public List<tblProducts> GetTblProductsList(tblProducts productTemplate)
        {
            var products = new List<tblProducts>();
            var parser = new HostchngParser(productTemplate, CustomerProcessLoader.GetCustomerProcess());
            var filedata = _fileImport.ReadFileContent(TriggerValue);
            var parseddata = parser.ParseFile(TriggerValue);

            var convertedRecords = parser.ConvertPLURecordsToTblProducts();
            
            if(convertedRecords != null)
            {
                products.AddRange(convertedRecords);
            }
            else
            {
                Logger.Error("Failed to convert PLU records to tblProducts");
            }

            return products;

        }

        public void Initialize()
        {
            _fileImport = new FileImport(this);
            _fileImport.StartWatching();
        }

        public void Terminate()
        {
            _fileImport.StopWatching();
        }
    }
}
