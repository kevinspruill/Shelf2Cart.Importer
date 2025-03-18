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
        [FieldTag("PNO", true)]
        public int PluNumber { get; set; }  // PNO - Required
        [FieldTag("UPC", true)]
        public string UpcCode { get; set; }  // UPC - Required
        [FieldTag("DN1", true)]
        public string DescriptionLine1 { get; set; }  // DN1 - Required for SPIA
        [FieldTag("DS1")]
        public int? DescriptionSize1 { get; set; }  // DS1
        [FieldTag("DN2")]
        public string DescriptionLine2 { get; set; }  // DN2
        [FieldTag("DS2")]
        public int? DescriptionSize2 { get; set; }  // DS2
        [FieldTag("DN3")]
        public string DescriptionLine3 { get; set; }  // DN3
        [FieldTag("DS3")]
        public int? DescriptionSize3 { get; set; }  // DS3
        [FieldTag("DN4")]
        public string DescriptionLine4 { get; set; }  // DN4
        [FieldTag("DS4")]
        public int? DescriptionSize4 { get; set; }  // DS4
        [FieldTag("UPR", true)]
        public int? UnitPrice { get; set; }  // UPR - Required for SPIA and SPPC
        [FieldTag("FWT")]
        public int? FixedWeightAmount { get; set; }  // FWT
        [FieldTag("UME", true)]
        public UnitOfMeasure? UnitOfMeasure { get; set; }  // UME - Required for SPIA
        [FieldTag("BCO")]
        public int? ByCountQuantity { get; set; }  // BCO
        [FieldTag("WTA")]
        public int? WrappedTareWeight { get; set; }  // WTA
        [FieldTag("UTA")]
        public int? UnwrappedTareWeight { get; set; }  // UTA
        [FieldTag("SLI")]
        public int? ShelfLife { get; set; }  // SLI
        [FieldTag("SLT")]
        public ShelfLifeType? ShelfLifeType { get; set; }  // SLT
        [FieldTag("EBY")]
        public int? UseBy { get; set; }  // EBY
        [FieldTag("CCL")]
        public int? CommodityClass { get; set; }  // CCL
        [FieldTag("LNU")]
        public string LogoNumbers { get; set; }  // LNU
        [FieldTag("GNO")]
        public string GraphicNumbers { get; set; }  // GNO
        [FieldTag("GNU")]
        public string GradeNumber { get; set; }  // GNU
        [FieldTag("INO")]
        public int? IngredientNumber { get; set; }  // INO
        [FieldTag("NTN")]
        public int? NutritionNumber { get; set; }  // NTN
        [FieldTag("ALG")]
        public int? AllergenNumber { get; set; }  // ALG
        [FieldTag("U1N")]
        public int? UserDefinedText1Number { get; set; }  // U1N
        [FieldTag("U2N")]
        public int? UserDefinedText2Number { get; set; }  // U2N
        [FieldTag("FTA")]
        public string ForcedTare { get; set; }  // FTA ('Y'/'N')
        [FieldTag("LF1")]
        public int? LabelFormatNumberOne { get; set; }  // LF1
        [FieldTag("LF2")]
        public int? LabelFormatNumberTwo { get; set; }  // LF2
        [FieldTag("FR1")]
        public int? DiscountPrice { get; set; }  // FR1
        [FieldTag("FDT")]
        public FrequentShopperDiscountType? DiscountMethod { get; set; }  // FDT
        [FieldTag("FSM")]
        public int? ByCountQuantityFrequentShopper { get; set; }  // FSM
        [FieldTag("FSX")]
        public int? ByCountItemExceptionPrice { get; set; }  // FSX
        [FieldTag("PTA")]
        public int? PercentageTareWeight { get; set; }  // PTA
        [FieldTag("FSL")]
        public string ForceShelfLifeEntry { get; set; }  // FSL ('Y'/'N')
        [FieldTag("FUB")]
        public string ForceUseByEntry { get; set; }  // FUB ('Y'/'N')
        [FieldTag("CNO")]
        public int? CoolTextNumber { get; set; }  // CNO
        [FieldTag("CCN")]
        public int? CoolClassNumber { get; set; }  // CCN
        [FieldTag("CSN")]
        public int? ShortListNumber { get; set; }  // CSN
        [FieldTag("SQN")]
        public int? SequenceNumber { get; set; }  // SQN
        [FieldTag("CUN")]
        public int? MostRecentlyUsedCoolNumber { get; set; }  // CUN
        [FieldTag("CRN")]
        public string CoolTrackingNumber { get; set; }  // CRN
        [FieldTag("CFX")]
        public string CoolPromptFlag { get; set; }  // CFX ('Y'/'N')
        [FieldTag("CXN")]
        public int? CoolPreTextNumber { get; set; }  // CXN
    }
}
