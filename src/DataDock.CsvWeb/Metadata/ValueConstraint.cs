using System;

namespace DataDock.CsvWeb.Metadata
{
    public class ValueConstraint : IDatatypeConstraint
    {
        public ValueConstraintType ConstraintType { get; set; }
        public double NumericThreshold { get; set; }
        public DateTime DateTimeThreshold { get; set; }
        public DateTimeOffset DurationThreshold { get; set; }

        public bool Validate(object v)
        {
            if (v is double || v is int || v is long || v is decimal || v is float)
            {
                return Validate((double) v, NumericThreshold);
            }

            if (v is DateTime)
            {
                return Validate((DateTime) v, DateTimeThreshold);
            }

            if (v is DateTimeOffset)
            {
                return Validate((DateTimeOffset) v, DurationThreshold);
            }

            return false;
        }

        private bool Validate<T>(T value, T threshold) where T : IComparable
        {
            switch (ConstraintType)
            {
                case ValueConstraintType.Min:
                    return value.CompareTo(threshold) >= 0;
                case ValueConstraintType.Max:
                    return value.CompareTo(threshold) <= 0;
                case ValueConstraintType.MinExclusive:
                    return value.CompareTo(threshold) > 0;
                case ValueConstraintType.MaxExclusive:
                    return value.CompareTo(threshold) < 0;
                default:
                    return false;
            }
        }
    }
}