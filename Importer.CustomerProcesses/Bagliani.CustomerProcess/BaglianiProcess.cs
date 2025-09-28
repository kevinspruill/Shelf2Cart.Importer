using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Module.ECRS.ThirdPartyAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bagliani.CustomerProcess
{
    public class BaglianiProcess : ICustomerProcess
    {
        public string Name => "Bagliani";

        public bool ForceUpdate { get; set; } = false;

        public T DataFileCondtioning<T>(T ImportData = null) where T : class
        {
            if (ImportData is Dictionary<string, string> record)
            {
                var tmpRecord = record;
                if (record["PowerField 3"].Contains("Shelf 2 Cart") || record["Department Name"].Contains("CHEESE")
                    || record["Department Name"].Contains("PREPARED FOOD"))
                {
                    if (tmpRecord["Item Name"].Length < 1)
                        tmpRecord["Item Name"] = tmpRecord["Receipt Alias"];

                    return tmpRecord as T;
                }
                return tmpRecord as T;
            }

            if (ImportData is tblProducts product)
            {
                var tmpProduct = product;
                return tmpProduct as T;
            }

            if (ImportData is ItemData itemData)
            {
                var tmpItemData = itemData;
                // if ItemId is more than 6 digits or contains non-numeric characters, return null to skip this record
                if (tmpItemData.ItemId.Length > 8 || !tmpItemData.ItemId.All(char.IsDigit))
                {
                    Logger.Warn($"Skipping ItemId '{tmpItemData.ItemId}' due to invalid length or non-numeric characters.");
                    return null;
                }

                return tmpItemData as T;
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
            if (product.Tare.Length > 0)
            {
                string rawWeightProfile = product.Tare;
                Logger.Trace($"Setting Scaleable and Tare using Weight Profile: {rawWeightProfile}");

                product.Scaleable = true;
                if (!rawWeightProfile.Contains("NT"))
                    product.Tare = int.Parse(rawWeightProfile.Substring(2)).ToString();
                else
                    product.Tare = "0";

                product.NetWt = "0";
            } 
            else if (product.NetWt.Length > 0)
            {
                string rawItemSize = product.NetWt.ToUpper();
                Logger.Trace($"Setting Scaleable and NetWt using Item Size: {rawItemSize}");

                product.Scaleable = false;

                //Use regex to isolate [digits][any amount of space][oz in any case] and then get the number from there.
                //If there's nothing, go for any number and lb and maybe convert it to ounces

                string pattern = @"(\d+)\s*oz";
                string digits = "";
                RegexOptions options = RegexOptions.IgnoreCase;

                Match match = Regex.Match(rawItemSize, pattern, options);
                if (match.Success)
                {
                    digits = match.Groups[1].Value;
                    Logger.Trace($"Raw Item Size Input: '{rawItemSize}' -> Isolated NetWt in ounces: '{digits}'");
                }
                else
                {
                    //TODO If it does not match it then we need to check for lb. and in that case convert the lb to oz
                    string poundPattern = @"(\d+)\s*lb";
                    match = Regex.Match(rawItemSize, poundPattern, options);
                    if (match.Success)
                    {
                        digits = (int.Parse(match.Groups[1].Value) * 16).ToString();
                        Logger.Trace($"Raw Item Size Input: '{rawItemSize}' -> Isolated NetWt in ounces: '{digits}'");
                    }
                }

                product.NetWt = digits;
            }

            //TODO Barcode Logic time, info below
            //Leading digits are always 01
            string barcode = string.Empty;

            if (product.PLU.Length == 4 && double.Parse(product.Price) < 100)
            {
                barcode = "02" + product.PLU;

                // Calculate PLU check digit using modulo 10
                var pluCheckDigit = CalculateModulo10CheckDigit(barcode);
                barcode += pluCheckDigit.ToString();
            }
            else if (product.PLU.Length == 5 && double.Parse(product.Price) < 100)
            {
                barcode = "2" + product.PLU;

                // Calculate PLU check digit using UPC-A algorithm
                var pluCheckDigit = CalculateUpcACheckDigit(barcode);
                barcode += pluCheckDigit.ToString();
            }
            else
            {
                //Always start with 01 and the padded product ID
                barcode = "01" + product.PLU.PadLeft(14,'0');
            }

            product.Description11 = barcode;

            if (product.Ingredients.Length > 0 && !product.Ingredients.ToUpper().Contains("INGREDIENTS:"))
                product.Ingredients = "INGREDIENTS: " + product.Ingredients;

            if (product.Description10.Length > 0 && !(product.Description10.ToUpper().Contains("CONTAINS:")
                || product.Description10.ToUpper().Contains("ALLERGENS:")))
                product.Description10 = "CONTAINS: " + product.Description10;

            return product;
        }

        /// <summary>
        /// Calculates UPC-A check digit using modulo 10 algorithm
        /// </summary>
        /// <param name="barcode">The barcode string without check digit</param>
        /// <returns>Single digit check digit (0-9)</returns>
        public static int CalculateUpcACheckDigit(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                throw new ArgumentException("Barcode cannot be null or empty");

            int sum = 0;

            // Process digits from right to left
            for (int i = barcode.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(barcode[i]))
                    throw new ArgumentException("Barcode must contain only digits");

                int digit = barcode[i] - '0';
                int position = barcode.Length - 1 - i;

                // Multiply every odd position (from right, 0-indexed) by 3
                if (position % 2 == 1)
                    digit *= 3;

                sum += digit;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit;
        }

        /// <summary>
        /// Calculates a simple modulo 10 check digit (commonly used in retail systems)
        /// This might be what your system refers to as "Code 128" check digit
        /// </summary>
        /// <param name="barcode">The barcode string without check digit</param>
        /// <returns>Single digit check digit (0-9)</returns>
        public static int CalculateModulo10CheckDigit(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                throw new ArgumentException("Barcode cannot be null or empty");

            int sum = 0;

            foreach (char c in barcode)
            {
                if (!char.IsDigit(c))
                    throw new ArgumentException("Barcode must contain only digits");

                sum += c - '0';
            }

            return sum % 10;
        }

        /// <summary>
        /// Calculates Code 128 check digit using the official Code 128 algorithm
        /// Note: This is the actual Code 128 standard check digit calculation
        /// </summary>
        /// <param name="data">The data to encode (string)</param>
        /// <param name="startChar">Start character value (104 for Code 128B)</param>
        /// <returns>Check digit value (0-102)</returns>
        public static int CalculateCode128CheckDigit(string data, int startChar = 104)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Data cannot be null or empty");

            int checkSum = startChar;

            for (int i = 0; i < data.Length; i++)
            {
                // Convert character to Code 128 value (for Code 128B subset)
                int charValue = data[i] - 32; // ASCII offset for Code 128B

                if (charValue < 0 || charValue > 94)
                    throw new ArgumentException($"Character '{data[i]}' is not valid for Code 128B");

                // Multiply by position (1-based)
                checkSum += charValue * (i + 1);
            }

            return checkSum % 103;
        }
    }
}
