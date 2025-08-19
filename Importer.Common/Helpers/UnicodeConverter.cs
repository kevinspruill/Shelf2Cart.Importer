using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Helpers
{
    public static class UnicodeConverter
    {
        // Map common Unicode punctuation/symbols to ASCII
        private static readonly Dictionary<char, string> Map = new Dictionary<char, string>
        {
            // Quotes
            ['\u2018'] = "'",
            ['\u2019'] = "'",
            ['\u201A'] = "'",
            ['\u201B'] = "'",
            ['\u201C'] = "\"",
            ['\u201D'] = "\"",
            ['\u201E'] = "\"",
            ['\u201F'] = "\"",
            ['\u00AB'] = "\"",
            ['\u00BB'] = "\"",

            // Dashes / minus
            ['\u2010'] = "-",
            ['\u2011'] = "-",
            ['\u2012'] = "-",
            ['\u2013'] = "-",
            ['\u2014'] = "-",
            ['\u2212'] = "-",

            // Spaces
            ['\u00A0'] = " ",
            ['\u2000'] = " ",
            ['\u2001'] = " ",
            ['\u2002'] = " ",
            ['\u2003'] = " ",
            ['\u2004'] = " ",
            ['\u2005'] = " ",
            ['\u2006'] = " ",
            ['\u2007'] = " ",
            ['\u2008'] = " ",
            ['\u2009'] = " ",
            ['\u200A'] = " ",
            ['\u202F'] = " ",
            ['\u205F'] = " ",
            ['\u3000'] = " ",

            // Zero width / joiners / BOM -> drop (handled below too)
            ['\u200B'] = "",
            ['\u200C'] = "",
            ['\u200D'] = "",
            ['\u2060'] = "",
            ['\uFEFF'] = "",

            // Punctuation / symbols
            ['\u2026'] = "...",      // …
            ['\u2022'] = "*",        // •
            ['\u00B7'] = ".",        // ·
            ['\u2044'] = "/",        // ⁄
            ['\u00B0'] = " deg",     // °
            ['\u00D7'] = "x",        // ×
            ['\u00F7'] = "/",        // ÷
            ['\u00B1'] = "+/-",      // ±
            ['\u2264'] = "<=",       // ≤
            ['\u2265'] = ">=",       // ≥
            ['\u2260'] = "!=",       // ≠
            ['\u03BC'] = "u",        // μ (micro)

            // Currency
            ['\u20AC'] = "EUR",      // €
            ['\u00A3'] = "GBP",      // £
            ['\u00A5'] = "JPY",      // ¥
            ['\u00A2'] = "c",        // ¢

            // Fractions
            ['\u00BC'] = "1/4",
            ['\u00BD'] = "1/2",
            ['\u00BE'] = "3/4",
            ['\u2153'] = "1/3",
            ['\u2154'] = "2/3",
            ['\u2155'] = "1/5",
            ['\u2156'] = "2/5",
            ['\u2157'] = "3/5",
            ['\u2158'] = "4/5",
            ['\u2159'] = "1/6",
            ['\u215A'] = "5/6",
            ['\u215B'] = "1/8",
            ['\u215C'] = "3/8",
            ['\u215D'] = "5/8",
            ['\u215E'] = "7/8",

            // Legal marks
            ['\u00A9'] = "(C)",
            ['\u00AE'] = "(R)",
            ['\u2122'] = "(TM)",

            // Ligatures not handled well by KD in some fonts
            ['\u00E6'] = "ae",
            ['\u00C6'] = "AE", // æ Æ
            ['\u0153'] = "oe",
            ['\u0152'] = "OE", // œ Œ
            ['\u00DF'] = "ss",                    // ß
            ['\u0141'] = "L",
            ['\u0142'] = "l", // Ł ł
            ['\u00D8'] = "O",
            ['\u00F8'] = "o", // Ø ø
        };

        /// <summary>
        /// Convert Unicode text to ASCII by normalization, diacritic removal,
        /// symbol mapping, and dropping/rewriting anything non-ASCII.
        /// </summary>
        /// <param name="input">Source text.</param>
        /// <param name="fallback">Replacement for still non-ASCII code points (default: drop).</param>
        /// <param name="keepTabsAndNewlines">Keep \t, \n, \r (default true).</param>
        public static string ToAscii(string input, string fallback = "", bool keepTabsAndNewlines = true)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // 1) Compatibility normalize: turns ligatures/superscripts to base chars
            string norm = input.Normalize(NormalizationForm.FormKD);

            var sb = new StringBuilder(norm.Length);

            for (int i = 0; i < norm.Length; i++)
            {
                char ch = norm[i];

                // Handle surrogate pairs (emoji, some CJK ext). Replace with fallback or drop.
                if (char.IsSurrogate(ch))
                {
                    // If it's a valid pair, skip both halves
                    if (char.IsHighSurrogate(ch) && i + 1 < norm.Length && char.IsLowSurrogate(norm[i + 1]))
                        i++; // advance past low surrogate

                    if (fallback != null) sb.Append(fallback);
                    continue;
                }

                // 2) Remove combining marks (diacritics) AFTER FormKD
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.NonSpacingMark ||
                    cat == UnicodeCategory.SpacingCombiningMark ||
                    cat == UnicodeCategory.EnclosingMark)
                {
                    continue;
                }

                // 3) Map common punctuation/whitespace/symbols
                if (Map.TryGetValue(ch, out string mapped))
                {
                    if (mapped.Length > 0) sb.Append(mapped);
                    continue;
                }

                // 4) ASCII fast-path
                if (ch <= 0x7F)
                {
                    // Drop most control chars; optionally keep \t \n \r
                    if (char.IsControl(ch))
                    {
                        if (keepTabsAndNewlines && (ch == '\t' || ch == '\n' || ch == '\r'))
                            sb.Append(ch);
                        // else drop
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    continue;
                }

                // 5) Non-ASCII left: write fallback (default = drop)
                if (fallback != null) sb.Append(fallback);
            }

            return sb.ToString();
        }
    }
}
