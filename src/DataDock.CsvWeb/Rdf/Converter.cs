using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DataDock.CsvWeb.Metadata;
using VDS.RDF;

namespace DataDock.CsvWeb.Rdf
{
    public class Converter
    {
        private readonly Table _tableMetadata;
        private readonly IRdfHandler _rdfHandler;
        private readonly List<string> _errors;
        private readonly IProgress<int> _progress;
        private readonly int _reportInterval;
        private readonly Action<string> _errorMessageSink;
        private int _headerRowCount;

        public IReadOnlyList<string> Errors => _errors;

        public Converter(Table tableMetadata, IRdfHandler rdfHandler, Action<string> errorMessageSink=null, IProgress<int> conversionProgress=null,  int reportInterval=50 )
        {
            _tableMetadata = tableMetadata;
            _rdfHandler = rdfHandler;
            _errors=  new List<string>();
            _progress = conversionProgress;
            _errorMessageSink = errorMessageSink;
            _reportInterval = reportInterval;
        }

        public void Convert(TextReader csvTextReader, bool hasHeaderRow = true)
        {
            using (var csv = new CsvReader(csvTextReader))
            {
                if (csv.Read() && csv.ReadHeader())
                {
                    if (_tableMetadata.TableSchema == null) _tableMetadata.TableSchema = new Schema(_tableMetadata);
                    if (_tableMetadata.TableSchema.Columns == null) AddTableColumns(_tableMetadata.TableSchema, csv.Context.HeaderRecord);
                    _headerRowCount = hasHeaderRow ? 1 : 0;
                    _rdfHandler.StartRdf();
                    while (csv.Read())
                    {
                        if (csv.Context.Row%_reportInterval == 0) _progress?.Report(csv.Context.Row);

                        // 4.6.8 Establish a new blank node Sdef to be used as the default subject for cells where about URL is undefined.
                        var sDef = _rdfHandler.CreateBlankNode();

                        var colCount = _tableMetadata.TableSchema.Columns.Count;
                        // For each cell in the current row where the suppress output annotation for the column associated with that cell is false:
                        for(var colIx = 0; colIx < colCount; colIx++)
                        {
                            var c = _tableMetadata.TableSchema.Columns[colIx];
                            if (c.SupressOutput) continue;
                            try
                            {
                                // 4.6.8.1 Establish a node S from about URL if set, or from Sdef otherwise as the current subject.
                                var s = c.AboutUrl == null ? (INode) sDef : ResolveTemplate(c.AboutUrl, csv);
                                var p = c.PropertyUrl == null
                                    ? _rdfHandler.CreateUriNode(new Uri(_tableMetadata.Url, "#" + c.Name))
                                    : ResolveTemplate(c.PropertyUrl, csv);
                                var cellValue = csv.GetField(colIx) ?? c.Default;
                                cellValue = CellParser.NormalizeCellValue(cellValue, c, c.Datatype);
                                if (cellValue != null)
                                {
                                    if (ValidateCellValue(cellValue, c.Datatype, c.Lang))
                                    {
                                        var o = c.ValueUrl == null
                                            ? (INode) CreateLiteralNode(cellValue, c.Datatype, c.Lang)
                                            : ResolveTemplate(c.ValueUrl, csv);
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
                    _rdfHandler.EndRdf(true);
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

            if (datatypeIri.Equals(DatatypeAnnotation.String.Iri) && !string.IsNullOrEmpty(language))
            {
                // Generate language tagged literal
                return _rdfHandler.CreateLiteralNode(cellValue, language);
            }

            // Generate a datatyped literal
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

        private IUriNode ResolveTemplate(UriTemplate template, CsvReader csv)
        {
            var uri = template.Resolve((p) => ResolveProperty(p, csv));
            return _rdfHandler.CreateUriNode(uri);
        }

        private string ResolveProperty(string property, CsvReader csv)
        {
            if (property.Equals("_row")) return (csv.Context.Row - _headerRowCount).ToString("D");
            var columnIndex = GetColumnIndex(property);
            return csv.GetField(columnIndex);
        }

        private int GetColumnIndex(string columnName)
        {
            for (var i = 0; i < _tableMetadata.TableSchema.Columns.Count; i++)
            {
                if (_tableMetadata.TableSchema.Columns[i].Name != null && _tableMetadata.TableSchema.Columns[i].Name.Equals(columnName)) return i;
            }
            throw new ConversionError($"Could not find a column named {columnName} in the CSV metadata.");
        }

        public class ConversionError : Exception
        {
            public ConversionError(string msg) : base(msg) { }
        }
    }
}
