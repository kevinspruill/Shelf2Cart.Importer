using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Modules.GrocerySignage.Models
{
    public class GrocerySignageItem
    {
        public string UPC {  get; set; }
        public string Description1 { get; set; }
        public string Description2 { get; set; }
        public string LableDescription1 { get; set; }
        public string SignDescription1 { get; set; }
        public string VendorNum {  get; set; }
        public string VendorName { get; set; }
        public string Deptnum { get; set; }
        public string Deptname { get; set; }
        public string UOM {  get; set; }
        public string Size { get; set; }
        public string Priceqty { get; set; }
        public string Pricetype { get; set; }
        public string Pricetypedesc { get; set; }
        public string Price {  get; set; }
        public string Pricestartdate { get; set; }
        public string Priceenddate { get; set; }
        public string Regpriceqty { get; set; }
        public string Regprice { get; set; }
        public string Aisle { get; set; }
        public string UOMP { get; set; }
        public string SIGNCOUNT { get; set; }
    }
}
