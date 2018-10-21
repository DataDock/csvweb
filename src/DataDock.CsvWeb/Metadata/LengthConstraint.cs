using System.Collections;

namespace DataDock.CsvWeb.Metadata
{
    public class LengthConstraint : IDatatypeConstraint
    {
        public int? Length { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }

        public bool Validate(object value)
        {
            // If value is a list, the constraint applies to each item in the list
            if (value is IEnumerable && !(value is string) && !(value is byte[]))
            {
                foreach (var v in (value as IEnumerable))
                {
                    if (!Validate(v)) return false;
                }

                return true;
            }

            var valueLength = GetValueLength(value);
            if (Length.HasValue && valueLength != Length.Value) return false;
            if (MinLength.HasValue && valueLength < MinLength.Value) return false;
            if (MaxLength.HasValue && valueLength > MaxLength.Value) return false;
            return true;
        }

        private int GetValueLength(object o)
        {
            if (o is string s) return s.Length;
            if (o is byte[] b) return b.Length;
            return -1;
        }
    }
}