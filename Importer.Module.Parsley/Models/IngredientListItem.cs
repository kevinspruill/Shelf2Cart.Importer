using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley.Models
{
    public class IngredientListItem
    {
        public double Id { get; set; }
        public double? ParentId { get; set; }
        public string LabelName { get; set; }
        public string Name { get; set; }
    }
}
