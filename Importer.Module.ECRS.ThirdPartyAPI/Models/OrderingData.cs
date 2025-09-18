using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.ThirdPartyAPI.Models
{
    public class OrderingData
    {
        [JsonProperty("recordID")]
        public string? RecordID { get; set; }

        [JsonProperty("supplierID")]
        public string? SupplierID { get; set; }

        [JsonProperty("supplierName")]
        public string? SupplierName { get; set; }

        [JsonProperty("deleted")]
        public bool? Deleted { get; set; }

        [JsonProperty("supplierCode")]
        public string? SupplierCode { get; set; }

        [JsonProperty("orderQty")]
        public decimal? OrderQty { get; set; }

        [JsonProperty("orderUnit")]
        public string? OrderUnit { get; set; }
    }
}
