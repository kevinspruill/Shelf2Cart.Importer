using Importer.Common;
using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
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

        public InvafreshModule()
        {
            //_fileImport = new FileImport(this);
        }

        public void Execute()
        {
            _fileImport = new FileImport(this);
            var filedata = _fileImport.ReadFileContent("D:\\811-Master_Export.txt");
            var parser = new HostchngParser();

            var parseddate = parser.ParseFile("D:\\811-Master_Export.txt");
        }

        public void Initialize()
        {
            _fileImport.StartWatching();
        }

        public void Terminate()
        {
            _fileImport.StopWatching();
        }
    }
}
