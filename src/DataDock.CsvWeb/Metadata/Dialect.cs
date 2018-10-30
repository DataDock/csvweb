using System;
using System.Collections.Generic;
using System.Text;
using CsvHelper.Configuration;

namespace DataDock.CsvWeb.Metadata
{
    public class Dialect
    {
        public string CommentPrefix { get; set; } = "#";
        public string Delimiter { get; set; } = ",";
        public bool DoubleQuote { get; set; } = true;
        public string Encoding { get; set; } = "utf-8";
        public bool Header { get; set; } = true;
        public int? HeaderRowCount { get; set; }
        public string[] LineTerminators = {"\n", "\r\n"};
        public string QuoteChar = "\"";
        public bool SkipBlankRows;
        public int SkipColumns;
        public bool SkipInitialSpace;
        public int SkipRows;
        public CsvTrim? Trim;
    }

    public enum CsvTrim
    {
        False,
        True,
        Start,
        End
    }
}
