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

        /// <summary>
        /// The annotation that defines the format of a value of this type, used when parsing a string value
        /// </summary>
        public IFormatSpecification Format { get; set; }

        /// <summary>
        /// Constraints derived from the constraint annotations
        /// </summary>
        public readonly IList<IDatatypeConstraint> Constraints = new List<IDatatypeConstraint>();
    }
}
