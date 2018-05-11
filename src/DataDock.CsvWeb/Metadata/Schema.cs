using System.Collections.Generic;

namespace DataDock.CsvWeb.Metadata
{
    public class Schema : InheritedPropertyContainer
    {
        public Schema(Table table) : base(table) {  }

        public IList<ColumnDescription> Columns;
    }
}