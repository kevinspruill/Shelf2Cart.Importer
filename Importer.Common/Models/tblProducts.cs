using System;

namespace Importer.Common.Models
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
    public class tblProducts
    {
            [ImportDBField("Barcode")]
            public string Barcode { get; set; } = string.Empty;

            [ImportDBField("BarType")]
            public string BarType { get; set; } = string.Empty;

            [ImportDBField("Button 1")]
            public string Button1 { get; set; } = string.Empty;

            [ImportDBField("Button 2")]
            public string Button2 { get; set; } = string.Empty;

            [ImportDBField("CategoryNum")]
            public string CategoryNum { get; set; } = string.Empty;

            [ImportDBField("DateEn")]
            public bool DateEn { get; set; } = false;

            [ImportDBField("Dept")]
            public string Dept { get; set; } = string.Empty;

            //[ImportDBField("DeptNum")]
            //public string DeptNum { get; set; } = string.Empty;

            [ImportDBField("Description 1")]
            public string Description1 { get; set; } = string.Empty;

            [ImportDBField("Description 10")]
            public string Description10 { get; set; } = string.Empty;

            [ImportDBField("Description 11")]
            public string Description11 { get; set; } = string.Empty;

            [ImportDBField("Description 12")]
            public string Description12 { get; set; } = string.Empty;

            [ImportDBField("Description 13")]
            public string Description13 { get; set; } = string.Empty;

            [ImportDBField("Description 14")]
            public string Description14 { get; set; } = string.Empty;

            [ImportDBField("Description 15")]
            public string Description15 { get; set; } = string.Empty;

            [ImportDBField("Description 16")]
            public string Description16 { get; set; } = string.Empty;

            [ImportDBField("Description 2")]
            public string Description2 { get; set; } = string.Empty;

            [ImportDBField("Description 3")]
            public string Description3 { get; set; } = string.Empty;

            [ImportDBField("Description 4")]
            public string Description4 { get; set; } = string.Empty;

            [ImportDBField("Description 5")]
            public string Description5 { get; set; } = string.Empty;
            
            [ImportDBField("Description 6")]
            public string Description6 { get; set; } = string.Empty;

            [ImportDBField("Description 7")]
            public string Description7 { get; set; } = string.Empty;

            [ImportDBField("Description 8")]
            public string Description8 { get; set; } = string.Empty;

            [ImportDBField("Description 9")]
            public string Description9 { get; set; } = string.Empty;

            [ImportDBField("IngredientNum")]
            public string IngredientNum { get; set; } = string.Empty;

            [ImportDBField("Ingredients")]
            public string Ingredients { get; set; } = string.Empty;

            [ImportDBField("LblName")]
            public string LblName { get; set; } = string.Empty;

            [ImportDBField("LblName2")]
            public string LblName2 { get; set; } = string.Empty;

            [ImportDBField("LblName3")]
            public string LblName3 { get; set; } = string.Empty;

            [ImportDBField("LblName4")]
            public string LblName4 { get; set; } = string.Empty;

            [ImportDBField("LblName5")]
            public string LblName5 { get; set; } = string.Empty;

            [ImportDBField("NetWt")]
            public string NetWt { get; set; } = string.Empty;

            [ImportDBField("NewItemFlg")]
            public bool NewItemFlg { get; set; } = false;

            [ImportDBField("NF 1")]
            public string NF1 { get; set; } = string.Empty;

            [ImportDBField("NF 10")]
            public string NF10 { get; set; } = string.Empty;

            [ImportDBField("NF 11")]
            public string NF11 { get; set; } = string.Empty;

            [ImportDBField("NF 12")]
            public string NF12 { get; set; } = string.Empty;

            [ImportDBField("NF 13")]
            public string NF13 { get; set; } = string.Empty;

            [ImportDBField("NF 14")]
            public string NF14 { get; set; } = string.Empty;

            [ImportDBField("NF 15")]
            public string NF15 { get; set; } = string.Empty;

            [ImportDBField("NF 16")]
            public string NF16 { get; set; } = string.Empty;

            [ImportDBField("NF 17")]
            public string NF17 { get; set; } = string.Empty;

            [ImportDBField("NF 18")]
            public string NF18 { get; set; } = string.Empty;

            [ImportDBField("NF 19")]
            public string NF19 { get; set; } = string.Empty;

            [ImportDBField("NF 2")]
            public string NF2 { get; set; } = string.Empty;

            [ImportDBField("NF 20")]
            public string NF20 { get; set; } = string.Empty;

            [ImportDBField("NF 3")]
            public string NF3 { get; set; } = string.Empty;

            [ImportDBField("NF 4")]
            public string NF4 { get; set; } = string.Empty;

            [ImportDBField("NF 5")]
            public string NF5 { get; set; } = string.Empty;

            [ImportDBField("NF 6")]
            public string NF6 { get; set; } = string.Empty;

            [ImportDBField("NF 7")]
            public string NF7 { get; set; } = string.Empty;

            [ImportDBField("NF 8")]
            public string NF8 { get; set; } = string.Empty;

            [ImportDBField("NF 9")]
            public string NF9 { get; set; } = string.Empty;

            [ImportDBField("NF Desc")]
            public string NFDesc { get; set; } = string.Empty;

            [ImportDBField("NFCalcium")]
            public string NFCalcium { get; set; } = string.Empty;

            [ImportDBField("NFCalciummcg")]
            public string NFCalciummcg { get; set; } = string.Empty;

            [ImportDBField("NFCalories")]
            public string NFCalories { get; set; } = string.Empty;

            [ImportDBField("NFCaloriesFromFat")]
            public string NFCaloriesFromFat { get; set; } = string.Empty;

            [ImportDBField("NFCholesterol")]
            public string NFCholesterol { get; set; } = string.Empty;

            [ImportDBField("NFCholesterolMG")]
            public string NFCholesterolMG { get; set; } = string.Empty;

            [ImportDBField("NFDietFiber")]
            public string NFDietFiber { get; set; } = string.Empty;

            [ImportDBField("NFIron")]
            public string NFIron { get; set; } = string.Empty;

            [ImportDBField("NFIronmcg")]
            public string NFIronmcg { get; set; } = string.Empty;

            [ImportDBField("NFPotassium")]
            public string NFPotassium { get; set; } = string.Empty;

            [ImportDBField("NFPotassiummcg")]
            public string NFPotassiummcg { get; set; } = string.Empty;

            [ImportDBField("NFProtein")]
            public string NFProtein { get; set; } = string.Empty;

            [ImportDBField("NFSatFatG")]
            public string NFSatFatG { get; set; } = string.Empty;

            [ImportDBField("NFServingSize")]
            public string NFServingSize { get; set; } = string.Empty;

            [ImportDBField("NFSodium")]
            public string NFSodium { get; set; } = string.Empty;

            [ImportDBField("NFSodiumMG")]
            public string NFSodiumMG { get; set; } = string.Empty;

            [ImportDBField("NFSugars")]
            public string NFSugars { get; set; } = string.Empty;

            [ImportDBField("NFSugarsAdded")]
            public string NFSugarsAdded { get; set; } = string.Empty;

            [ImportDBField("NFSugarsAddedG")]
            public string NFSugarsAddedG { get; set; } = string.Empty;

            [ImportDBField("NFTotalFat")]
            public string NFTotalFat { get; set; } = string.Empty;

            [ImportDBField("NFTotalFatG")]
            public string NFTotalFatG { get; set; } = string.Empty;

            [ImportDBField("NFTotCarbo")]
            public string NFTotCarbo { get; set; } = string.Empty;

            [ImportDBField("NFTotCarboG")]
            public string NFTotCarboG { get; set; } = string.Empty;

            [ImportDBField("NFVitA")]
            public string NFVitA { get; set; } = string.Empty;

            [ImportDBField("NFVitC")]
            public string NFVitC { get; set; } = string.Empty;

            [ImportDBField("NFVitD")]
            public string NFVitD { get; set; } = string.Empty;

            [ImportDBField("NFVitDmcg")]
            public string NFVitDmcg { get; set; } = string.Empty;

            [ImportDBField("NutriFactEn")]
            public bool NutriFactEn { get; set; } = false;

            [ImportDBField("NutrifactNum")]
            public string NutrifactNum { get; set; } = string.Empty;

            //[ImportDBField("PageNum")]
            //public string PageNum { get; set; } = string.Empty;

            [ImportDBField("PLU")]
            public string PLU { get; set; } = string.Empty;

            [ImportDBField("Price")]
            public string Price { get; set; } = string.Empty;

            [ImportDBField("PricePerLbEn")]
            public bool PricePerLbEn { get; set; } = false;

            [ImportDBField("ProdID")]
            public string ProdID { get; set; } = string.Empty;

            [ImportDBField("QtyEn")]
            public bool QtyEn { get; set; } = false;

            [ImportDBField("Sale End")]
            public DateTime? SaleEnd { get; set; }

            [ImportDBField("Sale Price")]
            public string SalePrice { get; set; } = string.Empty;

            [ImportDBField("Sale Start")]
            public DateTime? SaleStart { get; set; }

            [ImportDBField("Scaleable")]
            public bool Scaleable { get; set; } = false;

            [ImportDBField("Shelf Life")]
            public string ShelfLife { get; set; } = string.Empty;

            [ImportDBField("Shelf Life Type")]
            public string ShelfLifeType { get; set; } = string.Empty;

            [ImportDBField("StockNum")]
            public string StockNum { get; set; } = string.Empty;

            [ImportDBField("StoreIDEn")]
            public bool StoreIDEn { get; set; } = false;

            [ImportDBField("Tare")]
            public string Tare { get; set; } = string.Empty;

            [ImportDBField("VID1")]
            public string VID1 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 1")]
            public string ZonePrice1 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 10")]
            public string ZonePrice10 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 11")]
            public string ZonePrice11 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 12")]
            public string ZonePrice12 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 13")]
            public string ZonePrice13 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 14")]
            public string ZonePrice14 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 15")]
            public string ZonePrice15 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 16")]
            public string ZonePrice16 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 17")]
            public string ZonePrice17 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 18")]
            public string ZonePrice18 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 19")]
            public string ZonePrice19 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 2")]
            public string ZonePrice2 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 20")]
            public string ZonePrice20 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 3")]
            public string ZonePrice3 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 4")]
            public string ZonePrice4 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 5")]
            public string ZonePrice5 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 6")]
            public string ZonePrice6 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 7")]
            public string ZonePrice7 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 8")]
            public string ZonePrice8 { get; set; } = string.Empty;

            [ImportDBField("Zone Price 9")]
            public string ZonePrice9 { get; set; } = string.Empty;


        public tblProducts Clone()
        {
            return (tblProducts)this.MemberwiseClone();
        }

    }    
    
}