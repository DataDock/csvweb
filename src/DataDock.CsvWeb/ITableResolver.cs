using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataDock.CsvWeb
{
    public interface ITableResolver
    {
        /// <summary>
        /// Resolves a link to a CSV table resource, returning a text reader that can be used to retrieve the content of the resource
        /// </summary>
        /// <param name="tableUri"></param>
        /// <returns></returns>
        Task<Stream> ResolveAsync(Uri tableUri);
    }
}
