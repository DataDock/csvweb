using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Rdf;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query.Inference;
using VDS.RDF.Writing.Formatting;
using Xunit;
using Xunit.Sdk;

namespace DataDock.CsvWeb.Tests
{
    public class CsvwTestCasesSpec
    {
        [Theory]
        [CsvwtManifest("http://w3c.github.io/csvw/tests/manifest-rdf.ttl")]
        public async void RdfTests(string testId, CsvwTestDescription test)
        {
            switch (test.TestType)
            {
                case CsvwTestType.ToRdfTest:
                    await RunToRdfTest(test, false);
                    break;
                case CsvwTestType.ToRdfTestWithWarnings:
                    await RunToRdfTest(test, true);
                    break;
                case CsvwTestType.NegativeRdfTest:
                    await RunNegativeRdfTest(test);
                    break;
            }
        }

        private async Task RunToRdfTest(CsvwTestDescription test, bool expectWarnings)
        {
            var expect = new Graph();
            expect.LoadFromUri(test.Result);

            var actual = new Graph();
            var insertHandler = new GraphHandler(actual);
            var errorMessages = new List<string>();
            
            // Set up converter
            var converter = new Converter(insertHandler, ConverterMode.Standard, errorMessage => errorMessages.Add(errorMessage), suppressStringDatatype:true);
            var tableGroup = new TableGroup();
            var table = new Table(tableGroup) {Url = test.Action};

            await converter.ConvertAsync(tableGroup, new DefaultResolver());

            var differ = new GraphDiff();
            var graphDiff = differ.Difference(expect, actual);
           
            // Assert graphs are equal
            actual.Triples.Count.Should().Be(expect.Triples.Count,
                "Count of triples in output graph should match the count of triples in the expected result graph");
            Assert.Equal(expect.Triples.Count, actual.Triples.Count);
            Assert.True(graphDiff.AreEqual, "Expected graphs to be the same.\n" + ReportGraphDiff(graphDiff));
        }

        private async Task RunNegativeRdfTest(CsvwTestDescription test) { }

        private string ReportGraphDiff(GraphDiffReport gd)
        {
            var sb = new StringBuilder();
            var formatter = new UncompressedTurtleFormatter();
            var missingTriples = gd.AddedTriples.ToList();
            if (missingTriples.Any())
            {
                sb.Append($"{missingTriples.Count} triples expected and not found:\n");
                foreach (var t in missingTriples)
                {
                    sb.Append("\t");
                    sb.AppendLine(t.ToString(formatter));
                }
            }

            var unexpectedTriples = gd.RemovedTriples.ToList();
            if (unexpectedTriples.Any())
            {
                sb.Append($"{unexpectedTriples.Count} triples found but not expected:\n");
                foreach (var t in unexpectedTriples)
                {
                    sb.Append("\t");
                    sb.AppendLine(t.ToString(formatter));
                }
            }

            if (gd.RemovedMSGs.Any())
            {
                var unexpectedGraphs = gd.RemovedMSGs.ToList();
                sb.Append($"{unexpectedGraphs.Count} subgraphs expected and not found:\n");
                foreach (var t in unexpectedGraphs.SelectMany(g => g.Triples))
                {
                    sb.Append("\t");
                    sb.AppendLine(t.ToString(formatter));
                }
            }

            if (gd.AddedMSGs.Any())
            {
                var missingGraphs = gd.AddedMSGs.ToList();
                sb.Append($"{missingGraphs.Count} subgraphs unexpected:\n");
                foreach (var t in missingGraphs.SelectMany(g=>g.Triples))
                {
                    sb.Append("\t");
                    sb.AppendLine(t.ToString(formatter));
                }
            }

            return sb.ToString();
        }
    }

    public class CsvwtManifestAttribute : DataAttribute
    {
        private readonly IGraph _manifestGraph;
        private readonly List<INode> _testNodes;

        public CsvwtManifestAttribute(string manifestUrl)
        {
            _manifestGraph = new Graph();
            _manifestGraph.LoadFromUri(new Uri(manifestUrl));
            _manifestGraph.NamespaceMap.AddNamespace("csvwt", new Uri("http://www.w3.org/2013/csvw/tests/vocab#"));
            _manifestGraph.NamespaceMap.AddNamespace("mf", new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#"));
            _manifestGraph.NamespaceMap.AddNamespace("rdft", new Uri("http://www.w3.org/ns/rdftest#"));
            _manifestGraph.NamespaceMap.AddNamespace("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            var rdfType = _manifestGraph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            var mfManifest = _manifestGraph.CreateUriNode("mf:Manifest");

            //var reasoner = new StaticRdfsReasoner();
            //reasoner.Initialise(_manifestGraph);
            //reasoner.Apply(_manifestGraph);


            foreach (var manifestNode in _manifestGraph.GetTriplesWithPredicateObject(rdfType, mfManifest).Select(t=>t.Subject))
            {
                var entriesRoot =
                    _manifestGraph.GetTriplesWithSubjectPredicate(manifestNode, _manifestGraph.CreateUriNode("mf:entries"))
                        .Select(t => t.Object).First();
                _testNodes = _manifestGraph.GetListItems(entriesRoot).ToList();
            }
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var mfName = _manifestGraph.CreateUriNode("mf:name");
            var mfAction = _manifestGraph.CreateUriNode("mf:action");
            var mfResult = _manifestGraph.CreateUriNode("mf:result");
            var rdfType = _manifestGraph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            var rdfsComment = _manifestGraph.CreateUriNode("rdfs:comment");
            var rdftApproval = _manifestGraph.CreateUriNode("rdft:approval");
            var rdftApproved = _manifestGraph.CreateUriNode("rdft:Approved");
            var csvtOption = _manifestGraph.CreateUriNode("csvt:option");

            foreach (var testNode in _testNodes.OfType<IUriNode>().OrderBy(n=>n.Uri.ToString()))
            {
                // Don't pass tests that are not approved to the test runner
                var approved = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, rdftApproval)
                    .WithObject(rdftApproved).Any();
                if (!approved) continue;

                var type = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, rdfType)
                    .Select(t => (t.Object as IUriNode)).FirstOrDefault()?.ToString();
                if (type == null) continue;
                CsvwTestType testType;
                switch (type)
                {
                    case "http://www.w3.org/2013/csvw/tests/vocab#ToRdfTest":
                        testType = CsvwTestType.ToRdfTest;
                        break;
                    case "http://www.w3.org/2013/csvw/tests/vocab#ToRdfTestWithWarnings":
                        testType = CsvwTestType.ToRdfTestWithWarnings;
                        break;
                    case "http://www.w3.org/2013/csvw/tests/vocab#NegativeRdfTest":
                        testType = CsvwTestType.NegativeRdfTest;
                        break;
                    default:
                        throw new Exception("Unrecognized test type: " + type);
                }

                var testId = testNode.Uri.Fragment;
                var name = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, mfName)
                    .Select(t => t.Object.AsValuedNode().AsString()).FirstOrDefault();
                var comment = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, rdfsComment)
                    .Select(t => t.Object.AsValuedNode().AsString()).FirstOrDefault();
                var optionsNode = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, csvtOption)
                    .Select(t => t.Object).FirstOrDefault();
                var options = GetOptions(optionsNode);
                var action = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, mfAction)
                    .Select(t => (t.Object as IUriNode)?.Uri).FirstOrDefault();
                var result = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, mfResult)
                    .Select(t => (t.Object as IUriNode)?.Uri).FirstOrDefault();

                var testDescription = new CsvwTestDescription
                {
                    Id = testId,
                    Name = name,
                    Comment = comment,
                    TestType = testType,
                    Approved = true,
                    Options = options,
                    Action = action,
                    Result = result
                };
                yield return new object[]{testId, testDescription};
            }
        }

        private CsvwOptions GetOptions(INode optionsNode)
        {
            var ret = new CsvwOptions();
            if (optionsNode != null)
            {
                var noProv = _manifestGraph
                    .GetTriplesWithSubjectPredicate(optionsNode, _manifestGraph.CreateUriNode("csvt:noProv"))
                    .Select(t => t.Object.AsValuedNode().AsBoolean()).FirstOrDefault();
                return new CsvwOptions
                {
                    NoProv = noProv
                };
            }

            return ret;
        }
    }

    public class CsvwTestDescription
    {
        public string Id;
        public string Name;
        public string Comment;
        public CsvwTestType TestType;
        public bool Approved;
        public CsvwOptions Options;
        public Uri Action;
        public Uri Result;
    }

    public class CsvwOptions
    {
        public bool NoProv;
    }

    public enum CsvwTestType
    {
        NegativeRdfTest,
        ToRdfTest,
        ToRdfTestWithWarnings
    }
}
