using System;
using System.Collections.Generic;

namespace DataDock.CsvWeb.Metadata
{
    public class TableGroup : InheritedPropertyContainer
    {
        public TableGroup() : base(null) { Tables = new List<Table>(); }

        public Uri Id { get; set; }

        public IList<Table> Tables { get; }

    }
}
