using System;

namespace DataDock.CsvWeb.Metadata
{
    public class Table : InheritedPropertyContainer
    {
        public Table() : base(null) { }

        // TODO: Replace with a more type-safe constructor when we add the type definition for table group
        public Table(InheritedPropertyContainer tableGroup) :base(tableGroup) { }

        public Uri Url { get; set; }
        public Schema TableSchema { get; set; }
    }
}