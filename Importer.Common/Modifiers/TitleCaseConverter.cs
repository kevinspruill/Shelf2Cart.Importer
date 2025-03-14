using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CaseConverter;

namespace Importer.Common.Modifiers
{
    public static class TitleCaseConverter
    {
        private static readonly HashSet<string> AlwaysUppercase = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BBQ", "ID", "TV", "CEO", "CTO", "CFO", "COO", "CIO", "HR", "URL", "API", "UI", "UX", "UK", "USA", "EU", "UG", "NET", "WT"
        };

        private static readonly HashSet<string> AlwaysLowerCase = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "w/", "and", "or", "of", "the", "in", "on", "at", "to", "for", "with", "by", "from", "as", "into", "onto", "upon", "over", "under"
        };

        private static readonly HashSet<string> Abbreviations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "A.M. ", "P.M. ", "B.C. ", "A.D. ", "B.H. ", "B.L.T. "
        };

        // HashSet for Contractions
        private static readonly HashSet<string> Contractions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "I'm", "I'll", "I've", "I'd", "You're", "You'll", "You've", "You'd", "He's", "He'll", "He'd", "She's", "She'll", "She'd", "It's", "It'll", "It'd", "We're", "We'll", "We've", "We'd", "They're", "They'll", "They've", "They'd"
        };

        public static string ConvertToTitleCase(string input, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Remove extra spaces
            input = Regex.Replace(input, @"\s+", " ").Trim();

            // Use CaseConverter's ToTitleCase method
            var titleCased = input.ToTitleCase();

            // Handle special cases that CaseConverter might not cover
            titleCased = HandleSpecialCases(titleCased, preserveAcronyms);

            return titleCased;
        }

        private static string HandleSpecialCases(string input, bool preserveAcronyms)
        {

            // Handle words that should always be uppercase
            if (preserveAcronyms)
            {
                foreach (var word in Contractions.Concat(AlwaysUppercase).Concat(AlwaysLowerCase).Concat(Abbreviations))
                {
                    var replacement = AlwaysUppercase.Contains(word) || Abbreviations.Contains(word) ? word.ToUpper() : word.ToLower();
                    input = Regex.Replace(input, $@"\b{word}\b", replacement, RegexOptions.IgnoreCase);
                }
            }

            // Capitalize first and last words
            var words = input.Split(' ');
            if (words.Length > 0)
            {
                words[0] = CapitalizeFirstLetter(words[0]);
                if (words.Length > 1)
                {
                    words[words.Length - 1] = CapitalizeFirstLetter(words[words.Length - 1]);
                }
                input = string.Join(" ", words);
            }

            // Handle hyphenated words
            input = Regex.Replace(input, @"\b(\w+)(-)(\w+)\b", m =>
                CapitalizeFirstLetter(m.Groups[1].Value) + m.Groups[2].Value + CapitalizeFirstLetter(m.Groups[3].Value));

            // Handle words with apostrophes
            input = Regex.Replace(input, @"\b(\w+)(\')(s)?\b", m =>
                CapitalizeFirstLetter(m.Groups[1].Value) + m.Groups[2].Value + (m.Groups[3].Success ? "s" : ""));

            // Handle words with periods
            input = Regex.Replace(input, @"\b(\w+)(\.)\b", m =>
                CapitalizeFirstLetter(m.Groups[1].Value) + m.Groups[2].Value);

            // Handle words with slashes
            input = Regex.Replace(input, @"\b(\w+)(/)(\w+)\b", m =>
                CapitalizeFirstLetter(m.Groups[1].Value) + m.Groups[2].Value + CapitalizeFirstLetter(m.Groups[3].Value));

            // Handle words with parentheses
            input = Regex.Replace(input, @"\b(\w+)(\()(\w+)(\))\b", m =>
                CapitalizeFirstLetter(m.Groups[1].Value) + m.Groups[2].Value + CapitalizeFirstLetter(m.Groups[3].Value) + m.Groups[4].Value);

            return input;
        }

        private static string CapitalizeFirstLetter(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;
            return char.ToUpper(word[0]) + word.Substring(1);
        }

        public static void AddAlwaysUppercaseWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                AlwaysUppercase.Add(word.ToUpper());
            }
        }

        public static void RemoveAlwaysUppercaseWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                AlwaysUppercase.Remove(word.ToUpper());
            }
        }

        public static string ConvertToAllCaps(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.ToUpper();
            }

            return input;
        }
    }
}
