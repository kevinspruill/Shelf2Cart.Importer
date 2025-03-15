using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    public class IngredientItemRecord : BaseRecord
    {
        public int? PluNumber { get; set; }  // PNO
        public int IngredientNumber { get; set; }  // INO - Required
        public string IngredientText { get; set; }  // ITE - Required
        public int? IngredientTextFontSize { get; set; }  // IS1
        public string LinkedIngredientNumbers { get; set; }  // LNK
        public string ModifiedFlag { get; set; }  // MOD ('N'/'A')
    }
}
