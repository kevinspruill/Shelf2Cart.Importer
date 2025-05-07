using System;

namespace Importer.Modules.GrocerySignage.Models
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ImportDBFieldAttribute : Attribute
    {
        public string Name { get; }
        public ImportDBFieldAttribute(string name)
        {
            Name = name;
        }
    }
    public class MMScaleGrocery
    {
        [ImportDBField("UPC")]
        public string UPC { get; set; }

        [ImportDBField("Description1")]
        public string Description1 { get; set; }

        [ImportDBField("Description2")]
        public string Description2 { get; set; }

        [ImportDBField("LableDescription1")]
        public string LableDescription1 { get; set; }

        [ImportDBField("SignDescription1")]
        public string SignDescription1 { get; set; }

        [ImportDBField("VendorNum")]
        public string VendorNum { get; set; }

        [ImportDBField("VendorName")]
        public string VendorName { get; set; }

        [ImportDBField("Deptnum")]
        public string Deptnum { get; set; }

        [ImportDBField("Deptname")]
        public string Deptname { get; set; }

        [ImportDBField("UOM")]
        public string UOM { get; set; }

        [ImportDBField("Size")]
        public string Size { get; set; }

        [ImportDBField("Priceqty")]
        public string Priceqty { get; set; }

        [ImportDBField("Pricetype")]
        public string Pricetype { get; set; }

        [ImportDBField("Pricetypedesc")]
        public string Pricetypedesc { get; set; }

        [ImportDBField("Price")]
        public string Price { get; set; }

        [ImportDBField("Pricestartdate")]
        public string Pricestartdate { get; set; }

        [ImportDBField("Priceenddate")]
        public string Priceenddate { get; set; }

        [ImportDBField("Regpriceqty")]
        public string Regpriceqty { get; set; }

        [ImportDBField("Regprice")]
        public string Regprice { get; set; }

        [ImportDBField("Aisle")]
        public string Aisle { get; set; }

        [ImportDBField("UOMP")]
        public string UOMP { get; set; }

        [ImportDBField("SIGNCOUNT")]
        public string SIGNCOUNT { get; set; }


        public MMScaleGrocery Clone()
        {
            return (MMScaleGrocery)this.MemberwiseClone();
        }

    }    
    
}