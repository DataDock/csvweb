using System;
using System.Globalization;
using VDS.RDF.Parsing;

namespace DataDock.CsvWeb.Metadata
{
    public class DateFormatSpecification : IFormatSpecification
    {
        private readonly string _format;
        public DateFormatSpecification(string format)
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
            return DateTime.ParseExact(literal, _format, CultureInfo.InvariantCulture).ToString(XmlSpecsHelper.XmlSchemaDateFormat);
        }
    }
}
