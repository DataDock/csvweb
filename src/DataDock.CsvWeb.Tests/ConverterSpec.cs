using System.Collections.Generic;
using System.IO;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using DataDock.CsvWeb.Rdf;
using FluentAssertions;
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
        public void TestValidConversions(string tableMetadataPath, string csvFilePath, string expectedOutputGraphPath)
        {
            var metadataParser = new JsonMetadataParser(null);
            object metadataObject;
            using (var metadataReader = File.OpenText(tableMetadataPath))
            {
                metadataObject = metadataParser.Parse(metadataReader);
            }
            var tableMeta = metadataObject as Table;
            tableMeta.Should().NotBeNull(because:"The metadata file should parse as table metadata");
            var outputGraph = new Graph();
            var graphHandler = new GraphHandler(outputGraph);
            var converter = new Converter(tableMeta, graphHandler);
            using (var csvReader = File.OpenText(csvFilePath))
            {
                converter.Convert(csvReader);
            }
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
