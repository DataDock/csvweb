using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DataDock.CsvWeb
{
    public class DefaultResolver : ITableResolver
    {
        private readonly HttpClient _client;

        public DefaultResolver() : this(new HttpClient()) { }

        public DefaultResolver(HttpClient client)
        {
            _client = client;
        }

        public async Task<Stream> ResolveAsync(Uri tableUri)
        {
            // TODO: Handle HTTP errors
            var response =  await _client.GetAsync(tableUri);
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
