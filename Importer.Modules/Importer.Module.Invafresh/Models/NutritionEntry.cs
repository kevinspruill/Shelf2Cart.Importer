using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    public class NutritionEntry
    {
        public string NutritionType { get; set; }  // NENN
        public int? Value { get; set; }  // NEV
        public int? PercentageValue { get; set; }  // NEP
    }
}
