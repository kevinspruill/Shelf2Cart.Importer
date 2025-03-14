using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Models
{
    public class CleanseRule
    {
        public string SearchText { get; set; }
        public string InsertField { get; set; }
        public int CleanseType { get; set; }
    }
}
