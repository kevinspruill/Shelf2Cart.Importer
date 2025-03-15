using Importer.Module.Invafresh.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    public class BatchHeaderRecord : BaseRecord
    {
        [FieldTag("BNA", true)]
        public string BatchName { get; set; }  // BNA - Required
        [FieldTag("BNO")]
        public int? BatchNumber { get; set; }  // BNO
        [FieldTag("BDA")]
        public DateTime? BatchCreationDate { get; set; }  // BDA
        [FieldTag("BTI")]
        public string BatchCreationTime { get; set; }  // BTI
        [FieldTag("BID")]
        public int? NumberOfItemsInBatch { get; set; }  // ICO
        [FieldTag("BIC")]
        public DateTime? DateToApplyBatch { get; set; }  // EFD
        [FieldTag("EFT")]
        public string TimeToApplyBatch { get; set; }  // EFT
        [FieldTag("BTY")]
        public BatchType? BatchType { get; set; }  // BTY
        [FieldTag("FLG")]
        public int? QueueFlag { get; set; }  // FLG
    }
}
