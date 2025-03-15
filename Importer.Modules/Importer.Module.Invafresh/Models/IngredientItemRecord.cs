using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    public class IngredientItemRecord : BaseRecord
    {
        [FieldTag("PNO")]
        public int? PluNumber { get; set; }  // PNO
        [FieldTag("INO", true)]
        public int IngredientNumber { get; set; }  // INO - Required
        [FieldTag("ITE", true)]
        public string IngredientText { get; set; }  // ITE - Required
        [FieldTag("IS1")]
        public int? IngredientTextFontSize { get; set; }  // IS1
        [FieldTag("LNK")]
        public string LinkedIngredientNumbers { get; set; }  // LNK
        [FieldTag("MOD")]
        public string ModifiedFlag { get; set; }  // MOD ('N'/'A')
    }
}
