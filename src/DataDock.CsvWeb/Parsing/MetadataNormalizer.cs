﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataDock.CsvWeb.Parsing
{
    internal class MetadataNormalizer
    {
        private readonly Uri _baseUri;
        private readonly string _defaultLanguage;
        private readonly JObject _csvwContext;
        private readonly ITableResolver _resolver;
        public List<ParserWarning> Warnings { get; }

        public MetadataNormalizer(ITableResolver resolver, Uri baseUri, string defaultLanguage = null)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
            _defaultLanguage = defaultLanguage;
            Warnings = new List<ParserWarning>();
            using (var reader =
                new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("DataDock.CsvWeb.Resources.csvw.jsonld"),
                    Encoding.UTF8))
            {
                _csvwContext = JsonConvert.DeserializeObject<JObject>(reader.ReadToEnd())["@context"] as JObject;
            }
        }
        
        public JObject NormalizeMetadata(JObject o)
        {
            var context = new NormalizationContext{BaseUri = _baseUri, DefaultLangauge = _defaultLanguage};
            if (o.ContainsKey("@context"))
            {
                context = ProcessContext(o["@context"]);
                o.Remove("@context");
            }

            NormalizeObject(o, context, null);

            o["@context"] = MetadataSpecHelper.CsvwMetadataContext;

            return o;
        }

        private void EnsureType(JObject o, string ensureValue)
        {
            if (o.ContainsKey("@type"))
            {
                var typeValue = o["@type"].Value<string>();
                if (typeValue != ensureValue)
                {
                    throw new MetadataParseException($"Error at {o.Path}. Expected @type property to be set to '{ensureValue}', but found '{typeValue}'");
                }
            }
            else
            {
                o["@type"] = ensureValue;
            }
        }

        private void NormalizeObject(JObject o, NormalizationContext context, string parentProperty)
        {
            // Attempt to determine object type
            if (o.ContainsKey("tables"))
            {
                EnsureType(o, "TableGroup");
            }

            if (parentProperty == "tables" || parentProperty == null && o.ContainsKey("url"))
            {
                EnsureType(o, "Table");
            }

            if (parentProperty == "tableSchema")
            {
                EnsureType(o, "Schema");
            }

            if (parentProperty == "columns")
            {
                EnsureType(o, "Column");
            }

            if (parentProperty == "dialect")
            {
                EnsureType(o, "Dialect");
            }

            if (parentProperty == "transformations")
            {
                EnsureType(o, "Template");
            }

            foreach (var p in o.Properties().ToList())
            {
                if (MetadataSpecHelper.IsCommonProperty(p.Name) || p.Name.Equals("notes"))
                {
                    p.Value = NormalizeCommonPropertyValue(p.Value, context);
                }
                else if (MetadataSpecHelper.IsArrayProperty(p.Name))
                {
                    if (!(p.Value is JArray))
                    {
                        Warnings.Add(new ParserWarning(p, "Expected property value to be an array. The given property value will not be processed."));
                        o.Remove(p.Name);
                    }
                    else
                    {
                        var array = (JArray) p.Value;
                        foreach (var t in array)
                        {
                            if (t is JObject item)
                            {
                                NormalizeObject(item, context, p.Name);
                            }
                        }
                    }
                }
                else if (MetadataSpecHelper.IsLinkProperty(p.Name))
                {
                    if (p.Name == "@id" && p.Value.Value<string>().StartsWith("_:"))
                    {
                        throw new MetadataParseException("An @id property may not start with the string '_:'");
                    }
                    if (p.Value.Type == JTokenType.String)
                    {
                        p.Value = new Uri(context.BaseUri, p.Value.Value<string>()).ToString();
                    }
                }
                else if (MetadataSpecHelper.IsObjectProperty(p.Name))
                {
                    if (p.Value.Type == JTokenType.String)
                    {
                        p.Value = ResolveObjectReference(p.Value.Value<string>(), context, p.Name);
                    }
                    else if (p.Value is JObject obj)
                    {
                        NormalizeObject(obj, context, p.Name);
                    }
                    else
                    {
                        throw new MetadataParseException("Property " + p.Name +
                                                         " must be either a URI reference or an object. Found " +
                                                         Enum.GetName(typeof(JTokenType), p.Value.Type));
                    }
                }
                else if (MetadataSpecHelper.IsNaturalLanguageProperty(p.Name))
                {
                    if (p.Value.Type == JTokenType.String)
                    {
                        p.Value = new JObject(new JProperty(context.DefaultLangauge ?? "und", new JArray(p.Value)));
                    }
                    else if (p.Value.Type == JTokenType.Array)
                    {
                        p.Value = new JObject(new JProperty(context.DefaultLangauge ?? "und", p.Value));
                    }
                    else if (p.Value.Type != JTokenType.Object)
                    {
                        throw new MetadataParseException("Property " + p.Name +
                                                         " must be either a string, an array of string or an object. Found " +
                                                         Enum.GetName(typeof(JTokenType), p.Value.Type));
                    }
                }
                else if (MetadataSpecHelper.IsAtomicProperty(p.Name))
                {
                    if (p.Value.Type == JTokenType.String)
                    {
                        var stringValue = p.Value.Value<string>();
                        switch (p.Name)
                        {
                            case "datatype":
                                p.Value = new JObject(new JProperty("base", stringValue));
                                break;
                            // The "format" property can also be either a string or an object but there are no normalization rules for this property in the spec
                        }
                    }
                }
                else if (MetadataSpecHelper.IsUriTemplateProperty(p.Name))
                {
                    if (p.Value.Type != JTokenType.String)
                    {
                        Warnings.Add(new ParserWarning(p.Path, "The value of the {p.Name} property must be a string"));
                        p.Value = ResolveId("", context);
                    }

                    var stringValue = p.Value.Value<string>();
                    p.Value = ResolveId(stringValue, context);
                }
            }
        }

        private JObject ResolveObjectReference(string href, NormalizationContext context, string propertyName)
        {
            var uri = new Uri(context.BaseUri, href);
            var o =_resolver.ResolveJsonAsync(uri).Result;
            NormalizeObject(o, new NormalizationContext{BaseUri = uri, DefaultLangauge = context.DefaultLangauge}, propertyName);
            return o;
        }

        private NormalizationContext ProcessContext(JToken context)
        {
            if (context is JArray array)
            {
                foreach (var item in array)
                {
                    if (item is JObject localContext)
                    {
                        return ProcessContext(localContext);
                    }
                }
            }

            if (context is JObject o)
            {
                var nc = new NormalizationContext {BaseUri = _baseUri};
                if (o.ContainsKey("@base"))
                {
                    nc.BaseUri = new Uri(nc.BaseUri, o["@base"].Value<string>());
                }

                if (o.ContainsKey("@language"))
                {
                    var language = o["@language"].Value<string>();
                    if (LanguageTag.IsValid(language))
                    {
                        nc.DefaultLangauge = language;
                    }
                    else
                    {
                        Warnings.Add(new ParserWarning(o.Property("@language"), "The value of the '@language' property must be a valid BCP-47 language tag."));
                    }
                }
                return nc;
            }

            return new NormalizationContext{BaseUri = _baseUri, DefaultLangauge = _defaultLanguage};
        }

        private JToken NormalizeCommonPropertyValue(JToken t, NormalizationContext context)
        {
            if (t is JArray array)
            {
                var ret = new JArray();
                foreach (var v in array)
                {
                    ret.Add(NormalizeCommonPropertyValue(v, context));
                }

                return ret;
            }

            if (t.Type == JTokenType.String)
            {
                var ret = new JObject(new JProperty("@value", t.Value<string>()));
                if (!string.IsNullOrEmpty(context.DefaultLangauge))
                {
                    ret.Add(new JProperty("@language", context.DefaultLangauge));
                }

                return ret;
            }

            if (t.Type == JTokenType.Object)
            {
                var o = t as JObject;
                if (o.ContainsKey("@value")) return o;
                foreach (var p in o.Properties())
                {
                    if (p.Name.Equals("@id"))
                    {
                        var id = p.Value.Value<string>();
                        if (id.StartsWith("_:"))
                        {
                            throw new MetadataParseException("An @id property must not start with '_:'");
                        }
                        p.Value = ResolveId(p.Value.Value<string>(), context);
                    }
                    else if (!p.Name.Equals("@type"))
                    {
                        p.Value = NormalizeCommonPropertyValue(p.Value, context);
                    }
                }

                return o;
            }

            return t;
        }

        private string ResolveId(string id, NormalizationContext context)
        {
            if (id.Contains(":"))
            {
                var parts = id.Split(new[] {':'}, 2);
                var prefix = parts[0];
                var suffix = parts[1];
                if (!(prefix.Equals("_") || suffix.StartsWith("//")) && _csvwContext.ContainsKey(prefix))
                {
                    id = _csvwContext.GetValue(prefix).Value<string>() + suffix;
                }
            }

            return id;
        }

        private class NormalizationContext
        {
            public Uri BaseUri;
            public string DefaultLangauge;
        }
    }
}
