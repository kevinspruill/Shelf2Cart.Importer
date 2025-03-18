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
        public string Name { get; set; } = "Invafresh Importer Module";
        public string Version { get; set; } = "3.0.0";
        public bool IsEnabled { get; set; } = true;
        public ImporterType Type { get; set; } = ImporterType.File;
        public ImporterTrigger Trigger { get; set; } = ImporterTrigger.Auto;
        public string TriggerValue { get; set; } = string.Empty;

        FileImport _fileImport;
        public InvafreshSettingsLoader Settings { get; set; } = new InvafreshSettingsLoader();

        public InvafreshModule()
        {

        }

        public void Execute()
        {
           
        }

        public List<tblProducts> GetTblProductsList(tblProducts productTemplate)
        {
            var products = new List<tblProducts>();
            var parser = new HostchngParser(productTemplate);
            var filedata = _fileImport.ReadFileContent("811-Master_Export.txt");
            var parseddata = parser.ParseFile("811-Master_Export.txt");

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
