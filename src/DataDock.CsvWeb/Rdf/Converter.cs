#region copyright
// DataDock.CsvWeb is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2018 The DataDock Project (http://datadock.io/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace DataDock.CsvWeb.Rdf
{
    public class Converter
    {
        private readonly ITableResolver _resolver;
        private readonly IRdfHandler _rdfHandler;
        private readonly List<string> _errors;
        private readonly IProgress<int> _progress;
        private readonly int _reportInterval;
        private readonly Action<string> _errorMessageSink;
        private int _headerRowCount;
        private readonly bool _suppressStringDatatype;

        public IReadOnlyList<string> Errors => _errors;
        public ConverterMode Mode { get; set; }

        private INode _tableGroupNode, _tableNode, _rowNode;

        private const string CSVW_NS = "http://www.w3.org/ns/csvw#";
        private readonly JObject _csvwContext;

        public Converter(
            IRdfHandler rdfHandler,
            ITableResolver resolver = null,
            ConverterMode mode = ConverterMode.Standard,
            Action<string> errorMessageSink=null, 
            IProgress<int> conversionProgress=null,  
            int reportInterval=50,
            bool suppressStringDatatype = false)
        {
            _resolver = resolver ?? new DefaultResolver();
            _rdfHandler = rdfHandler;
            Mode = mode;
            _errors=  new List<string>();
            _progress = conversionProgress;
            _errorMessageSink = errorMessageSink;
            _reportInterval = reportInterval;
            _suppressStringDatatype = suppressStringDatatype;
            using (var reader =
                new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("DataDock.CsvWeb.Resources.csvw.jsonld"),
                    Encoding.UTF8))
            {
                _csvwContext = JsonConvert.DeserializeObject<JObject>(reader.ReadToEnd())["@context"] as JObject;
            }
        }

        public async Task ConvertAsync(Uri sourceUri, HttpClient httpClient)
        {
            var response = await httpClient.GetAsync(sourceUri);
            response.EnsureSuccessStatusCode();
            TableGroup tableGroup = null;
            if (IsCsvMimeType(response.Content.Headers.ContentType.MediaType))
            {
                var metadataResponse = await LocateMetadata(sourceUri, httpClient);
                if (metadataResponse != null && metadataResponse.IsSuccessStatusCode)
                {
                    tableGroup = ParseCsvMetadata(metadataResponse.RequestMessage.RequestUri,
                        await metadataResponse.Content.ReadAsStringAsync());
                }
                else
                {
                    tableGroup = new TableGroup();
                    var table = new Table(tableGroup) {Url = sourceUri};
                }
            }

            if (tableGroup != null)
            {
                await ConvertAsync(tableGroup);
            }
        }

        public async Task ConvertWithLocalMetadata(Uri sourceUri, HttpClient httpClient, string localMetadata)
        {
            var response = await httpClient.GetAsync(sourceUri);
            response.EnsureSuccessStatusCode();
            TableGroup tableGroup = null;
            if (IsCsvMimeType(response.Content.Headers.ContentType.MediaType))
            {
                tableGroup = ParseCsvMetadata(sourceUri, localMetadata);
            }

            if (tableGroup != null)
            {
                await ConvertAsync(tableGroup);
            }
        }

        private async Task<HttpResponseMessage> LocateMetadata(Uri sourceUri, HttpClient httpClient)
        {
            var csvmLocation = new Uri(sourceUri, "/.well-known/csvm");
            var csvmResponse = await httpClient.GetAsync(csvmLocation);
            var metadataLocations = new List<Uri>();
            if (csvmResponse.IsSuccessStatusCode)
            {
                var content = await csvmResponse.Content.ReadAsStringAsync();
                var lines = content.Split('\n');
                var templateParams = new Dictionary<string, string> {{"url", sourceUri.ToString()}};
                foreach (var line in lines.Select(l => l.Trim()))
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        var template = new UriTemplate(line);
                        var metadataLocation = template.Resolve(templateParams);
                        if (!metadataLocation.IsAbsoluteUri) metadataLocation = new Uri(sourceUri, metadataLocation);
                        metadataLocations.Add(metadataLocation);
                    }
                }
            }
            else
            {
                using (var tabularDataResponse = await httpClient.GetAsync(sourceUri))
                {
                    var linkHeaders = tabularDataResponse.Headers.GetValues("Link").ToList();
                    if (linkHeaders.Any())
                    {
                        foreach (var linkHeader in linkHeaders)
                        {
                            var parts = linkHeader.Split(';').Select(p => p.Replace(" ", "")).ToList();
                            // NOTE: Spec says that the link header must have both rel=describedby AND an appropriate type, but test case 014 has only the rel parameter, hence we use an OR here
                            if (parts.Any(p =>
                                    p.Equals("rel=\"describedby\"", StringComparison.InvariantCultureIgnoreCase)) ||
                                parts.Any(p =>
                                    p.Equals("type=\"application/csvm+json",
                                        StringComparison.InvariantCultureIgnoreCase) ||
                                    p.Equals("type=\"application/ld+json",
                                        StringComparison.InvariantCultureIgnoreCase) ||
                                    p.Equals("type =\"application/json", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                var link = parts.FirstOrDefault(p => p.StartsWith("<") && p.EndsWith(">"));
                                if (link != null)
                                {
                                    link = link.TrimStart('<').TrimEnd('>');
                                    metadataLocations.Add(new Uri(sourceUri, link));
                                }
                            }

                        }
                    }
                }

                metadataLocations.Add(new Uri(sourceUri + "-metadata.json"));
                metadataLocations.Add(new Uri(sourceUri, "csv-metadata.json"));
            }

            foreach (var loc in metadataLocations)
            {
                var metadataResponse = await httpClient.GetAsync(loc);
                if (metadataResponse.IsSuccessStatusCode)
                {
                    return metadataResponse;
                }
            }

            return null;
        }

        private static bool IsCsvMimeType(string mimeType)
        {
            return mimeType.Contains("text/csv");
        }

        private TableGroup ParseCsvMetadata(Uri baseUri, string metadata)
        {
            var parser = new JsonMetadataParser(_resolver, baseUri);
            return parser.Parse(new StringReader(metadata));
        }

        public async Task ConvertAsync(TableGroup tableGroup) 
        {
            _rdfHandler.StartRdf();
            _rdfHandler.HandleNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            _rdfHandler.HandleNamespace("csvw", new Uri(CSVW_NS));
            _rdfHandler.HandleNamespace("xsd", new Uri("http://www.w3.org/2001/XMLSchema#"));

            if (Mode == ConverterMode.Standard)
            {
                // 1. In standard mode only, establish a new node G. If the group of tables has an identifier then node G must be identified accordingly; else if identifier is null, then node G must be a new blank node.
                _tableGroupNode =
                    tableGroup.Id != null ? (INode)_rdfHandler.CreateUriNode(tableGroup.Id) : _rdfHandler.CreateBlankNode();
                // 2. n standard mode only, specify the type of node G as csvw:TableGroup
                _rdfHandler.HandleTriple(new Triple(_tableGroupNode,
                    _rdfHandler.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType)),
                    _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "TableGroup"))));
            }

            // TODO: 3. In standard mode only, emit the triples generated by running the algorithm specified in section 6. JSON-LD to RDF over any notes and non-core annotations specified for the group of tables, with node G as an initial subject, the notes or non-core annotation as property, and the value of the notes or non-core annotation as value
            EmitCommonProperties(_tableGroupNode, tableGroup);

            // 4. For each table where the suppress output annotation is false:
            foreach (var table in tableGroup.Tables.Where(t=>!t.SuppressOutput))
            {
                using (var textReader = new StreamReader(await _resolver.ResolveAsync(table.Url)))
                {
                    Convert(table, textReader);
                }
            }
            _rdfHandler.EndRdf(_errors.Count == 0);
        }
        
        private void Convert(Table tableMetadata, TextReader csvTextReader, bool hasHeaderRow = true)
        {
            if (Mode == ConverterMode.Standard)
            {
                // 4.1 In standard mode only, establish a new node T which represents the current table.
                _tableNode = tableMetadata.Id == null
                    ? (INode) _rdfHandler.CreateBlankNode()
                    : _rdfHandler.CreateUriNode(tableMetadata.Id);

                // 4.2 In standard mode only, relate the table to the group of tables; 
                _rdfHandler.HandleTriple(new Triple(_tableGroupNode,
                    _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "table")), _tableNode));
                // 4.3 In standard mode only, specify the type of node T as csvw:Table
                _rdfHandler.HandleTriple(new Triple(
                    _tableNode,
                    _rdfHandler.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType)),
                    _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "Table"))));
                // 4.4 In standard mode only, specify the source tabular data file URL for the current table based on the url annotation
                _rdfHandler.HandleTriple(new Triple(
                    _tableNode,
                    _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "url")),
                    _rdfHandler.CreateUriNode(tableMetadata.Url)));
                // TODO: 4.5 In standard mode only, emit the triples generated by running the algorithm specified in section 6. JSON-LD to RDF over any notes and non-core annotations specified for the table, with node T as an initial subject, the notes or non-core annotation as property, and the value of the notes or non-core annotation as value.
                EmitCommonProperties(_tableNode, tableMetadata);
            }

            using (var csv = new CsvReader(csvTextReader))
            {
                if (csv.Read() && csv.ReadHeader())
                {
                    if (tableMetadata.TableSchema == null) tableMetadata.TableSchema = new Schema(tableMetadata);
                    if (tableMetadata.TableSchema.Columns == null) AddTableColumns(tableMetadata.TableSchema, csv.Context.HeaderRecord);
                    _headerRowCount = hasHeaderRow ? 1 : 0;

                    while (csv.Read())
                    {
                        // Report progress
                        if (csv.Context.Row % _reportInterval == 0)
                        {
                            _progress?.Report(csv.Context.Row);
                        }

                        if (Mode == ConverterMode.Standard)
                        {
                            // 4.6.1 In standard mode only, establish a new blank node R which represents the current row.
                            _rowNode = _rdfHandler.CreateBlankNode();
                            // 4.6.2 In standard mode only, relate the row to the table
                            _rdfHandler.HandleTriple(new Triple(
                                _tableNode,
                                _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "row")),
                                _rowNode));
                            // 4.6.3 In standard mode only, specify the type of node R as csvw:Row
                            _rdfHandler.HandleTriple(new Triple(
                                _rowNode,
                                _rdfHandler.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType)),
                                _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "Row"))));
                            // 4.6.4 In standard mode only, specify the row number n for the row
                            _rdfHandler.HandleTriple(new Triple(
                                _rowNode,
                                _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "rownum")),
                                _rdfHandler.CreateLiteralNode(
                                    (csv.Context.Row - _headerRowCount).ToString("D", CultureInfo.InvariantCulture),
                                    UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeInteger))
                                ));
                            // 4.6.5 In standard mode only, specify the row source number nsource for the row within the source tabular data file URL using a fragment-identifier as specified in [RFC7111]
                            var rowUri = new Uri(tableMetadata.Url.ToString() + "#row=" + csv.Context.Row);
                            _rdfHandler.HandleTriple(new Triple(
                                _rowNode,
                                _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "url")),
                                _rdfHandler.CreateUriNode(rowUri)));
                            // TODO: Following rely on row annotations which are not currently supported
                            // 4.6.6 In standard mode only, if row titles is not null, insert any titles specified for the row.
                            // 4.6.7 In standard mode only, emit the triples generated by running the algorithm specified in section 6. JSON-LD to RDF over any non-core annotations specified for the row, with node R as an initial subject, the non-core annotation as property, and the value of the non-core annotation as value
                        }

                        // 4.6.8 Establish a new blank node Sdef to be used as the default subject for cells where about URL is undefined.
                        var sDef = _rdfHandler.CreateBlankNode();

                        var colCount = tableMetadata.TableSchema.Columns.Count;
                        // For each cell in the current row where the suppress output annotation for the column associated with that cell is false:
                        for(var colIx = 0; colIx < colCount; colIx++)
                        {
                            var c = tableMetadata.TableSchema.Columns[colIx];
                            if (c.SupressOutput) continue;
                            try
                            {
                                // 4.6.8.1 Establish a node S from about URL if set, or from Sdef otherwise as the current subject.
                                var s = c.AboutUrl == null ? (INode) sDef : ResolveTemplate(tableMetadata, c.AboutUrl, csv);
                                // 4.6.8.2 In standard mode only, relate the current subject to the current row
                                if (Mode == ConverterMode.Standard)
                                {
                                    _rdfHandler.HandleTriple(new Triple(_rowNode,
                                        _rdfHandler.CreateUriNode(UriFactory.Create(CSVW_NS + "describes")), s));
                                }

                                // 4.6.8.3 If the value of property URL for the cell is not null, then predicate P takes the value of property URL.
                                // Else, predicate P is constructed by appending the value of the name annotation for the column associated with the cell to the the tabular data file URL as a fragment identifier.
                                var p = c.PropertyUrl == null
                                    ? _rdfHandler.CreateUriNode(new Uri(tableMetadata.Url, "#" + c.Name))
                                    : ResolveTemplate(tableMetadata, c.PropertyUrl, csv);
                                var cellValue = csv.GetField(colIx) ?? c.Default;
                                cellValue = CellParser.NormalizeCellValue(cellValue, c, c.Datatype);
                                if (cellValue != null)
                                {
                                    if (ValidateCellValue(cellValue, c.Datatype, c.Lang))
                                    {
                                        var o = c.ValueUrl == null
                                            ? (INode) CreateLiteralNode(cellValue, c.Datatype, c.Lang)
                                            : ResolveTemplate(tableMetadata, c.ValueUrl, csv);
                                        _rdfHandler.HandleTriple(new Triple(s, p, o));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                var errorMessage = $"Conversion error at row {csv.Context.Row}, column '{c.Name}'. {ex.Message}";
                                _errors.Add(errorMessage);
                                _errorMessageSink?.Invoke(errorMessage);
                            }
                        }
                    }
                }
            }
        }

        private void AddTableColumns(Schema tableSchema, string[] columns)
        {
            if (tableSchema.Columns != null) return;
            tableSchema.Columns = new List<ColumnDescription>();
            foreach (var columnName in columns)
            {
                var columnDescription = tableSchema.Columns.FirstOrDefault(x => x.Name.Equals(columnName));
                if (columnDescription == null)
                {
                    columnDescription = new ColumnDescription(tableSchema) {Name = columnName};
                    tableSchema.Columns.Add(columnDescription);
                }
            }
        }

        private bool ValidateCellValue(string cellValue, DatatypeDescription datatypeDescription, string language)
        {
            // TODO: Implement me
            return true;
        }

        private ILiteralNode CreateLiteralNode(string cellValue, DatatypeDescription datatypeDescription, string language)
        {
            var datatypeIri = GetAnnotatedDatatypeIri(datatypeDescription);
            

            // C# library ignores the fragment part of the Iri so we need to also explicitly compare that.
            if (datatypeIri.Equals(DatatypeAnnotation.String.Iri) && 
                datatypeIri.Fragment.Equals(DatatypeAnnotation.String.Iri.Fragment)) 
            {
                if (!string.IsNullOrEmpty(language))
                {
                    // Generate language tagged literal
                    return _rdfHandler.CreateLiteralNode(cellValue, language);
                }

                if (_suppressStringDatatype)
                {
                    // In RDF 1.1 string is the default literal datatype, so we don't need to specify it
                    // DNR doesn't handle this internally at the moment and this can cause problems with verifying test results
                    return _rdfHandler.CreateLiteralNode(cellValue);
                }
            }

            // Generate a datatyped literal
            cellValue = NormalizeLiteral(cellValue, datatypeDescription, datatypeIri.ToString());
            return _rdfHandler.CreateLiteralNode(cellValue, datatypeIri);
        }

        private static Uri GetAnnotatedDatatypeIri(DatatypeDescription datatypeDescription)
        {
            if (datatypeDescription == null) return DatatypeAnnotation.String.Iri;
            if (datatypeDescription.Id != null)
            {
                return datatypeDescription.Id;
            }
            var annotation = DatatypeAnnotation.GetAnnotationById(datatypeDescription.Base);
            if (annotation == null)
            {
                throw new ConversionError(
                    $"Could not determine the correct IRI for the datatype annotation {datatypeDescription.Base}");
            }
            return annotation.Iri;
        }

        private static string NormalizeLiteral(string lit, DatatypeDescription datatype, string datatypeIri)
        {
            switch (datatypeIri)
            {
                case XmlSpecsHelper.XmlSchemaDataTypeDate:
                    if (!string.IsNullOrEmpty(datatype.Format))
                    {
                        return DateTime.ParseExact(lit, datatype.Format, CultureInfo.InvariantCulture).ToString(XmlSpecsHelper.XmlSchemaDateFormat);
                    }
                    return DateTime.Parse(lit).ToString(XmlSpecsHelper.XmlSchemaDateFormat);
                case XmlSpecsHelper.XmlSchemaDataTypeDateTime:
                    if (!string.IsNullOrEmpty(datatype.Format))
                    {
                        return DateTime.ParseExact(lit, datatype.Format, CultureInfo.InvariantCulture)
                            .ToString(XmlSpecsHelper.XmlSchemaDateTimeFormat);
                    }
                    return DateTime.Parse(lit).ToString(XmlSpecsHelper.XmlSchemaDateTimeFormat);
                // TODO: Implement numeric type normalization
            }

            return lit;
        }

        private IUriNode ResolveTemplate(Table tableMetadata, UriTemplate template, CsvReader csv)
        {
            var uri = template.Resolve((p) => ResolveProperty(tableMetadata, p, csv));
            if (!uri.IsAbsoluteUri) uri = new Uri(tableMetadata.Url, uri);
            return _rdfHandler.CreateUriNode(uri);
        }

        private string ResolveProperty(Table tableMetadata, string property, CsvReader csv)
        {
            if (property.Equals("_row")) return (csv.Context.Row - _headerRowCount).ToString("D");
            var columnIndex = GetColumnIndex(tableMetadata, property);
            return csv.GetField(columnIndex);
        }

        private int GetColumnIndex(Table tableMetadata, string columnName)
        {
            for (var i = 0; i < tableMetadata.TableSchema.Columns.Count; i++)
            {
                if (tableMetadata.TableSchema.Columns[i].Name != null && tableMetadata.TableSchema.Columns[i].Name.Equals(columnName)) return i;
            }
            throw new ConversionError($"Could not find a column named {columnName} in the CSV metadata.");
        }

        private void EmitCommonProperties(INode subject, ICommonPropertyContainer container)
        {
            foreach (var p in container.CommonProperties.Properties())
            {
                if (!Uri.IsWellFormedUriString(p.Name, UriKind.Absolute))
                {
                    throw new ConversionError(
                        "Expected common property name to have been normalized to an absolute URI. Found: " + p.Name);
                }
                
                EmitCommonProperty(subject, ExpandUrl(p.Name), p.Value);
            }
        }

        private void EmitCommonProperty(INode subject, Uri predicate, JToken value)
        {
            if (value is JArray array)
            {
                foreach (var item in array)
                {
                    EmitCommonProperty(subject, predicate, item);
                }
            }
            else if (value is JObject o)
            {
                if (o.ContainsKey("@value"))
                {
                    var litVal = o["@value"].Value<string>();
                    if (o.ContainsKey("@type"))
                    {
                        _rdfHandler.HandleTriple(new Triple(
                            subject,
                            _rdfHandler.CreateUriNode(predicate),
                            _rdfHandler.CreateLiteralNode(litVal, ExpandUrl(o["@type"].Value<string>()))));
                    }
                    else if (o.ContainsKey("@language"))
                    {
                        var lang = o["@language"].Value<string>();
                        _rdfHandler.HandleTriple(new Triple(
                            subject,
                            _rdfHandler.CreateUriNode(predicate),
                            _rdfHandler.CreateLiteralNode(litVal, lang)));
                    }
                    else
                    {
                        _rdfHandler.HandleTriple(new Triple(
                            subject,
                            _rdfHandler.CreateUriNode(predicate),
                            _rdfHandler.CreateLiteralNode(litVal, new Uri(XmlSpecsHelper.XmlSchemaDataTypeString))));
                    }
                }
                else
                {
                    var s = o.ContainsKey("@id")
                        ? (INode) _rdfHandler.CreateUriNode(new Uri(o["@id"].Value<string>()))
                        : _rdfHandler.CreateBlankNode();
                    _rdfHandler.HandleTriple(new Triple(subject, _rdfHandler.CreateUriNode(predicate), s));
                    if (o.ContainsKey("@type"))
                    {
                        if (o["@type"] is JArray typeArray)
                        {
                            foreach (var t in typeArray)
                            {
                                EmitTypeTriple(s, t);
                            }
                        }
                        else
                        {
                            EmitTypeTriple(s, o["@type"]);
                        }
                    }

                    foreach (var p in o.Properties())
                    {
                        if (!p.Name.StartsWith("@"))
                        {
                            EmitCommonProperty(s, ExpandUrl(p.Name), p.Value);
                        }
                    }
                }
            }
            else
            {
                switch (value.Type)
                {
                    case JTokenType.Boolean:
                        _rdfHandler.HandleTriple(new Triple(subject,
                            _rdfHandler.CreateUriNode(predicate),
                            _rdfHandler.CreateLiteralNode(
                                value.Value<bool>() ? "true" : "false",
                                new Uri(XmlSpecsHelper.XmlSchemaDataTypeBoolean)
                            )));
                        break;
                    case JTokenType.Integer:
                        _rdfHandler.HandleTriple(new Triple(
                            subject,
                            _rdfHandler.CreateUriNode(predicate),
                            _rdfHandler.CreateLiteralNode(
                                value.Value<int>().ToString(CultureInfo.InvariantCulture),
                                new Uri(XmlSpecsHelper.XmlSchemaDataTypeInteger))));
                        break;
                    case JTokenType.Float:
                        _rdfHandler.HandleTriple(new Triple(
                            subject,
                            _rdfHandler.CreateUriNode(predicate),
                            _rdfHandler.CreateLiteralNode(
                                value.Value<double>().ToString("E"),
                                new Uri(XmlSpecsHelper.XmlSchemaDataTypeDouble))));
                        break;
                    default:
                        _rdfHandler.HandleTriple(new Triple(
                            subject,
                            _rdfHandler.CreateUriNode(predicate),
                            _rdfHandler.CreateLiteralNode(
                                value.Value<string>(),
                                new Uri(XmlSpecsHelper.XmlSchemaDataTypeString))));
                        break;
                }
            }
        }

        private void EmitTypeTriple(INode s, JToken t)
        {
            var typeUri = ExpandUrl(t.Value<string>());
            _rdfHandler.HandleTriple(new Triple(s,
                _rdfHandler.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)),
                _rdfHandler.CreateUriNode(typeUri)));
        }

        private Uri ExpandUrl(string v)
        {
            if (_csvwContext.ContainsKey(v) && _csvwContext[v].Type == JTokenType.String)
            {
                return new Uri(_csvwContext[v].Value<string>());
            }

            if (v.Contains(":"))
            {
                var parts = v.Split(new[] {':'}, 2);
                var prefix = parts[0];
                var suffix = parts[1];
                if (suffix.StartsWith("//"))
                {
                    return new Uri(v);
                }

                if (_csvwContext.ContainsKey(prefix) && _csvwContext[prefix].Type == JTokenType.String)
                {
                    return new Uri(_csvwContext[prefix].Value<string>() + suffix);
                }
            }

            throw new MetadataParseException("Unable to expand URL value:  " + v);
        }

        public class ConversionError : Exception
        {
            public ConversionError(string msg) : base(msg) { }
        }
    }
}
