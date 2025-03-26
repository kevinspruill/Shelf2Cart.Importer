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
        public string Name { get; set; } = "Invafresh";
        public string Version { get; set; } = "3.0.0";
        
        public InvafreshSettingsLoader Settings { get; set; } = new InvafreshSettingsLoader();
        public Dictionary<string, object> TypeSettings { get; set; } = new Dictionary<string, object>();
        
        ICustomerProcess _customerProcess;
        string _filePath = string.Empty;
        FileWatcher _importerType;

        public List<tblProducts> GetTblProductsList(tblProducts productTemplate)
        {
            var products = new List<tblProducts>();

            var parser = new HostchngParser(productTemplate, _customerProcess);
            parser.ParseFile(_filePath.ToString());

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

        public void InitModule(ImporterInstance importerInstance)
        {
            _importerType = new FileWatcher();
            _importerType.ApplySettings(importerInstance.TypeSettings);
            _filePath = _importerType.FilePath;
            _customerProcess = InstanceLoader.GetCustomerProcess(importerInstance.CustomerProcess);

            SetupImporterType();
        }

        public void SetupImporterType()
        {
            if (_importerType != null)
            {
                _importerType.FilePath = TypeSettings.TryGetValue("FilePath", out object filePath) ? filePath.ToString() : string.Empty;
                _importerType.FileName = TypeSettings.TryGetValue("FileName", out object fileName) ? fileName.ToString() : string.Empty;
                _importerType.FileFilter = TypeSettings.TryGetValue("FileFilter", out object fileFilter) ? fileFilter.ToString() : string.Empty;

            }
            else
            {
                Logger.Error("Importer Type is not initialized.");
            }
        }
    }
}
