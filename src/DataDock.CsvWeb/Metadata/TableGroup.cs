using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DataDock.CsvWeb.Metadata
{
    public class TableGroup : InheritedPropertyContainer, ICommonPropertyContainer
    {
        public TableGroup() : base(null) { Tables = new List<Table>(); }

        public Uri Id { get; set; }

        public IList<Table> Tables { get; }

        public JObject CommonProperties { get; } = new JObject();
    }
}
