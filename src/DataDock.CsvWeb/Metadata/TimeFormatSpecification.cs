using System;
using NodaTime.Text;

namespace DataDock.CsvWeb.Metadata
{
    public class TimeFormatSpecification : IFormatSpecification
    {
        private readonly bool _hasOffset;
        private readonly LocalTimePattern _localPattern;
        private readonly OffsetTimePattern _offsetPattern;

        public TimeFormatSpecification(string format)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            _hasOffset = HasOffset(format);
            if (_hasOffset)
            {
                _offsetPattern = OffsetTimePattern.CreateWithInvariantCulture(GetNodaTimePattern(format));
            }
            else
            {
                _localPattern = LocalTimePattern.CreateWithInvariantCulture(GetNodaTimePattern(format));
            }
        }

        internal static string GetNodaTimePattern(string unicodePattern)
        {
            return unicodePattern.Replace("S", "F")
                .Replace("Z", "o<Z+HHmm>")
                .Replace("xxx", "o<m>")
                .Replace("xx", "o<M>")
                .Replace("x", "o<i>")
                .Replace("XXX", "o<Z+HH:mm>")
                .Replace("XX", "o<Z+HHmm>")
                .Replace("X", "o<I>");
        }

        internal static bool HasOffset(string unicodePattern)
        {
            return unicodePattern.Contains("X") || unicodePattern.Contains("x") || unicodePattern.Contains("Z");
        }

        public bool IsValid(string literal)
        {
            return _hasOffset ? _offsetPattern.Parse(literal).Success : _localPattern.Parse(literal).Success;
        }

        public string Normalize(string literal)
        {
            return _hasOffset
                ? OffsetTimePattern.ExtendedIso.Format(_offsetPattern.Parse(literal).GetValueOrThrow())
                : LocalTimePattern.ExtendedIso.Format(_localPattern.Parse(literal).GetValueOrThrow());
        }
    }
}
