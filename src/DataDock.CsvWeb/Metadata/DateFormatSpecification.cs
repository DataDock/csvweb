using System;
using System.Globalization;
using NodaTime.Text;

namespace DataDock.CsvWeb.Metadata
{
    public class DateFormatSpecification : IFormatSpecification
    {
        private readonly LocalDatePattern _pattern;
        
        public DateFormatSpecification(string format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            _pattern = LocalDatePattern.CreateWithInvariantCulture(format);
        }


        public bool IsValid(string literal)
        {
            return _pattern.Parse(literal).Success;
        }

        public string Normalize(string literal)
        {
            var parseResult = _pattern.Parse(literal);
            return parseResult.GetValueOrThrow().ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
