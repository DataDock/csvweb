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
using System.Linq;

namespace DataDock.CsvWeb.Metadata
{
    public class DatatypeAnnotation
    {
        public string Id { get; }
        public Uri Iri { get; }

        private static readonly List<DatatypeAnnotation> _all = new List<DatatypeAnnotation>();
        public static IEnumerable<DatatypeAnnotation> All => _all;

        private DatatypeAnnotation(string annotation, Uri datatypeIri)
        {
            Id = annotation;
            Iri = datatypeIri;
        }

        private const string Xsd = "http://www.w3.org/2001/XMLSchema#";
        private const string Csvw = "http://www.w3.org/ns/csvw#";
        private const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        public static DatatypeAnnotation Number = RegisterAnnotation("number", new Uri(Xsd + "double"));
        public static DatatypeAnnotation Binary = RegisterAnnotation("binary", new Uri(Xsd + "base64Binary"));
        public static DatatypeAnnotation Datetime = RegisterAnnotation("datetime", new Uri(Xsd + "dateTime"));
        public static DatatypeAnnotation Any = RegisterAnnotation("any", new Uri(Xsd + "anyAtomicType"));
        public static DatatypeAnnotation AnyAtomicType = RegisterAnnotation("anyAtomicType",
            new Uri(Xsd + "anyAtomicType"));
        public static DatatypeAnnotation AnyURi = RegisterAnnotation("anyURI", new Uri(Xsd + "anyURI"));
        public static DatatypeAnnotation Base64Binary = RegisterAnnotation("base64Binary",
            new Uri(Xsd + "base64Binary"));
        public static DatatypeAnnotation Boolean = RegisterAnnotation("boolean", new Uri(Xsd + "boolean"));
        public static DatatypeAnnotation Date = RegisterAnnotation("date", new Uri(Xsd + "date"));
        public static DatatypeAnnotation DateTime = RegisterAnnotation("dateTime", new Uri(Xsd + "dateTime"));
        public static DatatypeAnnotation DateTimeStamp = RegisterAnnotation("dateTimeStamp",
            new Uri(Xsd + "dateTimeStamp"));
        public static DatatypeAnnotation Decimal = RegisterAnnotation("decimal", new Uri(Xsd + "decimal"));
        public static DatatypeAnnotation Integer = RegisterAnnotation("integer", new Uri(Xsd + "integer"));
        public static DatatypeAnnotation Long = RegisterAnnotation("long", new Uri(Xsd + "long"));
        public static DatatypeAnnotation Int = RegisterAnnotation("int", new Uri(Xsd + "int"));
        public static DatatypeAnnotation Short = RegisterAnnotation("short", new Uri(Xsd + "short"));
        public static DatatypeAnnotation Byte = RegisterAnnotation("byte", new Uri(Xsd + "byte"));
        public static DatatypeAnnotation NonNegativeInteger = RegisterAnnotation("nonNegativeInteger",
            new Uri(Xsd + "nonNegativeInteger"));
        public static DatatypeAnnotation PositiveInteger = RegisterAnnotation("positiveInteger",
            new Uri(Xsd + "positiveInteger"));
        public static DatatypeAnnotation UnsignedLong = RegisterAnnotation("unsignedLong",
            new Uri(Xsd + "unsignedLong"));
        public static DatatypeAnnotation UnsignedInt = RegisterAnnotation("unsignedInt",
            new Uri(Xsd + "unsignedInt"));
        public static DatatypeAnnotation UnsignedShort = RegisterAnnotation("unsignedShort",
            new Uri(Xsd + "unsignedShort"));
        public static DatatypeAnnotation UnsignedByte = RegisterAnnotation("unsignedByte",
            new Uri(Xsd + "unsignedByte"));
        public static DatatypeAnnotation NonPositiveInteger = RegisterAnnotation("nonPositiveInteger",
            new Uri(Xsd + "nonPositiveInteger"));
        public static DatatypeAnnotation NegativeInteger = RegisterAnnotation("negativeInteger",
            new Uri(Xsd + "negativeInteger"));
        public static DatatypeAnnotation Double = RegisterAnnotation("double", new Uri(Xsd + "double"));
        public static DatatypeAnnotation Duration = RegisterAnnotation("duration", new Uri(Xsd + "duration"));
        public static DatatypeAnnotation DayTimeDuration = RegisterAnnotation("dayTimeDuration",
            new Uri(Xsd + "dayTimeDuration"));
        public static DatatypeAnnotation YearMonthDuration = RegisterAnnotation("yearMonthDuration",
            new Uri(Xsd + "yearMonthDuration"));
        public static DatatypeAnnotation Float = RegisterAnnotation("float", new Uri(Xsd + "float"));
        public static DatatypeAnnotation GDay = RegisterAnnotation("gDay", new Uri(Xsd + "gDay"));
        public static DatatypeAnnotation GMonth = RegisterAnnotation("gMonth", new Uri(Xsd + "gMonth"));
        public static DatatypeAnnotation GMonthDay = RegisterAnnotation("gMonthDay", new Uri(Xsd + "gMonthDay"));
        public static DatatypeAnnotation GYear = RegisterAnnotation("gYear", new Uri(Xsd + "gYear"));
        public static DatatypeAnnotation GYearMonth = RegisterAnnotation("gYearMonth", new Uri(Xsd + "gYearMonth"));
        public static DatatypeAnnotation HexBinary = RegisterAnnotation("hexBinary", new Uri(Xsd + "hexBinary"));
        public static DatatypeAnnotation QName = RegisterAnnotation("QName", new Uri(Xsd + "QName"));
        public static DatatypeAnnotation String = RegisterAnnotation("string", new Uri(Xsd + "string"));
        public static DatatypeAnnotation LangString = RegisterAnnotation("langString", new Uri(Rdf + "langString"));
        public static DatatypeAnnotation NormalizedString = RegisterAnnotation("normalizedString",
            new Uri(Xsd + "normalizedString"));
        public static DatatypeAnnotation Token = RegisterAnnotation("token", new Uri(Xsd + "token"));
        public static DatatypeAnnotation Language = RegisterAnnotation("language", new Uri(Xsd + "language"));
        public static DatatypeAnnotation Name = RegisterAnnotation("Name", new Uri(Xsd + "Name"));
        public static DatatypeAnnotation NMTOKEN = RegisterAnnotation("NMTOKEN", new Uri(Xsd + "NMTOKEN"));
        public static DatatypeAnnotation Xml = RegisterAnnotation("xml", new Uri(Rdf + "XMLLiteral"));
        public static DatatypeAnnotation Html = RegisterAnnotation("html", new Uri(Rdf + "HTML"));
        public static DatatypeAnnotation Json = RegisterAnnotation("json", new Uri(Csvw + "JSON"));
        public static DatatypeAnnotation Time = RegisterAnnotation("time", new Uri(Xsd + "time"));

        public static DatatypeAnnotation RegisterAnnotation(string annotationId, Uri datatypeUri)
        {
            var existing = GetAnnotationById(annotationId);
            if (existing != null) _all.Remove(existing);
            var annotation = new DatatypeAnnotation(annotationId, datatypeUri);
            _all.Add(annotation);
            return annotation;
        }

        public static DatatypeAnnotation GetAnnotationById(string annotationId)
        {
            return _all.FirstOrDefault(x => x.Id.Equals(annotationId));
        }
    }
}