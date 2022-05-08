using System;
using NodaTime.Text;

namespace DataDock.CsvWeb.Metadata
{
    public class DateTimeFormatSpecification : IFormatSpecification
    {
        private readonly bool _hasOffset;
        private readonly LocalDateTimePattern _localPattern;
        private readonly OffsetDateTimePattern _offsetPattern;

        public DateTimeFormatSpecification(string format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            _hasOffset = format.Contains("x") || format.Contains("X");
            if (_hasOffset)
            {
                _offsetPattern = OffsetDateTimePattern.CreateWithInvariantCulture(TimeFormatSpecification.GetNodaTimePattern(format));
            }
            else
            {
                _localPattern = LocalDateTimePattern.CreateWithInvariantCulture(TimeFormatSpecification.GetNodaTimePattern(format));
            }
        }

        public bool IsValid(string literal)
        {
            return _hasOffset ? _offsetPattern.Parse(literal).Success : _localPattern.Parse(literal).Success;
        }

        public string Normalize(string literal)
        {
            return _hasOffset
                ? OffsetDateTimePattern.ExtendedIso.Format(_offsetPattern.Parse(literal).GetValueOrThrow())
                : LocalDateTimePattern.ExtendedIso.Format(_localPattern.Parse(literal).GetValueOrThrow());
        }
    }
}