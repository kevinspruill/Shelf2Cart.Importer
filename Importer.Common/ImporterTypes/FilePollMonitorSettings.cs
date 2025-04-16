using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.ImporterTypes
{
    public class FilePollMonitorSettings
    {
        public string TargetPath { get; set; }
        public int PollIntervalMilliseconds { get; set; }
        public string DatabaseFile { get; set; }
        public HashSet<string> AllowedExtensions { get; set; }
    }
}
