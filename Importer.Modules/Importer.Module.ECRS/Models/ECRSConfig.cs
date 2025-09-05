using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.Models
{
    public class ECRSConfig
    {
        public string QuerySchedule { get; set; }
        public string LastFullLoad { get; set; }
        public int FullLoadIntervalHours { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public Dictionary<string, string> QueryParameters { get; set; }
    }
}
