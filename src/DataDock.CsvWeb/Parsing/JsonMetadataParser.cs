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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using DataDock.CsvWeb.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DataDock.CsvWeb.Parsing
{
    public class JsonMetadataParser
    {
        private readonly ITableResolver _resolver;
        private readonly Uri _baseUri;
        private string _defaultLanguage;

        

        public JsonMetadataParser(ITableResolver resolver, Uri baseUri, string defaultLanguage = null)
        {
            _resolver = resolver;
            _baseUri = baseUri;
            _defaultLanguage = defaultLanguage;
        }

        public TableGroup Parse(TextReader textReader)
        {
            var jsonParser = new Newtonsoft.Json.JsonSerializer();
            var rootObject = jsonParser.Deserialize(new JsonTextReader(textReader)) as JObject;
            if (rootObject == null)
            {
                throw new MetadataParseException("Expected root of JSON document to be an object.");
            }
            var normalizer = new MetadataNormalizer(_resolver, _baseUri, _defaultLanguage);
            rootObject = normalizer.NormalizeMetadata(rootObject);
            return Parse(rootObject);
        }

        
        private TableGroup Parse(JObject metadataObject)
        {
            JToken t;
            if (metadataObject.TryGetValue("tables", out t)) return ParseTableGroup(metadataObject);
            if (metadataObject.TryGetValue("url", out t))
            {
                var tableGroup = new TableGroup();
                ParseTable(tableGroup, metadataObject);
                return tableGroup;
            }
            throw new MetadataParseException("Unrecognized root object type");
        }

        public TableGroup ParseTableGroup(JObject root)
        {
            if (!root.TryGetValue("tables", out JToken t))
            {
                throw new MetadataParseException("Did not find required 'tables' property on table group object");
            }

            var tablesArray = t as JArray;
            if (tablesArray == null) throw new MetadataParseException("The value of the 'tables' property must be an array");
            var tableGroup = new TableGroup();

            if (root.TryGetValue("dialect", out t))
            {
                if (!(t is JObject dialectObject))
                {
                    throw new MetadataParseException("The value of the 'dialect' property must be a JSON object");
                }
                tableGroup.Dialect = ParseDialect(dialectObject);
            }
            else
            {
                tableGroup.Dialect = new Dialect();
            }

            foreach (var item in tablesArray)
            {
                var tableDescriptionObject = item as JObject;
                if (tableDescriptionObject == null)
                    throw new MetadataParseException("Items in the 'tables' array must be objects");
                ParseTable(tableGroup, tableDescriptionObject);
            }

            if (root.TryGetValue("@id", out t))
            {
                var id = (t as JValue)?.Value<string>();
                if (id == null) throw new MetadataParseException("The value of the @id property must be a string");
                tableGroup.Id = ResolveUri(id);
            }

            ParseInheritedProperties(root, tableGroup);
            ParseCommonProperties(root, tableGroup);
            tableGroup.Notes = ParseNotes(root);
            return tableGroup;
        }

        public Table ParseTable(TableGroup tableGroup, JObject root)
        {
            JToken t;
            
            if (!root.TryGetValue("url", out t))
            {
                throw new MetadataParseException("Did not find required 'url' property on table object");
            }
            var url = (t as JValue)?.Value<string>();
            if (url == null) throw new MetadataParseException("The value of the 'url' property must be a string");
            var tableUri = ResolveUri(url);
            var table = new Table(tableGroup) {Url = tableUri};
            if (root.TryGetValue("@id", out t))
            {
                var id = (t as JValue)?.Value<string>();
                if (id == null) throw new MetadataParseException("The value of the @id property must be a string");
                table.Id = ResolveUri(id);
            }
            if (root.TryGetValue("tableSchema", out t))
            {
                if (!(t is JObject schemaObject))
                {
                    throw new MetadataParseException("The value of the 'tableSchema' property must be a JSON object");
                }
                table.TableSchema = ParseTableSchema(table, schemaObject);
            }

            if (root.TryGetValue("dialect", out t))
            {
                if (!(t is JObject dialectObject))
                {
                    throw new MetadataParseException("The value of the 'dialect' property must be a JSON object");
                }
                table.Dialect = ParseDialect(dialectObject);
            }
            else
            {
                table.Dialect = tableGroup.Dialect ?? new Dialect();
            }

            table.SuppressOutput = ParseSuppressOutput(root);
            ParseInheritedProperties(root, table);
            ParseCommonProperties(root, table);
            table.Notes = ParseNotes(root);
            return table;
        }

        private Schema ParseTableSchema(Table table, JObject root)
        {
            var schema = new Schema(table) {Columns = new List<ColumnDescription>()};
            ParseInheritedProperties(root, schema);
            JToken t;
            if (root.TryGetValue("columns", out t))
            {
                var cols = t as JArray;
                if (cols == null) throw new MetadataParseException("The value of the 'columns' property must be a JSON array");
                var columnNumber = 1;
                foreach (var item in cols)
                {
                    var col = item as JObject;
                    if (col == null) throw new MetadataParseException("The items in the 'columns' array must be JSON objects");
                    var colDesc = ParseColumnDescription(schema, columnNumber, col);
                    schema.Columns.Add(colDesc);
                    columnNumber++;
                }
            }
            return schema;
        }

        private ColumnDescription ParseColumnDescription(Schema schema, int columnNumber, JObject root)
        {
            var columnDescription = new ColumnDescription(schema);
            JToken t;
            if (root.TryGetValue("name", out t))
            {
                var nameValue = t as JValue;
                if (nameValue == null || nameValue.Type != JTokenType.String) throw new MetadataParseException("The value of the 'name' property must be a string");
                ValidateColumnName(nameValue.Value<string>());
                columnDescription.Name = nameValue.Value<string>();
            }
            if (root.TryGetValue("suppressOutput", out t))
            {
                var suppress = t as JValue;
                if (suppress == null || suppress.Type != JTokenType.Boolean)
                    throw new MetadataParseException("The value of the 'suppressOutput' property must be a boolean");
                columnDescription.SuppressOutput = suppress.Value<bool>();
            }

            if (root.TryGetValue("titles", out t))
            {
                columnDescription.Titles = ParseNaturalLanguageProperty(t);
            }

            if (root.TryGetValue("default", out t))
            {
                var defaultValue = t as JValue;
                if (defaultValue == null || defaultValue.Type != JTokenType.String) throw new MetadataParseException("The value of the 'default' property must be a string");
                columnDescription.Default = defaultValue.Value<string>();
            }

            if (columnDescription.Name == null && columnDescription.Titles != null)
            {
                columnDescription.Name = columnDescription.Titles.Where(x => x.LanguageTag.Equals(_defaultLanguage))
                                             .Select(x => x.Value).FirstOrDefault() ?? 
                                         columnDescription.Titles.Where(x => x.LanguageTag == "und")
                                             .Select(x => x.Value).FirstOrDefault();
            }

            if (columnDescription.Name == null)
            {
                columnDescription.Name = "_col." + columnNumber;
            }
            columnDescription.SuppressOutput = ParseSuppressOutput(root);
            ParseInheritedProperties(root, columnDescription);
            return columnDescription;
        }

        private static bool ParseSuppressOutput(JObject root)
        {
            if (root.TryGetValue("suppressOutput", out JToken t))
            {
                var suppress = t as JValue;
                if (suppress == null || suppress.Type != JTokenType.Boolean)
                    throw new MetadataParseException("The value of the 'suppressOutput' property must be a boolean");
                return suppress.Value<bool>();
            }

            return false;
        }

        private IList<LanguageTaggedString> ParseNaturalLanguageProperty(JToken tok)
        {
            var ret = new List<LanguageTaggedString>();
            if (tok is JValue jv && jv.Type == JTokenType.String)
            {
                ret.Add(new LanguageTaggedString {LanguageTag = _defaultLanguage, Value = jv.Value<string>()});
            }
            else if (tok is JObject o)
            {
                foreach (var p in o.Properties())
                {
                    if (p.Value is JArray valArray)
                    {
                        foreach (var v in valArray)
                        {
                            ret.Add(new LanguageTaggedString
                                {LanguageTag = p.Name, Value = v.Value<string>()});
                        }
                    }
                    else
                    {
                        ret.Add(new LanguageTaggedString {LanguageTag = p.Name, Value = p.Value.Value<string>()});
                    }
                }
            }
            else if (tok is JArray valArray)
            {
                foreach (var item in valArray)
                {
                    ret.AddRange(ParseNaturalLanguageProperty(item));
                }
            }

            return ret;
        }

        private static void ValidateColumnName(string name)
        {
            if (name.StartsWith("_")) throw new MetadataParseException("$Column name {name} is not valid. Column names must not start with an _ character.");
            // TODO: Other rules (rules for variables in URL templates)
        }

        private static Dialect ParseDialect(JToken root)
        {
            var d = root.ToObject<Dialect>();
            if (!d.HeaderRowCount.HasValue)
            {
                d.HeaderRowCount = d.Header ? 1 : 0;
            }

            if (!d.Trim.HasValue)
            {
                d.Trim = d.SkipInitialSpace ? CsvTrim.Start : CsvTrim.False;
            }

            return d;
        }

        private static void ParseInheritedProperties(JObject root, InheritedPropertyContainer container)
        {
            JToken t;
            if (root.TryGetValue("datatype", out t))
            {
                var datatypeVal = t as JValue;
                if (datatypeVal != null && datatypeVal.Type == JTokenType.String)
                {
                    var datatypeId = datatypeVal.Value<string>();
                    ValidateBaseDatatype(datatypeId);
                    container.Datatype = new DatatypeDescription {Base = datatypeId};
                }
                else
                {
                    var datatypeObj = t as JObject;
                    if (datatypeObj != null)
                    {
                        container.Datatype = ParseDatatype(datatypeObj);
                    }
                    else
                    {
                        throw new MetadataParseException("The value of the 'datatype' property must be a string or a JSON object");
                    }
                }
            }

            if (root.TryGetValue("lang", out t))
            {
                if (t is JValue v)
                {
                    container.Lang = v.Value<string>();
                }
                else
                {
                    throw new MetadataParseException("The value of the 'lang' property must be a string");
                }
            }

            if (root.TryGetValue("aboutUrl", out t))
            {
                var aboutUrlVal = t as JValue;
                if (aboutUrlVal != null && aboutUrlVal.Type == JTokenType.String)
                {
                    var aboutUrl = aboutUrlVal.Value<string>();
                    container.AboutUrl = new UriTemplate(aboutUrl);
                }
                else
                {
                    throw new MetadataParseException("The value of the 'aboutUrl' property must be a string");
                }
            }
            if (root.TryGetValue("propertyUrl", out t))
            {
                var propertyUrlVal = t as JValue;
                if (propertyUrlVal != null && propertyUrlVal.Type == JTokenType.String)
                {
                    var propertyUrl = propertyUrlVal.Value<string>();
                    container.PropertyUrl = new UriTemplate(propertyUrl);
                }
                else
                {
                    throw new MetadataParseException("The value of the 'propertyUrl' property must be a string");
                }
            }
            if (root.TryGetValue("valueUrl", out t))
            {
                var valueUrlVal = t as JValue;
                if (valueUrlVal != null && valueUrlVal.Type == JTokenType.String)
                {
                    var valueUrl = valueUrlVal.Value<string>();
                    container.ValueUrl = new UriTemplate(valueUrl);
                }
                else
                {
                    throw new MetadataParseException("The value of the 'valueUrl' property must be a string");
                }
            }
            if (root.TryGetValue("null", out t))
            {
                if (t is JArray array)
                {
                    container.Null = array.Select(item => item.Value<string>()).ToArray();
                }
                else if (t is JValue v)
                {
                    container.Null = new[] { v.Value<string>() };
                }
            }

            if (root.TryGetValue("separator", out t))
            {
                if (t is JValue v)
                {
                    container.Separator = v.Value<string>();
                }
                else
                {
                    throw new MetadataParseException("The value of the 'separator' property must be a string");
                }
            }

        }

        private static DatatypeDescription ParseDatatype(JObject root)
        {
            var datatype = new DatatypeDescription();
            JToken t;
            if (root.TryGetValue("base", out t))
            {
                var baseVal = t as JValue;
                if (baseVal == null || baseVal.Type != JTokenType.String)
                {
                    throw new MetadataParseException("The value of the 'base' property must be a string");
                }
                var datatypeId = baseVal.Value<string>();
                ValidateBaseDatatype(datatypeId);
                datatype.Base = datatypeId;
            }
            else
            {
                datatype.Base = "string";
            }

            if (root.TryGetValue("format", out t))
            {
                switch (datatype.Base)
                {
                    case "boolean":
                        datatype.Format = new BooleanFormatSpecification(t.Value<string>());
                        break;
                    case "date":
                        datatype.Format = new DateFormatSpecification(t.Value<string>());
                        break;
                    case "datetime":
                        datatype.Format = new DateTimeFormatSpecification(t.Value<string>());
                        break;
                    default:
                        throw new NotImplementedException($"Support for format annotations on the datatype '{datatype.Base}' is not yet implemented");
                }
            }

            if (root.TryGetValue("minimum", out t))
            {
                datatype.Constraints.Add(ParseConstraint(datatype, ValueConstraintType.Min, t));
            }

            if (root.TryGetValue("minInclusive", out t))
            {
                datatype.Constraints.Add(ParseConstraint(datatype, ValueConstraintType.Min, t));
            }

            if (root.TryGetValue("minExclusive", out t))
            {
                datatype.Constraints.Add(ParseConstraint(datatype, ValueConstraintType.MinExclusive, t));
            }

            if (root.TryGetValue("maximum", out t))
            {
                datatype.Constraints.Add(ParseConstraint(datatype, ValueConstraintType.Max, t));
            }

            if (root.TryGetValue("maxInclusive", out t))
            {
                datatype.Constraints.Add(ParseConstraint(datatype, ValueConstraintType.Max, t));
            }

            if (root.TryGetValue("maxExclusive", out t))
            {
                datatype.Constraints.Add(ParseConstraint(datatype, ValueConstraintType.MaxExclusive, t));
            }

            return datatype;
        }

        private static void ValidateBaseDatatype(string baseTypeId)
        {
            var datatypeAnnotation = DatatypeAnnotation.GetAnnotationById(baseTypeId);
            if (datatypeAnnotation == null)
                throw new MetadataParseException(
                    $"Unable to match the datatype '{baseTypeId}' to a known datatype");
        }

        private static ValueConstraint ParseConstraint(DatatypeDescription datatype, ValueConstraintType constraintType, JToken t)
        {
            if (t.Type == JTokenType.Integer)
                return new ValueConstraint
                    {ConstraintType = constraintType, NumericThreshold = (double) t.Value<int>()};
            if (t.Type == JTokenType.Float)
                return new ValueConstraint
                    { ConstraintType = constraintType, NumericThreshold = t.Value<double>() };
            // TODO: Better parsing of min and max values using datatype and format
            throw new NotImplementedException("Only numeric minimum and maximum constraints are currently supported");
        }

        private Uri ResolveUri(string link)
        {
            Uri ret;
            if (_baseUri == null)
            {
                // No base URI for the parse, so the value must be an absolute URI
                if (!Uri.TryCreate(link, UriKind.Absolute, out ret))
                {
                    throw new MetadataParseException(
                        $"The value '{link}' could not be parsed as an absolute IRI and no base IRI is available for resolving relative links.");
                }
                return ret;
            }
            if (!Uri.TryCreate(_baseUri, link, out ret))
            {
                throw new MetadataParseException(
                    $"The value '{link} could not be parsed as either an absolute or relative IRI.");
            }
            return ret;
        }

        private void ParseCommonProperties(JObject root, ICommonPropertyContainer container)
        {
            foreach (var p in root.Properties())
            {
                if (MetadataSpecHelper.IsCommonProperty(p.Name))
                {
                    container.CommonProperties.Add(p.DeepClone());
                }
            }
        }

        private JArray ParseNotes(JObject root)
        {
            if (root.TryGetValue("notes", out JToken t))
            {
                if (!(t is JArray notesArray))
                {
                    throw new MetadataParseException("The value of the 'notes' property must be a JSON array");
                }
                return notesArray.DeepClone() as JArray;
            }

            return null;
        }
    }
}
