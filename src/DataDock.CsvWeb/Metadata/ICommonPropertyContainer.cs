using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DataDock.CsvWeb.Metadata
{
    public interface ICommonPropertyContainer
    {
        JObject CommonProperties { get; }
    }
}
