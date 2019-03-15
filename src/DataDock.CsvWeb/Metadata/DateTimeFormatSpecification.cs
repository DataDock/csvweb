using System;
using System.Globalization;
using VDS.RDF.Parsing;

namespace DataDock.CsvWeb.Metadata
{
    public class DateTimeFormatSpecification : IFormatSpecification
    {
        private readonly string _format;
        public DateTimeFormatSpecification(string format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
        }


        public bool IsValid(string literal)
        {
            return DateTime.TryParseExact(literal, _format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var result);
        }

        public string Normalize(string literal)
        {
            var formattedString = DateTime.ParseExact(literal, _format, CultureInfo.InvariantCulture)
                .ToString(XmlSpecsHelper.XmlSchemaDateTimeFormat);
            if (formattedString.EndsWith(".000000"))
            {
                formattedString = formattedString.Substring(0, formattedString.Length - 7);
            }

            return formattedString;
        }
    }
}