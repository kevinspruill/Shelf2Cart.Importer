using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley.Models
{
    public class NutritionalInfo
    {
        public bool IsPackaged { get; set; }
        public ServingSize ServingSize { get; set; }
        public double? ServingsPerPackage { get; set; }
        public string NutritionServingSize { get; set; }
        public bool Incomplete { get; set; }
        public Dictionary<string, Nutrient> Nutrients { get; set; }
        public Allergens Allergens { get; set; }
        public string AllergenString { get; set; }
        public Characteristics Characteristics { get; set; }
        public string Ingredients { get; set; }
        public List<IngredientListItem> IngredientList { get; set; }
    }
}
