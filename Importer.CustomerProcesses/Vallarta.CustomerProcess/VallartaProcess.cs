using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vallarta.CustomerProcess
{
    public class VallartaProcess : ICustomerProcess
    {
        public string Name => "Vallarta";

        public bool ForceUpdate { get; set; } = false;

        public T DataFileCondtioning<T>(T ImportData = null) where T : class
        {
            if (ImportData is Dictionary<string,object> record)
            {
                string rawBarcodeData = record["Barcode"].ToString();
                Logger.Trace($"Setting Barcode and Description11 using raw barcode data: {record["Barcode"]}");
                var tmpRecord = record;
                tmpRecord["Barcode"] = rawBarcodeData.Substring(2, 6);
                tmpRecord["Description11"] = rawBarcodeData.Substring(2);
                return tmpRecord as T;
            }
            return ImportData;
        }

        public void PostProductProcess()
        {
 
        }

        public void PostQueryRoutine()
        {

        }

        public tblProducts PreProductProcess(tblProducts product)
        {
            return product;
        }

        public void PreProductProcess()
        {

        }

        public void PreQueryRoutine()
        {

        }

        public tblProducts ProductProcessor(tblProducts product)
        {
            return product;
        }
    }
}
