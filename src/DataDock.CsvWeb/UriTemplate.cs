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
        /// Resolve the template to an absolute or relateive IRI using the provided replacement function
        /// </summary>
        /// <param name="replacementFunc"></param>
        /// <returns></returns>
        public Uri Resolve(Func<string, string> replacementFunc)
        {
            var resolvedTemplate = _replacementTermRegex.Replace(_templateString, match =>
            {
                var replacementValue = replacementFunc(match.Groups[1].Value);
                if (string.IsNullOrEmpty(replacementValue))
                {
                    throw new UriTemplateBindingException(match.Groups[1].Value);
                }
                return replacementValue;
            });

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
    }
}
