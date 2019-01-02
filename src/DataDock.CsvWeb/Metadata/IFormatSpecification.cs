using System;
using System.Collections.Generic;
using System.Text;

namespace DataDock.CsvWeb.Metadata
{
    public interface IFormatSpecification
    {
        bool IsValid(string literal);
        string Normalize(string literal);
    }
}
