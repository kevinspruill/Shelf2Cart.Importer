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
        tblProducts ProductTemplate { get; set; }
        bool ProcessQueued { get; set; }
        bool Flush { get; set; }
        void InitModule(ImporterInstance importerInstance);
        void SetupImporterType();
        void StartModule();
        Task<bool> TriggerProcess();
        List<tblProducts> GetTblProductsList();
        void StopModule();
        int GetPendingFileCount();
        List<tblProducts> GetTblProductsDeleteList();
    }
}
