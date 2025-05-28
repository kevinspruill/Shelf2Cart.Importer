using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley.Models
{
    public class Allergens
    {
        public bool? Milk { get; set; }
        public bool? Eggs { get; set; }
        public bool? Wheat { get; set; }
        public bool? Peanuts { get; set; }
        public bool? Soybeans { get; set; }
        public bool? Molluscs { get; set; }
        public bool? CerealsGluten { get; set; }
        public bool? Celery { get; set; }
        public bool? Mustard { get; set; }
        public bool? SesameSeeds { get; set; }
        public bool? SulphurDioxideSulphites { get; set; }
        public bool? Lupin { get; set; }
        public string Fish { get; set; }
        public string CrustaceanShellfish { get; set; }
        public string TreeNuts { get; set; }
    }
}
