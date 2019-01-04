using Newtonsoft.Json.Linq;

namespace DataDock.CsvWeb.Parsing
{
    public class ParserWarning
    {
        public string Path { get; }
        public string Message { get; }

        public ParserWarning(JToken token, string message) : this(token.Path, message) { }

        public ParserWarning(string jsonPath, string message)
        {
            Path = jsonPath;
            Message = message;
        }
    }
}