using System.Text.RegularExpressions;

namespace DataDock.CsvWeb.Parsing
{
    /// <summary>
    /// Provides services for parsing BCP-47 language tags, using code provided by https://stackoverflow.com/users/10311/porges at https://stackoverflow.com/questions/7035825/regular-expression-for-a-language-tag-as-defined-by-bcp47
    /// </summary>
    internal class LanguageTag
    {
        private static readonly Regex LanguageTagRegex;

        static LanguageTag()
        {
            var regular = "(art-lojban|cel-gaulish|no-bok|no-nyn|zh-guoyu|zh-hakka|zh-min|zh-min-nan|zh-xiang)";
            var irregular =
                "(en-GB-oed|i-ami|i-bnn|i-default|i-enochian|i-hak|i-klingon|i-lux|i-mingo|i-navajo|i-pwn|i-tao|i-tay|i-tsu|sgn-BE-FR|sgn-BE-NL|sgn-CH-DE)";
            var grandfathered = "(?<grandfathered>" + irregular + "|" + regular + ")";
            var privateUse = "(?<privateUse>x(-[A-Za-z0-9]{1,8})+)";
            var singleton = "[0-9A-WY-Za-wy-z]";
            var extension = "(?<extension>" + singleton + "(-[A-Za-z0-9]{2,8})+)";
            var variant = "(?<variant>[A-Za-z0-9]{5,8}|[0-9][A-Za-z0-9]{3})";
            var region = "(?<region>[A-Za-z]{2}|[0-9]{3})";
            var script = "(?<script>[A-Za-z]{4})";
            var extlang = "(?<extlang>[A-Za-z]{3}(-[A-Za-z]{3}){0,2})";
            var language = "(?<language>([A-Za-z]{2,3}(-" + extlang + ")?)|[A-Za-z]{4}|[A-Za-z]{5,8})";
            var langtag = "(" + language + "(-" + script + ")?" + "(-" + region + ")?" + "(-" + variant + ")*" + "(-" +
                          extension + ")*" + "(-" + privateUse + ")?" + ")";
            var languageTag = @"^(" + grandfathered + "|" + langtag + "|" + privateUse + ")$";
            LanguageTagRegex = new Regex(languageTag);
        }

        /// <summary>
        /// Verifies that the provided string matches the specification of a valid BCP-47 language tag
        /// </summary>
        /// <param name="languageTag">The string to be verified</param>
        /// <returns>True if the string matches the production for a valid BCP-47 language tag, false otherwise</returns>
        public static bool IsValid(string languageTag)
        {
            return LanguageTagRegex.IsMatch(languageTag);
        }
    }
}
