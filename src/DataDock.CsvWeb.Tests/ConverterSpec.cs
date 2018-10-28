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
using System.IO;
using System.Threading.Tasks;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using DataDock.CsvWeb.Rdf;
using FluentAssertions;
using Moq;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using Xunit;

namespace DataDock.CsvWeb.Tests
{
    public class ConverterSpec
    {
        public static IEnumerable<object[]> ValidConversionData = new[]
        {
            new object[] {"data\\valid-table-1.json", "data\\countries.csv", "data\\valid-table-1-out.ttl" },
            new object[] {"data\\valid-table-2.json", "data\\countries.csv", "data\\valid-table-2-out.ttl" },
            new object[] {"data\\valid-table-3.json", "data\\countries.csv", "data\\valid-table-3-out.ttl" },
            new object[] {"data\\valid-table-4.json", "data\\countries.csv", "data\\valid-table-4-out.ttl" },
            new object[] {"data\\valid-table-5.json", "data\\countries.csv", "data\\valid-table-5-out.ttl" },
            new object[] {"data\\valid-table-6.json", "data\\countries.csv", "data\\valid-table-6-out.ttl" },
            new object[] {"data\\valid-table-7.json", "data\\countries.csv", "data\\valid-table-7-out.ttl" },
            new object[] {"data\\valid-table-suppressed-columns.json", "data\\countries.csv", "data\\valid-table-suppressed-columns-out.ttl" },
            new object[] {"data\\empty_column.metadata.json", "data\\empty_column.csv", "data\\empty_column.out.ttl"}, 
            new object[] {"data\\escaping.metadata.json", "data\\escaping.csv", "data\\escaping.out.ttl"}
        };

        [Theory]
        [MemberData(nameof(ValidConversionData))]
        public async void TestValidConversions(string tableMetadataPath, string csvFilePath, string expectedOutputGraphPath)
        {
            var metadataParser = new JsonMetadataParser(new DefaultResolver(), new Uri("http://example.org/metadata.json"));
            TableGroup tableGroup;
            using (var metadataReader = File.OpenText(tableMetadataPath))
            {
                tableGroup = metadataParser.Parse(metadataReader);
            }

            tableGroup.Should().NotBeNull();
            tableGroup.Tables.Should().HaveCount(1);
            var tableMeta = tableGroup.Tables[0];
            tableMeta.Should().NotBeNull(because:"The metadata file should parse as table metadata");
            var outputGraph = new Graph();
            var graphHandler = new GraphHandler(outputGraph);
            var resolverMock = new Mock<ITableResolver>();
            resolverMock.Setup(x => x.ResolveAsync(It.IsAny<Uri>())).Returns(Task.FromResult(File.OpenRead(csvFilePath) as Stream));
            var converter = new Converter(graphHandler, resolverMock.Object, ConverterMode.Minimal);
            await converter.ConvertAsync(tableMeta.Parent as TableGroup);
            converter.Errors.Count.Should().Be(0, "Expected 0 errors. Got {0}. Error listing is:\n{1}", converter.Errors.Count, string.Join("\n", converter.Errors));
            var turtleParser = new TurtleParser(TurtleSyntax.W3C);
            var expectGraph = new Graph();
            turtleParser.Load(expectGraph, expectedOutputGraphPath);

            var diff = new GraphDiff();
            var diffReport = diff.Difference(expectGraph, outputGraph);
            diffReport.AreEqual.Should().BeTrue();
        }
    }
}
