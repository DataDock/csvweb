using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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

        public async Task<JObject> ResolveJsonAsync(Uri jsonUri)
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.8));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/csvm+json", 0.9));
            var response = await _client.GetAsync(jsonUri);
            var content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }
    }
}
