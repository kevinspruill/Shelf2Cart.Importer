using Importer.Common.Interfaces;
using Importer.Common.Models.TypeSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Models
{
    public class ImporterInstance
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImporterType { get; set; }
        public string ImporterModule { get; set; }
        public string CustomerProcess { get; set; }
        public bool Enabled { get; set; }
        public ImporterTypeSettings TypeSettings { get; set; }

    }
}
