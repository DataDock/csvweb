using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DataDock.CsvWeb.Parsing;
using VDS.RDF.Parsing;

namespace DataDock.CsvWeb.Metadata
{
    public class NumericFormatSpecification : IFormatSpecification
    {
        private readonly char _decimalChar;
        private readonly string _decimalSeparator;
        private readonly char? _groupChar;
        private readonly string _groupSeparator;
        private readonly string _pattern;
        private Regex _patternRegex;

        public NumericFormatSpecification(char decimalChar, char? groupChar, string pattern)
        {
            _decimalChar = decimalChar;
            _groupChar = groupChar;
            _decimalSeparator = new string(decimalChar, 1);
            _groupSeparator = _groupChar.HasValue ? new string(_groupChar.Value, 1) : string.Empty;
            _pattern = pattern;
            ValidatePattern();
            MakePatternRegex();
        }

        public NumericFormatSpecification(string pattern):this('.', ',', pattern) { }

        private void ValidatePattern()
        {
            if (_pattern == null) return;
            var validChars = new List<char> {'0', '#', _decimalChar, 'E', '+', '%', '‰'};
            if (_groupChar.HasValue) validChars.Add(_groupChar.Value);
            if (_pattern.Any(c => !validChars.Contains(c)))
            {
                throw new MetadataParseException("Invalid pattern for numeric format specification.");
            }
        }

        private void MakePatternRegex()
        {
            if (!string.IsNullOrEmpty(_pattern))
            {
                _patternRegex  = new Regex(_pattern.Replace("#", "\\d").Replace("0", "\\d").Replace(".", "\\."));
            }
            else
            {
                var groupCharPattern = string.IsNullOrEmpty(_groupSeparator) ? "" : "|" + Regex.Escape(_groupSeparator);
                var decimalCharPattern = Regex.Escape(new string(_decimalChar, 1));
                _patternRegex = new Regex("^(((\\+|-)?\\d(\\d" + groupCharPattern + ")*(" + decimalCharPattern + "\\d+)?((E(\\+|-)?\\d+)|%|‰))|NaN|(-)?INF)$");
            }
        }

        public bool IsValid(string literal)
        {
            return _patternRegex.IsMatch(literal);
        }

        public string Normalize(string literal)
        {
            if (_groupChar.HasValue) literal = literal.Replace(new string(_groupChar.Value, 1), string.Empty);
            literal = literal.Replace(_decimalSeparator, CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
            if (literal.Contains("E"))
            {
                var d = double.Parse(literal, CultureInfo.InvariantCulture);
                return d.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                var dec = decimal.Parse(literal, CultureInfo.InvariantCulture);
                return dec.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
