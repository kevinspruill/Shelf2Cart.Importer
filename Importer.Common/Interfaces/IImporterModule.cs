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
        bool IsEnabled { get; set; }
        ImporterType Type { get; set; }
        ImporterTrigger Trigger { get; set; }
        string TriggerValue { get; set; } // Path if ImporterType is File, or URL if ImporterType is API
        void Initialize();
        void Execute();
        List<tblProducts> GetTblProductsList();
        void Terminate();
    }
}
