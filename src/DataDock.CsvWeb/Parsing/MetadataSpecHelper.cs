using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataDock.CsvWeb.Parsing
{
    public class MetadataSpecHelper
    {
        public static string[] ArrayProperties = new[]
            {"tables", "transformations", "notes", "@context", "foreignKeys", "columns", "lineTerminators"};

        public static string[] LinkProperties =
            {"url", "targetFormat", "scriptFormat", "@id", "resource", "schemaReference"};

        public static string[] UriTemplateProperties = { "aboutUrl", "propertyUrl", "valueUrl" };
        public static string[] ColumnReferenceProperties = { "columnReference", "primaryKey", "rowTitles" };
        public static string[] ObjectProperties = { "reference", "tableSchema", "dialect" };
        public static string[] NaturalLanguageProperties = { "titles" };

        public static string[] AtomicProperties =
        {
            "source", "@type", "null", "lang", "textDirection", "separator", "ordered", "default", "datatype",
            "required",
            "base", "format", "length", "minLength", "maxLength", "minimum", "maximum", "minInclusive", "maxInclusive",
            "minExclusive", "maxExclusive",
            "decimalChar", "groupChar", "pattern",
            "tableDirection", "suppressOutput",
            "@language", "@base",
            "commentPrefix", "doubleQuote", "delimiter", "encoding", "header", "headerRowCount", "quoteChar",
            "skipBlankRows", "skipColumns", "skipInitialSpace", "skipRows", "trim",
            "names", "virtual"
        };

        public static readonly string CsvwMetadataContext = "http://www.w3.org/ns/csvw";

        public static bool IsArrayProperty(string propertyName)
        {
            return ArrayProperties.Contains(propertyName);
        }

        public static bool IsLinkProperty(string propertyName)
        {
            return LinkProperties.Contains(propertyName);
        }

        public static bool IsUriTemplateProperty(string propertyName)
        {
            return UriTemplateProperties.Contains(propertyName);
        }

        public static bool IsColumnReferenceProperty(string propertyName)
        {
            return ColumnReferenceProperties.Contains(propertyName);
        }

        public static bool IsObjectProperty(string propertyName)
        {
            return ObjectProperties.Contains(propertyName);
        }

        public static bool IsNaturalLanguageProperty(string propertyName)
        {
            return NaturalLanguageProperties.Contains(propertyName);
        }

        public static bool IsAtomicProperty(string propertyName)
        {
            return AtomicProperties.Contains(propertyName);
        }

        public static bool IsCommonProperty(string propertyName)
        {
            return propertyName.Contains(":") && !propertyName.Contains("://");
        }
    }
}
