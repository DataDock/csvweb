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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataDock.CsvWeb.Metadata;

namespace DataDock.CsvWeb.Rdf
{
    public class CellParser
    {
        private static readonly DatatypeAnnotation[] RetainsLineEndings =
            {
                DatatypeAnnotation.String, DatatypeAnnotation.Json, DatatypeAnnotation.Html,
                DatatypeAnnotation.AnyAtomicType
            };

        private static readonly DatatypeAnnotation[] RetainsLeadingAndTrailingWhitespace =
        {
            DatatypeAnnotation.String, DatatypeAnnotation.Json, DatatypeAnnotation.Html,
            DatatypeAnnotation.AnyAtomicType, DatatypeAnnotation.NormalizedString
        };

        private static readonly DatatypeAnnotation[] RetainsLeadingAndTrailingWhitespaceInList =
        {
            DatatypeAnnotation.String, DatatypeAnnotation.AnyAtomicType
        };

        public static CellValue NormalizeCellValue(string rawValue, ColumnDescription column,
            DatatypeDescription cellDatatype)
        {
            var baseDatatype = cellDatatype == null
                ? DatatypeAnnotation.String
                : DatatypeAnnotation.GetAnnotationById(cellDatatype.Base);
            if (baseDatatype == null)
                throw new Converter.ConversionError($"Unrecognized cell base datatype ID: {cellDatatype.Base}");
            var cellValue = new CellValue {RawString = rawValue};
            if (rawValue == null)
            {
                return cellValue;
            }

            var normalizedString = rawValue;

            if (!RetainsLineEndings.Contains(baseDatatype))
            {
                normalizedString = normalizedString.Replace('\u000d', ' ').Replace('\u000a', ' ')
                    .Replace('\u0009', ' ');
            }

            if (!RetainsLeadingAndTrailingWhitespace.Contains(baseDatatype))
            {
                normalizedString = normalizedString.Trim();
                normalizedString = Regex.Replace(normalizedString, @"\s+", " ");
            }

            if (normalizedString.Equals(string.Empty))
            {
                normalizedString = column.Default;
            }

            cellValue.NormalizedString = normalizedString;

            // 5. if the column separator annotation is not null, the cell value is a list of values; set the list annotation on the cell to true, and create the cell value created by:
            if (column.Separator != null)
            {
                cellValue.IsList = true;
                // 5.1 if the normalized string is the same as any one of the values of the column null annotation, then the resulting value is null.
                if (column.Null.Contains(normalizedString) || normalizedString==null)
                {
                    cellValue.ValueList = null;
                }
                else
                {
                    cellValue.ValueList = new List<string>();
                    // 5.2 split the normalized string at the character specified by the column separator annotation.
                    foreach (var tok in normalizedString.Split(new[] {column.Separator}, StringSplitOptions.None))
                    {
                        // 5.3 unless the datatype base is string or anyAtomicType, strip leading and trailing whitespace from these strings.
                        var normalizedToken = tok;
                        if (!RetainsLeadingAndTrailingWhitespaceInList.Contains(baseDatatype))
                        {
                            normalizedToken = normalizedToken.Trim();
                        }
                        // 5.4 applying the remaining steps to each of the strings in turn.
                        NormalizeCellValue(cellValue, normalizedToken, column, cellDatatype);
                    }
                }
            }
            else
            {
                NormalizeCellValue(cellValue, normalizedString, column, cellDatatype);
            }

            return cellValue;
        }

        private static void NormalizeCellValue(CellValue cellValue, string str, ColumnDescription column, DatatypeDescription datatype)
        {
            // 6. if the string is an empty string, apply the remaining steps to the string given by the column default annotation.
            if (string.Empty.Equals(str)) str = column.Default;

            // 7. if the string is the same as any one of the values of the column null annotation, then the resulting value is null.
            // If the column separator annotation is null and the column required annotation is true, add an error to the list of errors for the cell.
            if (column.Null.Contains(str))
            {
                str = null;
                if (column.Separator == null && column.Required)
                {
                    cellValue.Errors.Add("Got NULL value for a required cell");
                }
            }
            /* Still TODO:
             * 8. parse the string using the datatype format if one is specified, as described below to give a value with an associated datatype. If the datatype base is string, or there is no datatype, the value has an associated language from the column lang annotation. If there are any errors, add them to the list of errors for the cell; in this case the value has a datatype of string; if the datatype base is string, or there is no datatype, the value has an associated language from the column lang annotation.
             * 9. validate the value based on the length constraints described in section 4.6.1 Length Constraints, the value constraints described in section 4.6.2 Value Constraints and the datatype format annotation if one is specified, as described below. If there are any errors, add them to the list of errors for the cell.
             */
            if (cellValue.IsList)
            {
                cellValue.ValueList.Add(str);
            }
            else
            {
                cellValue.Value = str;
            }
        }
    }

    public class CellValue
    {
        public string RawString { get; set; }
        public string NormalizedString { get; set; }
        public bool IsList { get; set; }
        public List<string> ValueList { get; set; }
        public string Value { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
