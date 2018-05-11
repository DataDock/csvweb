using System;

namespace DataDock.CsvWeb.Metadata
{
    public class DatatypeDescription
    {
        /// <summary>
        /// The absolute URL that identifies the datatype, or null if undefined
        /// </summary>
        public Uri Id { get; set; }

        /// <summary>
        ///  The annotation that determines the base datatype from which this datatype is derived
        /// </summary>
        public string Base { get; set; }
    }
}
