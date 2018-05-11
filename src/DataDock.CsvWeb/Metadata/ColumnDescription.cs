using System.Collections.Generic;

namespace DataDock.CsvWeb.Metadata
{
    public class ColumnDescription : InheritedPropertyContainer
    {
        public ColumnDescription(Schema tableSchema) : base(tableSchema) { }

        public string Name { get; set; }
        public IList<LanguageTaggedString> Titles { get; set; }
        public bool SupressOutput { get; set; }
    }
}