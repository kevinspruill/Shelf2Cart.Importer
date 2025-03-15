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
        public string BatchName { get; set; }  // BNA - Required
        public int? BatchNumber { get; set; }  // BNO
        public DateTime? BatchCreationDate { get; set; }  // BDA
        public string BatchCreationTime { get; set; }  // BTI
        public int? NumberOfItemsInBatch { get; set; }  // ICO
        public DateTime? DateToApplyBatch { get; set; }  // EFD
        public string TimeToApplyBatch { get; set; }  // EFT
        public BatchType? BatchType { get; set; }  // BTY
        public int? QueueFlag { get; set; }  // FLG
    }
}
