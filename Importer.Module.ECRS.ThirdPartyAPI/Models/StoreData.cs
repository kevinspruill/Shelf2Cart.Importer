using Importer.Module.ECRS.ThirdPartyAPI.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.ThirdPartyAPI.Models
{
    public class StoreData
    {
        [JsonProperty("recordID")]
        public string? RecordID { get; set; }

        [JsonProperty("storeName")]
        public string? StoreName { get; set; }

        [JsonProperty("storeNumber")]
        public string? StoreNumber { get; set; }

        [JsonProperty("deleted")]
        public bool? Deleted { get; set; }

        [JsonProperty("localPowerField1")]
        public string? LocalPowerField1 { get; set; }

        [JsonProperty("localPowerField2")]
        public string? LocalPowerField2 { get; set; }

        [JsonProperty("localPowerField3")]
        public string? LocalPowerField3 { get; set; }

        [JsonProperty("localPowerField4")]
        public string? LocalPowerField4 { get; set; }

        [JsonProperty("localPowerField5")]
        public string? LocalPowerField5 { get; set; }

        [JsonProperty("localPowerField6")]
        public string? LocalPowerField6 { get; set; }

        [JsonProperty("localPowerField7")]
        public string? LocalPowerField7 { get; set; }

        [JsonProperty("localPowerField8")]
        public string? LocalPowerField8 { get; set; }

        [JsonProperty("local")]
        public bool? Local { get; set; }

        [JsonProperty("dsd")]
        public bool? Dsd { get; set; }

        [JsonProperty("discontinued")]
        public bool? Discontinued { get; set; }

        [JsonProperty("location")]
        public string? Location { get; set; }

        [JsonProperty("sequenceNumber")]
        public int? SequenceNumber { get; set; }

        // Switched to enum
        [JsonProperty("weight")]
        public string? Weight { get; set; }

        [JsonProperty("fixedTare")]
        public decimal? FixedTare { get; set; }

        [JsonProperty("percentTare")]
        public decimal? PercentTare { get; set; }

        [JsonProperty("unitOfMeasure")]
        public int? UnitOfMeasure { get; set; } // Keep as ID unless you’ve standardized values.

        // Switched to enum
        [JsonProperty("tareType")]
        public TareType? TareType { get; set; }

        [JsonProperty("zone")]
        public string? Zone { get; set; }

        [JsonProperty("storeStreetAddress")]
        public string? StoreStreetAddress { get; set; }

        [JsonProperty("storeCity")]
        public string? StoreCity { get; set; }

        [JsonProperty("storeState")]
        public string? StoreState { get; set; }

        [JsonProperty("storePostalCode")]
        public string? StorePostalCode { get; set; }

        [JsonProperty("ingredients")]
        public string? Ingredients { get; set; }

        [JsonProperty("shelfLife")]
        public string? ShelfLife { get; set; }

        [JsonProperty("descLine1")]
        public string? DescLine1 { get; set; }

        [JsonProperty("descLine2")]
        public string? DescLine2 { get; set; }

        [JsonProperty("descLine3")]
        public string? DescLine3 { get; set; }

        [JsonProperty("descLine4")]
        public string? DescLine4 { get; set; }

        [JsonProperty("descSize1")]
        public int? DescSize1 { get; set; }

        [JsonProperty("descSize2")]
        public int? DescSize2 { get; set; }

        [JsonProperty("descSize3")]
        public int? DescSize3 { get; set; }

        [JsonProperty("descSize4")]
        public int? DescSize4 { get; set; }

        [JsonProperty("fixedWeightAmt")]
        public decimal? FixedWeightAmt { get; set; }

        [JsonProperty("byCountQty")]
        public decimal? ByCountQty { get; set; }

        // Switched to enum
        [JsonProperty("forceShelfLife")]
        public ForceShelfLifeOption? ForceShelfLife { get; set; }

        [JsonProperty("userAssigned1")]
        public string? UserAssigned1 { get; set; }

        [JsonProperty("userAssigned2")]
        public string? UserAssigned2 { get; set; }

        [JsonProperty("userAssigned3")]
        public string? UserAssigned3 { get; set; }

        [JsonProperty("userAssigned4")]
        public string? UserAssigned4 { get; set; }

        [JsonProperty("userAssigned5")]
        public decimal? UserAssigned5 { get; set; }

        [JsonProperty("userAssigned6")]
        public decimal? UserAssigned6 { get; set; }

        [JsonProperty("userAssigned7")]
        public decimal? UserAssigned7 { get; set; }

        [JsonProperty("wrkName")]
        public string? WrkName { get; set; }

        [JsonProperty("pclName1")]
        public string? PclName1 { get; set; }

        [JsonProperty("pclName2")]
        public string? PclName2 { get; set; }

        [JsonProperty("pclName3")]
        public string? PclName3 { get; set; }

        [JsonProperty("pclName4")]
        public string? PclName4 { get; set; }

        [JsonProperty("promoStart")]
        public string? PromoStart { get; set; }

        [JsonProperty("promoEnd")]
        public string? PromoEnd { get; set; }

        [JsonProperty("promoPrice1")]
        public decimal? PromoPrice1 { get; set; }

        [JsonProperty("promoPrice2")]
        public decimal? PromoPrice2 { get; set; }

        [JsonProperty("promoPrice3")]
        public decimal? PromoPrice3 { get; set; }

        [JsonProperty("promoPrice4")]
        public decimal? PromoPrice4 { get; set; }

        [JsonProperty("promoDivider1")]
        public decimal? PromoDivider1 { get; set; }

        [JsonProperty("promoDivider2")]
        public decimal? PromoDivider2 { get; set; }

        [JsonProperty("promoDivider3")]
        public decimal? PromoDivider3 { get; set; }

        [JsonProperty("promoDivider4")]
        public decimal? PromoDivider4 { get; set; }

        [JsonProperty("promoDiscount1")]
        public string? PromoDiscount1 { get; set; }

        [JsonProperty("promoDiscount2")]
        public string? PromoDiscount2 { get; set; }

        [JsonProperty("promoDiscount3")]
        public string? PromoDiscount3 { get; set; }

        [JsonProperty("promoDiscount4")]
        public string? PromoDiscount4 { get; set; }

        [JsonProperty("price1")]
        public decimal? Price1 { get; set; }

        [JsonProperty("price2")]
        public decimal? Price2 { get; set; }

        [JsonProperty("price3")]
        public decimal? Price3 { get; set; }

        [JsonProperty("price4")]
        public decimal? Price4 { get; set; }

        [JsonProperty("divider1")]
        public string? Divider1 { get; set; }

        [JsonProperty("divider2")]
        public string? Divider2 { get; set; }

        [JsonProperty("divider3")]
        public string? Divider3 { get; set; }

        [JsonProperty("divider4")]
        public string? Divider4 { get; set; }

        [JsonProperty("discount1")]
        public string? Discount1 { get; set; }

        [JsonProperty("discount2")]
        public string? Discount2 { get; set; }

        [JsonProperty("discount3")]
        public string? Discount3 { get; set; }

        [JsonProperty("discount4")]
        public string? Discount4 { get; set; }

        [JsonProperty("webCartEnabled")]
        public bool? WebCartEnabled { get; set; }

        [JsonProperty("weightedNetSalesGrade")]
        public decimal? WeightedNetSalesGrade { get; set; }

        [JsonProperty("weightedVelocityGrade")]
        public decimal? WeightedVelocityGrade { get; set; }

        [JsonProperty("weightedProfitGrade")]
        public decimal? WeightedProfitGrade { get; set; }

        [JsonProperty("weightedDeptGrade")]
        public decimal? WeightedDeptGrade { get; set; }

        [JsonProperty("weightedBasketGrade")]
        public decimal? WeightedBasketGrade { get; set; }

        [JsonProperty("percentNetSalesGrade")]
        public decimal? PercentNetSalesGrade { get; set; }

        [JsonProperty("percentVelocityGrade")]
        public decimal? PercentVelocityGrade { get; set; }

        [JsonProperty("percentProfitGrade")]
        public decimal? PercentProfitGrade { get; set; }

        [JsonProperty("percentDeptGrade")]
        public decimal? PercentDeptGrade { get; set; }

        [JsonProperty("percentBasketGrade")]
        public decimal? PercentBasketGrade { get; set; }
    }
}
