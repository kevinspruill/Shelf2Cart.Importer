using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Interfaces
{
    public interface IImporterModule
    {
        string Name { get; set; }
        string Version { get; set; }
        ImporterInstance ImporterInstance { get; set; }
        string ImporterTypeData { get; set; }
        void InitModule(ImporterInstance importerInstance);
        void SetupImporterType();
        void StartModule();
        void TriggerProcess();
        List<tblProducts> GetTblProductsList(tblProducts productTemplate);
        void StopModule();

    }
}
