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
        public List<tblProducts> GetTblProductsList(tblProducts productTemplate)
        {
            return new List<tblProducts>();
        }

        public void InitModule(ImporterInstance importerInstance)
        {
            throw new NotImplementedException();
        }
    }
}
