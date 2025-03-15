using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    public class NutritionEntry
    {
        [FieldTag("NENN")]
        public string NutritionType { get; set; }  // NENN
        [FieldTag("NEV")]
        public int? Value { get; set; }  // NEV
        [FieldTag("NEP")]
        public int? PercentageValue { get; set; }  // NEP
    }
}
