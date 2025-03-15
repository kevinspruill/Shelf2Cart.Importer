using Importer.Module.Invafresh.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    public class PluItemRecord : BaseRecord
    {
        public int PluNumber { get; set; }  // PNO - Required
        public string UpcCode { get; set; }  // UPC - Required
        public string DescriptionLine1 { get; set; }  // DN1 - Required for SPIA
        public int? DescriptionSize1 { get; set; }  // DS1
        public string DescriptionLine2 { get; set; }  // DN2
        public int? DescriptionSize2 { get; set; }  // DS2
        public string DescriptionLine3 { get; set; }  // DN3
        public int? DescriptionSize3 { get; set; }  // DS3
        public string DescriptionLine4 { get; set; }  // DN4
        public int? DescriptionSize4 { get; set; }  // DS4
        public int? UnitPrice { get; set; }  // UPR - Required for SPIA and SPPC
        public int? FixedWeightAmount { get; set; }  // FWT
        public UnitOfMeasure? UnitOfMeasure { get; set; }  // UME - Required for SPIA
        public int? ByCountQuantity { get; set; }  // BCO
        public int? WrappedTareWeight { get; set; }  // WTA
        public int? UnwrappedTareWeight { get; set; }  // UTA
        public int? ShelfLife { get; set; }  // SLI
        public ShelfLifeType? ShelfLifeType { get; set; }  // SLT
        public int? UseBy { get; set; }  // EBY
        public int? CommodityClass { get; set; }  // CCL
        public string LogoNumbers { get; set; }  // LNU
        public string GraphicNumbers { get; set; }  // GNO
        public int? IngredientNumber { get; set; }  // INO
        public int? NutritionNumber { get; set; }  // NTN
        public int? AllergenNumber { get; set; }  // ALG
        public int? UserDefinedText1Number { get; set; }  // U1N
        public int? UserDefinedText2Number { get; set; }  // U2N
        public string ForcedTare { get; set; }  // FTA ('Y'/'N')
        public int? LabelFormatNumberOne { get; set; }  // LF1
        public int? LabelFormatNumberTwo { get; set; }  // LF2
        public int? DiscountPrice { get; set; }  // FR1
        public FrequentShopperDiscountType? DiscountMethod { get; set; }  // FDT
        public int? ByCountQuantityFrequentShopper { get; set; }  // FSM
        public int? ByCountItemExceptionPrice { get; set; }  // FSX
        public int? PercentageTareWeight { get; set; }  // PTA
        public string ForceShelfLifeEntry { get; set; }  // FSL ('Y'/'N')
        public string ForceUseByEntry { get; set; }  // FUB ('Y'/'N')
        public int? CoolTextNumber { get; set; }  // CNO
        public int? CoolClassNumber { get; set; }  // CCN
        public int? ShortListNumber { get; set; }  // CSN
        public int? SequenceNumber { get; set; }  // SQN
        public int? MostRecentlyUsedCoolNumber { get; set; }  // CUN
        public string CoolTrackingNumber { get; set; }  // CRN
        public string CoolPromptFlag { get; set; }  // CFX ('Y'/'N')
        public int? CoolPreTextNumber { get; set; }  // CXN
    }
}
