using Importer.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Models.TypeSettings
{
    public class SchedulerServiceSettings : ImporterTypeSettings
    {
        public override string Type { get; set; } = "SchedulerService";
        public string ScheduleType { get; set; } // "Cron" or "TimeSpan"
        public string ScheduleInterval { get; set; } // Cron expression or TimeSpan string
    }
}
