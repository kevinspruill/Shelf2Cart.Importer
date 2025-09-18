using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.ThirdPartyAPI.Models
{
    public class ItemData
    {
        [JsonProperty("recordID", Required = Required.Always)]
        public string RecordID { get; set; } = default!;

        [JsonProperty("itemId", Required = Required.Always)]
        public string ItemId { get; set; } = default!;

        [JsonProperty("receiptAlias", Required = Required.Always)]
        public string ReceiptAlias { get; set; } = default!;

        [JsonProperty("itemName")]
        public string? ItemName { get; set; }

        [JsonProperty("size")]
        public string? Size { get; set; }

        [JsonProperty("sizeQty")]
        public decimal? SizeQty { get; set; }

        [JsonProperty("sizeUnit")]
        public string? SizeUnit { get; set; }

        [JsonProperty("memo")]
        public string? Memo { get; set; }

        [JsonProperty("suggestedRetail")]
        public decimal? SuggestedRetail { get; set; }

        [JsonProperty("coolText")]
        public string? CoolText { get; set; }

        [JsonProperty("deptName")]
        public string? DeptName { get; set; }

        [JsonProperty("deptNumber")]
        public int? DeptNumber { get; set; }

        [JsonProperty("subDeptName")]
        public string? SubDeptName { get; set; }

        [JsonProperty("subDeptNumber")]
        public int? SubDeptNumber { get; set; }

        [JsonProperty("categoryName")]
        public string? CategoryName { get; set; }

        [JsonProperty("categoryNumber")]
        public int? CategoryNumber { get; set; }

        [JsonProperty("subCategoryName")]
        public string? SubCategoryName { get; set; }

        [JsonProperty("subCategoryNumber")]
        public int? SubCategoryNumber { get; set; }

        [JsonProperty("varietyName")]
        public string? VarietyName { get; set; }

        [JsonProperty("varietyNumber")]
        public int? VarietyNumber { get; set; }

        [JsonProperty("brand")]
        public string? Brand { get; set; }

        [JsonProperty("powerField1")]
        public string? PowerField1 { get; set; }

        [JsonProperty("powerField2")]
        public string? PowerField2 { get; set; }

        [JsonProperty("powerField3")]
        public string? PowerField3 { get; set; }

        [JsonProperty("powerField4")]
        public string? PowerField4 { get; set; }

        [JsonProperty("powerField5")]
        public string? PowerField5 { get; set; }

        [JsonProperty("powerField6")]
        public string? PowerField6 { get; set; }

        [JsonProperty("powerField7")]
        public string? PowerField7 { get; set; }

        [JsonProperty("powerField8")]
        public string? PowerField8 { get; set; }

        [JsonProperty("packageQty")]
        public decimal? PackageQty { get; set; }

        [JsonProperty("regionalDescriptor")]
        public string? RegionalDescriptor { get; set; }

        [JsonProperty("productionMethod")]
        public string? ProductionMethod { get; set; }

        [JsonProperty("localReceiptAlias")]
        public string? LocalReceiptAlias { get; set; }

        [JsonProperty("healthAttr1")]
        public string? HealthAttr1 { get; set; }

        [JsonProperty("healthAttr2")]
        public string? HealthAttr2 { get; set; }

        [JsonProperty("healthAttr3")]
        public string? HealthAttr3 { get; set; }

        [JsonProperty("healthAttr4")]
        public string? HealthAttr4 { get; set; }

        [JsonProperty("healthAttr5")]
        public string? HealthAttr5 { get; set; }

        [JsonProperty("healthAttr6")]
        public string? HealthAttr6 { get; set; }

        [JsonProperty("marketingAttr1")]
        public string? MarketingAttr1 { get; set; }

        [JsonProperty("marketingAttr2")]
        public string? MarketingAttr2 { get; set; }

        [JsonProperty("marketingAttr3")]
        public string? MarketingAttr3 { get; set; }

        [JsonProperty("marketingAttr4")]
        public string? MarketingAttr4 { get; set; }

        [JsonProperty("marketingAttr5")]
        public string? MarketingAttr5 { get; set; }

        [JsonProperty("marketingAttr6")]
        public string? MarketingAttr6 { get; set; }

        [JsonProperty("image")]
        public string? Image { get; set; }

        // Note: These appear as "heath" in the sample/spec; keeping exact spellings.
        [JsonProperty("heathAttrImg1")]
        public string? HeathAttrImg1 { get; set; }

        [JsonProperty("heathAttrImg2")]
        public string? HeathAttrImg2 { get; set; }

        [JsonProperty("heathAttrImg3")]
        public string? HeathAttrImg3 { get; set; }

        [JsonProperty("heathAttrImg4")]
        public string? HeathAttrImg4 { get; set; }

        [JsonProperty("heathAttrImg5")]
        public string? HeathAttrImg5 { get; set; }

        [JsonProperty("heathAttrImg6")]
        public string? HeathAttrImg6 { get; set; }

        [JsonProperty("ordering")]
        public List<OrderingData>? Ordering { get; set; }

        [JsonProperty("stores")]
        public List<StoreData>? Stores { get; set; }
    }
}
