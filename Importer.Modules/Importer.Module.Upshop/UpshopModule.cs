using Importer.Common.Interfaces;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Upshop
{
    public class UpshopModule : IImporterModule
    {
        public string Name { get; set; } = "Upshop";
        public string Version { get; set; } = "1.0.0";
        public ImporterInstance ImporterInstance { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ImporterTypeData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public tblProducts ProductTemplate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Flush { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
