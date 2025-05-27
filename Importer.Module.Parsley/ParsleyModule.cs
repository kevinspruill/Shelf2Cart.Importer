using Importer.Common.Interfaces;
using Importer.Common.Models;
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
            throw new NotImplementedException();
        }

        public void InitModule(ImporterInstance importerInstance)
        {
            throw new NotImplementedException();
        }

        public void SetupImporterType()
        {
            throw new NotImplementedException();
        }

        public void StartModule()
        {
            throw new NotImplementedException();
        }

        public void StopModule()
        {
            throw new NotImplementedException();
        }

        public void TriggerProcess()
        {
            throw new NotImplementedException();
        }
    }
}
