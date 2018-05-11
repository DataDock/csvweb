using System;

namespace DataDock.CsvWeb
{
    /// <summary>
    /// Exception raised if the template contains a replacement term that could not be bound with the provided
    /// replacement dictionary/function.
    /// </summary>
    public class UriTemplateBindingException : Exception
    {
        public UriTemplateBindingException(string unboundTerm) : base("Could not bind the replacement term \"" + unboundTerm + "\" in the URI template")
        {
        }
    }
}