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
using System.Text.RegularExpressions;

namespace DataDock.CsvWeb
{
    public class UriTemplate
    {
        private readonly string _templateString;
        private readonly Regex _replacementTermRegex = new Regex(@"\{([^\{]+)\}");

        public UriTemplate(string templateString)
        {
            _templateString = templateString;
        }

        /// <summary>
        /// Resolve the template to an absolute or relative IRI using the provided dictionary of replacement terms
        /// </summary>
        /// <param name="replacementTerms"></param>
        /// <returns></returns>
        public Uri Resolve(Dictionary<string, string> replacementTerms)
        {
            return Resolve(replacementTerm =>
            {
                if (!replacementTerms.TryGetValue(replacementTerm, out var replacementValue))
                {
                    throw new UriTemplateBindingException(replacementTerm);
                }
                return replacementValue;
            });
        }

        /// <summary>
        /// Resolve the template to an absolute IRI using the provided dictionary of replacement terms and the
        /// provided base URI for relative IRI resolution.
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="replacementTerms"></param>
        /// <returns></returns>
        public Uri Resolve(Uri baseUri, Dictionary<string, string> replacementTerms)
        {
            return new Uri(baseUri, Resolve(replacementTerms));
        }

        /// <summary>
        /// Resolve the template to an absolute or relative IRI using the provided replacement function
        /// </summary>
        /// <param name="replacementFunc"></param>
        /// <returns></returns>
        public Uri Resolve(Func<string, string> replacementFunc)
        {
            var resolvedTemplate = _replacementTermRegex.Replace(
                _templateString,
                match => ResolveExpression(match.Groups[1].Value, replacementFunc));
            return new Uri(resolvedTemplate, UriKind.RelativeOrAbsolute);
        }

        /// <summary>
        /// Resolve the template to an absolute IRI using the provided replacement function and
        /// base IRI for relative IRI resolution.
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="replacementFunc"></param>
        /// <returns></returns>
        public Uri Resolve(Uri baseUri, Func<string, string> replacementFunc)
        {
            return new Uri(baseUri, Resolve(replacementFunc));
        }

        private string ResolveExpression(string expr, Func<string, string> replacementFunc)
        {
            if (expr[0] == '#')
            {
                return "#" + string.Join(",", ResolveVariableList(expr.Substring(1), replacementFunc));
            }

            return string.Join(",", ResolveVariableList(expr, replacementFunc));
        }


        private IEnumerable<string> ResolveVariableList(string varList, Func<string, string> replacementFunc)
        {
            foreach (var v in varList.Split(','))
            {
                var replacmentValue = replacementFunc(v);
                if (string.IsNullOrEmpty(replacmentValue))
                {
                    throw new UriTemplateBindingException(v);
                }

                yield return replacmentValue;
            }
        }
    }
}
