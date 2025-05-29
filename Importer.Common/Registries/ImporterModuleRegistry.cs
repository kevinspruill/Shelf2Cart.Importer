using Importer.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Registries
{
    public static class ImporterModuleRegistry
    {
        public static Dictionary<string, IImporterModule> Modules = new Dictionary<string, IImporterModule>();
    }
}
