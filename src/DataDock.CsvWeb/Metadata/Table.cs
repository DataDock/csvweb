﻿#region copyright
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
using Newtonsoft.Json.Linq;

namespace DataDock.CsvWeb.Metadata
{
    public class Table : InheritedPropertyContainer, ICommonPropertyContainer
    {
        /// <summary>
        /// Creates a new table in the specified parent table group
        /// </summary>
        /// <param name="tableGroup"></param>
        public Table(TableGroup tableGroup) : base(tableGroup)
        {
            tableGroup.Tables.Add(this);
            Dialect = tableGroup.Dialect ?? new Dialect();
        }

        /// <summary>
        /// The table identifier as specified by the id annotation on the table
        /// </summary>
        public Uri Id { get; set; }
        /// <summary>
        /// The URL from which the table content can be loaded
        /// </summary>
        public Uri Url { get; set; }

        public Schema TableSchema { get; set; }

        public Dialect Dialect { get; set; }

        public bool SuppressOutput { get; set; }

        public JObject CommonProperties { get; } = new JObject();

        public JArray Notes { get; set; }
    }
}