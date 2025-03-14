using System;
using System.Collections.Generic;
using System.Reflection;
using Importer.Common.Helpers;
using Importer.Common.Models;

namespace Importer.Common.Modifiers
{
    public static class NumberFormatProcessor
    {
        private static readonly Dictionary<string, NumberFormatModel> _numberFormatRules;

        static NumberFormatProcessor()
        {
            _numberFormatRules = LoadNumberFormatRules();
        }

        private static Dictionary<string, NumberFormatModel> LoadNumberFormatRules()
        {
            var rules = DatabaseHelper.GetAllNumberFormats();
            var rulesDictionary = new Dictionary<string, NumberFormatModel>();

            foreach (var rule in rules)
            {
                rulesDictionary[rule.MMField] = rule;
            }

            return rulesDictionary;
        }

        public static tblProducts ApplyNumberFormatting(tblProducts product)
        {
            foreach (var property in typeof(tblProducts).GetProperties())
            {
                var attribute = property.GetCustomAttribute<ImportDBFieldAttribute>();
                if (attribute != null && _numberFormatRules.TryGetValue(attribute.Name, out var rule))
                {
                    if (rule.FormatNumber)
                    {
                        var value = property.GetValue(product);
                        if (value != null && decimal.TryParse(value.ToString(), out decimal numericValue))
                        {
                            string formattedValue = FormatNumber(numericValue, rule.DecimalCount);
                            property.SetValue(product, formattedValue);
                        }
                    }
                }
            }

            return product;
        }

        private static string FormatNumber(decimal value, int decimalCount)
        {
            if (decimalCount < 0)
            {
                // For negative decimal counts, shift the decimal point left
                decimal shiftedValue = value / (decimal)Math.Pow(10, Math.Abs(decimalCount));
                return shiftedValue.ToString($"F{Math.Abs(decimalCount)}");
            }
            else
            {
                // For zero or positive decimal counts, format to the specified number of decimal places
                return value.ToString($"F{decimalCount}");
            }
        }
    }
}