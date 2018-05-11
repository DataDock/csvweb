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

        public static string NormalizeCellValue(string cellValue, ColumnDescription column, DatatypeDescription cellDatatype)
        {
            var baseDatatype = cellDatatype == null ? DatatypeAnnotation.String : DatatypeAnnotation.GetAnnotationById(cellDatatype.Base);
            if (baseDatatype == null) throw new Converter.ConversionError($"Unrecognized cell base datatype ID: {cellDatatype.Base}");
            if (cellValue != null)
            {
                if (!RetainsLineEndings.Contains(baseDatatype))
                {
                    cellValue = cellValue.Replace('\u000d', ' ').Replace('\u000a', ' ').Replace('\u0009', ' ');
                }
                if (!RetainsLeadingAndTrailingWhitespace.Contains(baseDatatype))
                {
                    cellValue = cellValue.Trim();
                    cellValue = Regex.Replace(cellValue, @"\s+", " ");
                }
                if (cellValue.Equals(string.Empty))
                {
                    cellValue = column.Default;
                }
                /* Still TODO:
                 * 5. if the column separator annotation is not null, the cell value is a list of values; set the list annotation on the cell to true, and create the cell value created by:
                 *   5.1 if the normalized string is the same as any one of the values of the column null annotation, then the resulting value is null.
                 *   5.2 split the normalized string at the character specified by the column separator annotation.
                 *   5.3 unless the datatype base is string or anyAtomicType, strip leading and trailing whitespace from these strings.
                 *   5.4 applying the remaining steps to each of the strings in turn.
                 * 6. if the string is an empty string, apply the remaining steps to the string given by the column default annotation.
                 * 7. if the string is the same as any one of the values of the column null annotation, then the resulting value is null. If the column separator annotation is null and the column required annotation is true, add an error to the list of errors for the cell.
                 * 8. parse the string using the datatype format if one is specified, as described below to give a value with an associated datatype. If the datatype base is string, or there is no datatype, the value has an associated language from the column lang annotation. If there are any errors, add them to the list of errors for the cell; in this case the value has a datatype of string; if the datatype base is string, or there is no datatype, the value has an associated language from the column lang annotation.
                 * 9. validate the value based on the length constraints described in section 4.6.1 Length Constraints, the value constraints described in section 4.6.2 Value Constraints and the datatype format annotation if one is specified, as described below. If there are any errors, add them to the list of errors for the cell.
                 */
            }
            return cellValue;
        }
    }
}
