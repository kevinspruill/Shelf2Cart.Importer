using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Importer.Common.Modifiers
{
    public class DescriptionCleanse
    {
        private readonly List<CleanseRule> _cleanseRules;
        private readonly bool _buttonCombine;
        private readonly string _buttonOptionIn;

        public DescriptionCleanse(List<CleanseRule> cleanseRules, bool buttonCombine = false, string buttonOptionIn = "")
        {
            _cleanseRules = cleanseRules;
            _buttonCombine = buttonCombine;
            _buttonOptionIn = buttonOptionIn;
        }

        public tblProducts CleanseProductDescription(tblProducts product)
        {
            var insertFieldDict = new Dictionary<string, string>();
            var descriptions = new[]
            {
            product.Description1, product.Description2, product.Description3, product.Description4,
            product.Description5, product.Description6, product.Description7, product.Description8,
            product.Description9, product.Description10, product.Description11, product.Description12,
            product.Description13, product.Description14, product.Description15, product.Description16
        };

            foreach (var rule in _cleanseRules)
            {
                Cleanse(descriptions, rule, insertFieldDict);
            }

            if (_buttonCombine)
            {
                CombineButtons(product, descriptions, insertFieldDict);
            }

            // Update product descriptions
            product.Description1 = descriptions[0];
            product.Description2 = descriptions[1];
            product.Description3 = descriptions[2];
            product.Description4 = descriptions[3];
            product.Description5 = descriptions[4];
            product.Description6 = descriptions[5];
            product.Description7 = descriptions[6];
            product.Description8 = descriptions[7];
            product.Description9 = descriptions[8];
            product.Description10 = descriptions[9];
            product.Description11 = descriptions[10];
            product.Description12 = descriptions[11];
            product.Description13 = descriptions[12];
            product.Description14 = descriptions[13];
            product.Description15 = descriptions[14];
            product.Description16 = descriptions[15];

            // Update additional fields
            foreach (var kvp in insertFieldDict)
            {
                SetPropertyValue(product, kvp.Key, kvp.Value.Trim());
            }

            return product;
        }

        private void Cleanse(string[] descriptions, CleanseRule rule, Dictionary<string, string> insertFieldDict)
        {
            int fieldStart = -1;
            string regexText = "";

            if (new[] { 2, 3, 4 }.Contains(rule.CleanseType))
            {
                var regex = new Regex(@"(\d*\.?\d+)" + Regex.Escape(rule.SearchText), RegexOptions.IgnoreCase);
                for (int i = 0; i < descriptions.Length; i++)
                {
                    var match = regex.Match(descriptions[i]);
                    if (match.Success)
                    {
                        fieldStart = i;
                        regexText = match.Value;
                        break;
                    }
                }
            }
            else
            {
                fieldStart = Array.FindIndex(descriptions, d => d.IndexOf(rule.SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (fieldStart == -1) return;

            string insertFieldValue = "";
            switch (rule.CleanseType)
            {
                case 1:
                case 3:
                    for (int i = fieldStart; i < descriptions.Length; i++)
                    {
                        if (i == fieldStart)
                        {
                            int startChar = descriptions[i].IndexOf(rule.SearchText, StringComparison.OrdinalIgnoreCase);
                            insertFieldValue = descriptions[i].Substring(startChar);
                            descriptions[i] = descriptions[i].Substring(0, startChar).Trim();
                        }
                        else
                        {
                            insertFieldValue += " " + descriptions[i];
                            descriptions[i] = "";
                        }
                    }
                    break;

                case 2:
                case 4:
                    descriptions[fieldStart] = descriptions[fieldStart].Replace(regexText, "").Trim();
                    insertFieldValue = rule.CleanseType == 2 ? regexText : regexText.Replace(rule.SearchText, "");
                    break;

                default:
                    descriptions[fieldStart] = descriptions[fieldStart].Replace(rule.SearchText, "").Trim();
                    insertFieldValue = rule.SearchText;
                    break;
            }

            if (insertFieldDict.ContainsKey(rule.InsertField))
                insertFieldDict[rule.InsertField] += " " + insertFieldValue.Trim();
            else
                insertFieldDict[rule.InsertField] = insertFieldValue.Trim();

            for (int i = 0; i < descriptions.Length; i++)
            {
                descriptions[i] = Regex.Replace(descriptions[i], @"\s+", " ").Trim();
            }
        }

        private void CombineButtons(tblProducts product, string[] descriptions, Dictionary<string, string> insertFieldDict)
        {
            string button1Val = descriptions[0].Trim();
            string button2Val = descriptions[1].Trim();

            if (_buttonOptionIn.Contains("1-4"))
            {
                button2Val = (button2Val + " " + descriptions[2] + " " + descriptions[3]).Trim();
                button2Val = button2Val.Substring(0, Math.Min(button2Val.Length, 64));
            }

            if (_buttonOptionIn.Contains("PLU"))
            {
                string pluSuffix = $" PLU: {product.PLU}";
                int maxLength = 64 - pluSuffix.Length;
                button2Val = button2Val.Substring(0, Math.Min(button2Val.Length, maxLength)) + pluSuffix;
            }

            insertFieldDict["Button 1"] = button1Val;
            insertFieldDict["Button 2"] = button2Val;
        }

        private void SetPropertyValue(tblProducts product, string propertyName, string value)
        {
            var prop = typeof(tblProducts).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<ImportDBFieldAttribute>()?.Name == propertyName);

            if (prop != null)
            {
                if (prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(product, bool.Parse(value));
                }
                else if (prop.PropertyType == typeof(DateTime))
                {
                    prop.SetValue(product, DateTime.Parse(value));
                }
                else
                {
                    prop.SetValue(product, value);
                }
            }
        }
    }
}
