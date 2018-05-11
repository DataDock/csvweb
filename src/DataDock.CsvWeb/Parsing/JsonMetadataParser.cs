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
using System.Xml;
using DataDock.CsvWeb.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DataDock.CsvWeb.Parsing
{
    public class JsonMetadataParser
    {
        private readonly Uri _baseUri;
        private string _defaultLanguage;

        public JsonMetadataParser(Uri baseUri, string defaultLanguage = null)
        {
            _baseUri = baseUri;
            _defaultLanguage = defaultLanguage;
        }

        public object Parse(TextReader textReader)
        {
            var jsonParser = new Newtonsoft.Json.JsonSerializer();
            var rootObject = jsonParser.Deserialize(new JsonTextReader(textReader)) as JObject;
            if (rootObject == null)
            {
                throw new MetadataParseException("Expected root of JSON document to be an object.");
            }
            return Parse(rootObject);
        }

        public object Parse(JObject metadataObject)
        {
            JToken t;
            if (metadataObject.TryGetValue("url", out t)) return ParseTable(metadataObject);
            throw new MetadataParseException("Unrecognized root object type");
        }

        public Table ParseTable(JObject root)
        {
            JToken t;
            if (!root.TryGetValue("url", out t))
            {
                throw new MetadataParseException("Did not find required 'url' property on table object");
            }
            var url = (t as JValue)?.Value<string>();
            if (url == null) throw new MetadataParseException("The value of the 'url' property must be a string");
            var tableUri = ResolveUri(url);
            var table = new Table {Url = tableUri};
            if (root.TryGetValue("tableSchema", out t))
            {
                var schemaObject = t as JObject;
                if (schemaObject == null)
                {
                    throw new MetadataParseException("The value of the 'tableSchema' property must be a JSON object");
                }
                table.TableSchema = ParseTableSchema(table, schemaObject);
            }
            ParseInheritedProperties(root, table);
            return table;
        }

        private static Schema ParseTableSchema(Table table, JObject root)
        {
            var schema = new Schema(table) {Columns = new List<ColumnDescription>()};
            ParseInheritedProperties(root, schema);
            JToken t;
            if (root.TryGetValue("columns", out t))
            {
                var cols = t as JArray;
                if (cols == null) throw new MetadataParseException("The value of the 'columns' property must be a JSON array");
                foreach (var item in cols)
                {
                    var col = item as JObject;
                    if (col == null) throw new MetadataParseException("The items in the 'columns' array must be JSON objects");
                    var colDesc = ParseColumnDescription(schema, col);
                    schema.Columns.Add(colDesc);
                }
            }
            return schema;
        }

        private static ColumnDescription ParseColumnDescription(Schema schema, JObject root)
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
                columnDescription.SupressOutput = suppress.Value<bool>();
            }
            ParseInheritedProperties(root, columnDescription);
            return columnDescription;
        }

        private static void ValidateColumnName(string name)
        {
            if (name.StartsWith("_")) throw new MetadataParseException("$Column name {name} is not valid. Column names must not start with an _ character.");
            // TODO: Other rules (rules for variables in URL templates)
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
            return datatype;
        }

        private static void ValidateBaseDatatype(string baseTypeId)
        {
            var datatypeAnnotation = DatatypeAnnotation.GetAnnotationById(baseTypeId);
            if (datatypeAnnotation == null)
                throw new MetadataParseException(
                    $"Unable to match the datatype '{baseTypeId}' to a known datatype");
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
                    $"The value '{link} could not be parsed as either an aboslute or relative IRI.");
            }
            return ret;
        }

    }
}
