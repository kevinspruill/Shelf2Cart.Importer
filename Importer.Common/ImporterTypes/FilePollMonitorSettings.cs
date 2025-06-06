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
        public string DatabaseFile { get; set; } = "FileHistory.db";
        public HashSet<string> AllowedExtensions { get; set; }
        public bool UseLogin { get; set; } = false;
        public string LoginUsername { get; set; } = string.Empty;
        public string LoginPassword { get; set; } = string.Empty;
        public string LoginDomain { get; set; } = string.Empty;

    }
}
