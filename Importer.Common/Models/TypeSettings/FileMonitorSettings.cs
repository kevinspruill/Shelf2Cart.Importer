using Importer.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Models.TypeSettings
{
    public class FileMonitorSettings : ImporterTypeSettings
    {
        public override string Type { get; set; } = "FileMonitor";
        public string TargetPath { get; set; } = string.Empty;
        public int PollIntervalMilliseconds { get; set; } = 150;
        public string DatabaseFile { get; set; } = "FileHistory.db";
        public HashSet<string> AllowedExtensions { get; set; } = new HashSet<string>();
        public bool IsAdminFile { get; set; } = false;
        public bool UseLogin { get; set; } = false;
        public string LoginUsername { get; set; } = string.Empty;
        public string LoginPassword { get; set; } = string.Empty;
        public string LoginDomain { get; set; } = string.Empty;

    }
}
