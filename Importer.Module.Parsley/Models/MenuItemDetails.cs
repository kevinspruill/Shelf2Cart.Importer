using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Parsley.Models
{
    public class MenuItemDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string Photo { get; set; }
        public string HeatingInstructionOven { get; set; }
        public string HeatingInstructionMicrowave { get; set; }
        public bool? IsSubrecipe { get; set; }
        public string Type { get; set; }
        public List<string> SupportedUnits { get; set; }
        public double? Cost { get; set; }
        public double? Price { get; set; }
        public DateTime? LastModified { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, object> CustomTags { get; set; }
        public string ChefID { get; set; }
        public int? ChefParsleyId { get; set; }
        public string ShelfLife { get; set; }
        public NutritionalInfo NutritionalInfo { get; set; }
        public string ItemNumber { get; set; }
        public string ManagementItemNumber { get; set; }
    }
}
