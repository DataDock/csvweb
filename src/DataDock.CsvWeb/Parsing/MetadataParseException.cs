using System;

namespace DataDock.CsvWeb.Parsing
{
    public class MetadataParseException : Exception
    {
        public MetadataParseException(string msg) : base(msg)
        {
        }
    }
}