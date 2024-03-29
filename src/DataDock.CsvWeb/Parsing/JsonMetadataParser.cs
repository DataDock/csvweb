﻿#region copyright
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
using System.Runtime.CompilerServices;
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
        private List<ParserWarning> Warnings { get; }
        

        public JsonMetadataParser(ITableResolver resolver, Uri baseUri, string defaultLanguage = null)
        {
            _resolver = resolver;
            _baseUri = baseUri;
            _defaultLanguage = defaultLanguage;
            Warnings = new List<ParserWarning>();
        }

        public TableGroup Parse(TextReader textReader)
        {
            var jsonParser = new JsonSerializer();
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
                tableGroup.Id = ParseLinkProperty(t, "@id");
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
            var tableUri = ParseLinkProperty(t, "url");
            var table = new Table(tableGroup) {Url = tableUri};
            if (root.TryGetValue("@id", out t))
            {
                table.Id = ParseLinkProperty(t, "@id");
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

        private Uri ParseLinkProperty(JToken token, string propertyName)
        {
            if (token.Type != JTokenType.String)
            {
                Warn(token, $"Value of property '{propertyName}' must be a string");
                return ResolveUri(string.Empty);
            }
            var id = (token as JValue)?.Value<string>();
            if (id == null)
            {
                Warn(token, $"Value of property '{propertyName}' must be a string");
                return ResolveUri(string.Empty);
            }
            return ResolveUri(id);
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
                if (!(t is JValue nameValue) || nameValue.Type != JTokenType.String) throw new MetadataParseException("The value of the 'name' property must be a string");
                ValidateColumnName(nameValue.Value<string>());
                columnDescription.Name = nameValue.Value<string>();
            }
            if (root.TryGetValue("suppressOutput", out t))
            {
                if (!(t is JValue suppress) || suppress.Type != JTokenType.Boolean)
                    throw new MetadataParseException("The value of the 'suppressOutput' property must be a boolean");
                columnDescription.SuppressOutput = suppress.Value<bool>();
            }

            if (root.TryGetValue("titles", out t))
            {
                columnDescription.Titles = ParseNaturalLanguageProperty(t);
            }

            if (root.TryGetValue("default", out t))
            {
                if (!(t is JValue defaultValue) ||
                    defaultValue.Type != JTokenType.String && defaultValue.Type != JTokenType.Date)
                {
                    throw new MetadataParseException("The value of the 'default' property must be a string");
                }
                columnDescription.Default = defaultValue.Type == JTokenType.Date ? JsonConvert.SerializeObject(defaultValue).Trim('"') : defaultValue.Value<string>();
            }

            if (root.TryGetValue("virtual", out t))
            {
                if (!(t is JValue virtualValue) || virtualValue.Type != JTokenType.Boolean)
                {
                    throw new MetadataParseException("The value of the 'virtual' property must be a boolean");
                }
                columnDescription.Virtual = virtualValue.Value<bool>();
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

        private Dialect ParseDialect(JObject root)
        {
            var dialect = new Dialect
            {
                CommentPrefix = ParseStringProperty(root, "commentPrefix", "#"),
                Delimiter = ParseStringProperty(root, "delimiter", ","),
                DoubleQuote = ParseBooleanProperty(root, "doubleQuote", true),
                Encoding = ParseStringProperty(root, "encoding", "utf-8"),
                Header = ParseBooleanProperty(root, "header", true),
                LineTerminators = ParseArrayOfStrings(root, "lineTerminators", new string[] {"\r\n", "\n"}),
                QuoteChar = ParseStringProperty(root, "quoteChar", "\"", true),
                SkipBlankRows = ParseBooleanProperty(root, "skipBlankRows", false),
                SkipColumns = ParseNonNegativeIntegerProperty(root, "skipColumns", 0),
                SkipInitialSpace = ParseBooleanProperty(root, "skipInitialSpace", false),
                SkipRows = ParseNonNegativeIntegerProperty(root, "skipRows", 0)
            };

            dialect.HeaderRowCount = ParseNonNegativeIntegerProperty(root, "headerRowCount", dialect.Header ? 1 : 0);
            if (root.ContainsKey("trim"))
            {
                var trimProperty = root.Property("trim");
                if (trimProperty.Value.Type == JTokenType.Boolean)
                {
                    dialect.Trim = trimProperty.Value.Value<bool>() ? CsvTrim.True : CsvTrim.False;
                }
                else if (trimProperty.Value.Type == JTokenType.String)
                {
                    switch (trimProperty.Value<string>())
                    {
                        case "true":
                            dialect.Trim = CsvTrim.True;
                            break;
                        case "false":
                            dialect.Trim = CsvTrim.False;
                            break;
                        case "start":
                            dialect.Trim = CsvTrim.Start;
                            break;
                        case "end":
                            dialect.Trim = CsvTrim.End;
                            break;
                        default:
                            Warn(trimProperty,
                                $"Expected value to be one of 'true', 'false', 'start', or 'end'. Found {trimProperty.Value<string>()}. Using default value 'true'");
                            dialect.Trim = CsvTrim.True;
                            break;
                    }
                }
            }
            else
            {
                dialect.Trim = dialect.SkipInitialSpace ? CsvTrim.Start : CsvTrim.False;
            }

            try
            {
                System.Text.Encoding.GetEncoding(dialect.Encoding);
            }
            catch (ArgumentException)
            {
                Warn(root.Property("encoding"), $"{root.Property("encoding").Value.Value<string>()} is not a recognized text encoding. Using default value 'utf-8'");
                dialect.Encoding = "utf-8";
            }
            return dialect;
        }

        private string ParseStringProperty(JObject root, string propertyName, string defaultValue,
            bool allowNull = false)
        {
            if (root.ContainsKey(propertyName))
            {
                var property = root.Property(propertyName);
                if (property.Value.Type == JTokenType.String)
                {
                    return property.Value.Value<string>();
                }

                if (allowNull && property.Type == JTokenType.Null)
                {
                    return null;
                }

                Warn(property, "Value must be a string" + (allowNull ? " or null" : ""));
            }

            return defaultValue;
        }

        private bool ParseBooleanProperty(JObject root, string propertyName, bool defaultValue)
        {
            if (root.ContainsKey(propertyName))
            {
                var property = root.Property(propertyName);
                if (property.Value.Type == JTokenType.Boolean)
                {
                    return property.Value.Value<bool>();
                }
                Warn(property, "Value must be a boolean");
            }
            return defaultValue;
        }

        private int ParseIntegerProperty(JObject root, string propertyName, int defaultValue)
        {
            if (root.ContainsKey(propertyName))
            {
                var property = root.Property(propertyName);
                if (property.Value.Type == JTokenType.Integer)
                {
                    return property.Value.Value<int>();
                }
                Warn(property, "Value must be a boolean");
            }
            return defaultValue;
        }

        private int ParseNonNegativeIntegerProperty(JObject root, string propertyName, int defaultValue)
        {
            var value = ParseIntegerProperty(root, propertyName, defaultValue);
            if (value >= 0) return value;
            Warn(root.Property(propertyName),
                $"Expected value to be a non-negative integer. Found {value}.");
            return defaultValue;
        }

        private string[] ParseArrayOfStrings(JObject root, string propertyName, string[] defaultValue,
            bool allowSingleValue = true)
        {
            if (!root.ContainsKey(propertyName)) return defaultValue;

            var property = root.Property(propertyName);
            if (property.Value.Type == JTokenType.Array)
            {
                if (property.Value is JArray values)
                {
                    return values.Where(v => v.Type == JTokenType.String).Select(v => v.Value<string>()).ToArray();
                }
            }

            if (allowSingleValue && property.Value.Type == JTokenType.String)
            {
                return new[] {property.Value.Value<string>()};
            }

            Warn(property, "Value must be an array of strings" + (allowSingleValue ? " or a string" : ""));
            return defaultValue;
        }

        private void ParseInheritedProperties(JObject root, InheritedPropertyContainer container)
        {
            JToken t;
            if (root.TryGetValue("datatype", out t))
            {
                var datatypeVal = t as JValue;
                if (datatypeVal != null && datatypeVal.Type == JTokenType.String)
                {
                    var datatypeId = datatypeVal.Value<string>();
                    if (IsValidBaseDatatype(datatypeId))
                    {
                        container.Datatype = new DatatypeDescription {Base = datatypeId};
                    }
                    else
                    {
                        Warn(t, $"Unsupported base datatype '{datatypeId}");
                    }
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
                if (t is JValue v && v.Type == JTokenType.String)
                {
                    var langString = v.Value<string>();
                    if (LanguageTag.IsValid(langString))
                    {
                        container.Lang = v.Value<string>();
                    }
                    else
                    {
                        Warn(t, $"The value '{langString}' is not a valid BCP-47 language tag.");
                    }
                }
                else
                {
                    Warn(t, "The value of the 'lang' property must be a string");
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

        private DatatypeDescription ParseDatatype(JObject root)
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
                if (IsValidBaseDatatype(datatypeId))
                {
                    datatype.Base = datatypeId;
                }
                else
                {
                    Warn(t, $"Unsupported base datatype '{datatypeId}'");
                    datatype.Base = "string";
                }
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
                    case "time":
                        datatype.Format = new TimeFormatSpecification(t.Value<string>());
                        break;
                    case "datetime":
                        datatype.Format = new DateTimeFormatSpecification(t.Value<string>());
                        break;
                    case "number":
                    case "decimal":
                    case "double":
                    case "float":
                        if (t.Type == JTokenType.Object)
                        {
                            var formatObject = t as JObject;
                            datatype.Format = new NumericFormatSpecification(
                                formatObject.ContainsKey("decimalChar")?formatObject["decimalChar"].Value<string>()[0] : '.',
                                formatObject.ContainsKey("groupChar")?formatObject["groupChar"].Value<string>()[0]:',',
                                formatObject.ContainsKey("pattern")?formatObject["pattern"].Value<string>():null);
                        }
                        else
                        {
                            datatype.Format = new NumericFormatSpecification(t.Value<string>());
                        }

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

        private static bool IsValidBaseDatatype(string baseTypeId)
        {
            var datatypeAnnotation = DatatypeAnnotation.GetAnnotationById(baseTypeId);
            return datatypeAnnotation != null;
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

        private void Warn(JToken contextToken, string msg)
        {
            Warnings.Append(new ParserWarning(contextToken, msg));
        }
    }
}
