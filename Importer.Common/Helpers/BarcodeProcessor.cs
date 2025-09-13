using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Helpers
{
    public static class BarcodeProcessor
    {
        public static tblProducts ApplyBarcodeFormatting(tblProducts product)
        {
            if (string.IsNullOrEmpty(product.Barcode))
            {
                return product;
            }
            // Remove non-numeric characters
            var numericBarcode = new string(product.Barcode.Where(char.IsDigit).ToArray());
            // If the barcode is longer than 12 digits, truncate it to the last 12 digits
            if (numericBarcode.Length == 13)
            {
                numericBarcode = numericBarcode.Substring(2, 6);
            }
                
            // If the barcode is less than 12 digits, pad it with leading zeros
            else if (numericBarcode.Length == 11)
            {
                numericBarcode = numericBarcode.Substring(0, 6);
            }
            else
            {
                string padded = "000000" + numericBarcode;
                numericBarcode = padded.Length > 5 ? padded.Substring(padded.Length - 5) : padded;
            }

            Logger.Trace($"Length of formatted barcode: {numericBarcode.Length}");
            Logger.Trace($"Formatted barcode: {product.Barcode}");
            
            product.Barcode = numericBarcode;
            
            return product;
        }
    }
}
