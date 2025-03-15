using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    // Alternative model for legacy nutrition format
    public class LegacyNutritionItemRecord : BaseRecord
    {
        public int NutritionNumber { get; set; }  // NTN - Required
        public int? LabelFormatNumber { get; set; }  // LF1
        public string ServingsPerContainer { get; set; }  // SPC
        public string ServingSizeDescription { get; set; }  // SSZ
        public List<NutritionEntry> NutritionEntries { get; set; } = new List<NutritionEntry>();
    }
}
