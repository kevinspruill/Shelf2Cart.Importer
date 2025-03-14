using Importer.Common.Helpers;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Modifiers
{
    public class FindReplace
    {
        List<ReplaceRule> _rules;

        public FindReplace(List<ReplaceRule> findReplaceRules = null)
        {
            _rules = findReplaceRules ?? DatabaseHelper.LoadReplaceRules();
        }

        public string ApplyReplacements(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            foreach (var rule in _rules)
            {
                if (!string.IsNullOrEmpty(rule.Value_Find))
                {
                    input = input.Replace(rule.Value_Find, rule.Value_Replace == "NULL" ? " " : rule.Value_Replace);
                }
                else
                {
                    // Log a warning for rules with empty Value_Find
                    // Logger.Debug($"Skipping replacement rule with empty 'Value_Find'. Rule ID: {rule.Value_ID}");
                }
            }
            return input;
        }
    }
}
