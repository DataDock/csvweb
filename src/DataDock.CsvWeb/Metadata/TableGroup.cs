using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DataDock.CsvWeb.Metadata
{
    public class TableGroup : InheritedPropertyContainer, ICommonPropertyContainer
    {
        public TableGroup() : base(null)
        {
            Tables = new List<Table>();
            Dialect = new Dialect();
            CommonProperties = new JObject();
        }

        public Uri Id { get; set; }

        public IList<Table> Tables { get; }

        public Dialect Dialect { get; set; }

        public JObject CommonProperties { get; }
    }
}
