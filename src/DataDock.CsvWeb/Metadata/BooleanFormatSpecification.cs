using System;
using System.Collections.Generic;
using System.Text;
using DataDock.CsvWeb.Parsing;
using DataDock.CsvWeb.Rdf;

namespace DataDock.CsvWeb.Metadata
{
    public class BooleanFormatSpecification : IFormatSpecification
    {
        private readonly string _trueFormat;
        private readonly string _falseFormat;
        internal BooleanFormatSpecification(string format)
        {
            if(format == null) throw new ArgumentNullException(nameof(format));
            var parts = format.Split('|');
            if (parts.Length != 2)
            {
                throw new MetadataParseException("Invalid format specification. The format specifier for a boolean datatype must be two strings separated by a | character.");
            }

            _trueFormat = parts[0];
            _falseFormat = parts[1];
        }


        public bool IsValid(string literal)
        {
            return literal.Equals(_trueFormat) || literal.Equals(_falseFormat);
        }

        public string Normalize(string literal)
        {
            if (literal.Equals(_trueFormat)) return "true";
            if (literal.Equals(_falseFormat)) return "false";
            throw new Converter.ConversionError(
                $"Could not parse cell value '{literal}' as a boolean according to the column format specification.");
        }
    }
}
