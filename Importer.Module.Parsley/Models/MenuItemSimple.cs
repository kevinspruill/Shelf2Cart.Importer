using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley.Models
{
    public class MenuItemSimple
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ItemNumber { get; set; }
        public double? Price { get; set; }
        public DateTime? LastModified { get; set; }
        public List<string> Tags { get; set; }
        public bool? IsRecipeActive { get; set; }
        public string ChefID { get; set; }
        public int? ChefParsleyId { get; set; }
        public bool? IsSubrecipe { get; set; }
        public string Type { get; set; } // recipe | subrecipe | ingredient
    }
}
