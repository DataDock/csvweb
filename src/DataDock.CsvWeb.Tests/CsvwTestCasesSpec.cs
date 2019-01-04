using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom.Css;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Rdf;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query.Inference;
using VDS.RDF.Writing.Formatting;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using Xunit.Sdk;

namespace DataDock.CsvWeb.Tests
{
    public class CsvwTestCasesSpec : IClassFixture<CsvwTestServerFixture>
    {
        private readonly FluentMockServer _server;
        private readonly Uri _baseUri;

        public CsvwTestCasesSpec(CsvwTestServerFixture fixture)
        {
            _server = fixture.Server;
            _baseUri = fixture.BaseUri;
        }

        [Theory]
        [CsvwtManifest("data\\test-suite\\manifest-rdf.ttl")]
        public async void CsvwRdfTests(string testId, CsvwTestDescription test)
        {
            SetupTest(test);
            await RunTestAsync(test);
        }

        /* KA: An example of how to setup and run a single test for debugging purposes */
        [Fact]
        public async void RunSingleTest()
        {
            var manifestGraph = new Graph();
            manifestGraph.LoadFromFile("data\\test-suite\\manifest-rdf.ttl");
            var testReader = new CsvwtManifestReader(manifestGraph);
            var test = testReader.ReadTest(new Uri(manifestGraph.BaseUri, "manifest-rdf#test074"));
            var sw = new Stopwatch();
            sw.Start();
            SetupTest(test);
            sw.Stop();
            Console.WriteLine($"Setup took {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            await RunTestAsync(test);
            sw.Stop();
            Console.WriteLine($"Test run took {sw.ElapsedMilliseconds} ms");
        }
        /* */

        private async Task RunTestAsync(CsvwTestDescription test)
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

        private void SetupTest(CsvwTestDescription test)
        {
            var csvResponse = Response.Create()
                .WithBodyFromFile(test.Action.LocalFilePath)
                .WithHeader("Content-Type", test.Action.LocalFilePath.EndsWith(".json") ? "application/json" : "text/csv")
                .WithStatusCode(200);
            if (!string.IsNullOrEmpty(test.HttpLink))
            {
                csvResponse = csvResponse.WithHeader("Link", test.HttpLink);
            }

            _server
                .Given(Request.Create().WithPath("/" + test.Action.Uri).UsingGet())
                .RespondWith(csvResponse);

            if (test.Result.LocalFilePath != null)
            {
                _server
                    .Given(Request.Create().WithPath("/" + test.Result.Uri).UsingGet())
                    .RespondWith(
                        Response.Create()
                            .WithBodyFromFile(test.Result.LocalFilePath)
                            .WithHeader("Content-Type", "application/turtle")
                            .WithStatusCode(200)
                    );
            }

            if (test.Implicit != null)
            {
                foreach (var implictFile in test.Implicit)
                {
                    _server.Given(Request.Create().WithPath("/" + implictFile.Uri).UsingGet())
                        .RespondWith(
                            Response.Create()
                                .WithBodyFromFile(implictFile.LocalFilePath)
                                .WithHeader("Content-Type", "application/csvm+json")
                                .WithStatusCode(200));
                }
            }
        }

        private async Task RunToRdfTest(CsvwTestDescription test, bool expectWarnings)
        {
            var expect = new Graph();
            expect.LoadFromUri(new Uri(_baseUri, test.Result.Uri));

            var actual = new Graph();
            var insertHandler = new GraphHandler(actual);
            var errorMessages = new List<string>();
            
            // Set up converter
            var converter = new Converter(
                insertHandler, 
                new DefaultResolver(), 
                test.Options.Minimal ? ConverterMode.Minimal : ConverterMode.Standard, 
                errorMessage => errorMessages.Add(errorMessage), 
                suppressStringDatatype:true);
            if (test.Options.Metadata != null)
            {
                var localMetadata = File.ReadAllText(test.Options.Metadata.LocalPath);
                await converter.ConvertWithLocalMetadata(new Uri(_baseUri, test.Action.Uri), new HttpClient(), localMetadata);
            }
            else
            {
                await converter.ConvertAsync(new Uri(_baseUri, test.Action.Uri), new HttpClient());
            }

            //var tableGroup = new TableGroup();
            //var table = new Table(tableGroup) {Url = new Uri(_baseUri, test.Action.Uri)};
            //await converter.ConvertAsync(tableGroup, new DefaultResolver());

            converter.Errors.Should().BeEmpty("Expected no errors during conversion. Got:\n\t" +
                                              string.Join("\n\t", converter.Errors));
            var differ = new GraphDiff();
            NormalizeLiterals(expect);
            NormalizeLiterals(actual);
            var graphDiff = differ.Difference(expect, actual);
           
            // Assert graphs are equal
            //actual.Triples.Count.Should().Be(expect.Triples.Count,
            //    "Count of triples in output graph should match the count of triples in the expected result graph");
            //Assert.Equal(expect.Triples.Count, actual.Triples.Count);
            Assert.True(graphDiff.AreEqual, "Expected graphs to be the same.\n" + ReportGraphDiff(graphDiff));
        }

        private static readonly Uri XsdString = new Uri(XmlSpecsHelper.XmlSchemaDataTypeString);

        private void NormalizeLiterals(IGraph g)
        {
            List<Triple> toNormalize = new List<Triple>();
            foreach (var t in g.Triples)
            {
                if (t.Object is ILiteralNode lit && string.IsNullOrEmpty(lit.Language) && lit.DataType == null)
                {
                    toNormalize.Add(t);
                }
            }

            foreach (var t in toNormalize)
            {
                var lit = t.Object as ILiteralNode;
                g.Retract(t);
                g.Assert(new Triple(t.Subject, t.Predicate, g.CreateLiteralNode(lit.Value, XsdString)));
            }
        }

        private async Task RunNegativeRdfTest(CsvwTestDescription test)
        {
            var actual = new Graph();
            var insertHandler = new GraphHandler(actual);
            var errorMessages = new List<string>();

            // Set up converter
            var converter = new Converter(
                insertHandler,
                new DefaultResolver(),
                test.Options.Minimal ? ConverterMode.Minimal : ConverterMode.Standard,
                errorMessage => errorMessages.Add(errorMessage),
                suppressStringDatatype: true);
            if (test.Options.Metadata != null)
            {
                var localMetadata = File.ReadAllText(test.Options.Metadata.LocalPath);
                await converter.ConvertWithLocalMetadata(new Uri(_baseUri, test.Action.Uri), new HttpClient(), localMetadata);
            }
            else
            {
                await converter.ConvertAsync(new Uri(_baseUri, test.Action.Uri), new HttpClient());
            }

            Assert.NotEmpty(errorMessages);
        }

        private string ReportGraphDiff(GraphDiffReport gd)
        {
            var sb = new StringBuilder();
            var formatter = new UncompressedTurtleFormatter();
            var missingTriples = gd.RemovedTriples.ToList();
            if (missingTriples.Any())
            {
                sb.Append($"{missingTriples.Count} triples expected and not found:\n");
                foreach (var t in missingTriples)
                {
                    sb.Append("\t");
                    sb.AppendLine(t.ToString(formatter));
                }
            }

            var unexpectedTriples = gd.AddedTriples.ToList();
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

    public class CsvwTestServerFixture : IDisposable
    {
        internal FluentMockServer Server { get; private set; }
        internal Uri BaseUri { get; private set; }

        public CsvwTestServerFixture()
        {
            Server = FluentMockServer.Start();
            BaseUri = new Uri("http://localhost:" + Server.Ports[0]);
            Server.Given(Request.Create().WithPath("/manifest-rdf.ttl"))
                .RespondWith(Response.Create().WithBodyFromFile("data\\test-suite\\manifest-rdf.ttl")
                    .WithHeader("Content-Type", "application/turtle")
                    .WithStatusCode(200));
        }

        public void Dispose()
        {
            Server.Stop();
        }
    }

    public class CsvwtManifestReader
    {
        private IGraph _manifestGraph;
        private List<INode> _testNodes;

        public CsvwtManifestReader(IGraph manifestGraph)
        {
            _manifestGraph = manifestGraph;
            _manifestGraph.NamespaceMap.AddNamespace("csvwt", new Uri("http://www.w3.org/2013/csvw/tests/vocab#"));
            _manifestGraph.NamespaceMap.AddNamespace("mf", new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#"));
            _manifestGraph.NamespaceMap.AddNamespace("rdft", new Uri("http://www.w3.org/ns/rdftest#"));
            _manifestGraph.NamespaceMap.AddNamespace("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            var rdfType = _manifestGraph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            var mfManifest = _manifestGraph.CreateUriNode("mf:Manifest");
            foreach (var manifestNode in _manifestGraph.GetTriplesWithPredicateObject(rdfType, mfManifest).Select(t => t.Subject))
            {
                var entriesRoot =
                    _manifestGraph.GetTriplesWithSubjectPredicate(manifestNode, _manifestGraph.CreateUriNode("mf:entries"))
                        .Select(t => t.Object).First();
                _testNodes = _manifestGraph.GetListItems(entriesRoot).ToList();
            }
        }

        public List<CsvwTestDescription> ReadAllTests()
        {
            return _testNodes.OfType<IUriNode>().Select(ReadTestDescription).ToList();
        }

        public CsvwTestDescription ReadTest(Uri testUri)
        {
            var testNode = _manifestGraph.GetUriNode(testUri);// _testNodes.OfType<IUriNode>().Where(x => x.Uri.Equals(testUri)).FirstOrDefault();
            if (testNode == null) throw new Exception("No test definition with URI " + testUri);
            return ReadTestDescription(testNode);
        }

        private CsvwTestDescription ReadTestDescription(IUriNode testNode)
        {
            var mfName = _manifestGraph.CreateUriNode("mf:name");
            var mfAction = _manifestGraph.CreateUriNode("mf:action");
            var mfResult = _manifestGraph.CreateUriNode("mf:result");
            var rdfType = _manifestGraph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            var rdfsComment = _manifestGraph.CreateUriNode("rdfs:comment");
            var rdftApproval = _manifestGraph.CreateUriNode("rdft:approval");
            var rdftApproved = _manifestGraph.CreateUriNode("rdft:Approved");
            var csvtOption = _manifestGraph.CreateUriNode("csvt:option");
            var csvtImplicit = _manifestGraph.CreateUriNode("csvt:implicit");
            var csvtHttpLink = _manifestGraph.CreateUriNode("csvt:httpLink");

            // Don't pass tests that are not approved to the test runner
            var approved = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, rdftApproval)
                .WithObject(rdftApproved).Any();
            if (!approved) return null;

            var type = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, rdfType)
                .Select(t => (t.Object as IUriNode)).FirstOrDefault()?.ToString();
            if (type == null) return null;
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
            var implicitResources = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, csvtImplicit)
                .Select(t => (t.Object as IUriNode)?.Uri).ToList();
            var httpLink = _manifestGraph.GetTriplesWithSubjectPredicate(testNode, csvtHttpLink)
                .Select(t => (t.Object as ILiteralNode)?.Value).FirstOrDefault();

            var relAction = action == null ? null : _manifestGraph.BaseUri.MakeRelativeUri(action);
            var relResult = result == null ? null : _manifestGraph.BaseUri.MakeRelativeUri(result);
            
            var testDescription = new CsvwTestDescription
            {
                Id = testId,
                Name = name,
                Comment = comment,
                TestType = testType,
                Approved = true,
                Options = options,
                Action = new CsvwTestFileDescription
                {
                    Uri = relAction,
                    LocalFilePath = action?.LocalPath
                },
                Result = new CsvwTestFileDescription
                {
                    Uri = relResult,
                    LocalFilePath = result?.LocalPath
                },
                Implicit = new List<CsvwTestFileDescription>(),
                HttpLink = httpLink
            };

            foreach (var implicitResource in implicitResources)
            {
                var relImplicit = implicitResource == null
                    ? null
                    : _manifestGraph.BaseUri.MakeRelativeUri(implicitResource);
                testDescription.Implicit.Add(new CsvwTestFileDescription
                {
                    Uri = relImplicit,
                    LocalFilePath = implicitResource?.LocalPath
                });
            }

            return testDescription;
        }

        private CsvwOptions GetOptions(INode optionsNode)
        {
            var ret = new CsvwOptions();
            if (optionsNode != null)
            {
                var noProv = _manifestGraph
                    .GetTriplesWithSubjectPredicate(optionsNode, _manifestGraph.CreateUriNode("csvt:noProv"))
                    .Select(t => t.Object.AsValuedNode().AsBoolean()).FirstOrDefault();
                var metadata = _manifestGraph
                    .GetTriplesWithSubjectPredicate(optionsNode, _manifestGraph.CreateUriNode("csvt:metadata"))
                    .Select(t => t.Object).OfType<IUriNode>().FirstOrDefault();
                var minimal = _manifestGraph
                    .GetTriplesWithSubjectPredicate(optionsNode, _manifestGraph.CreateUriNode("csvt:minimal"))
                    .Select(t => t.Object.AsValuedNode().AsBoolean()).FirstOrDefault();
                return new CsvwOptions
                {
                    NoProv = noProv,
                    Metadata = metadata?.Uri,
                    Minimal = minimal
                };
            }

            return ret;
        }

    }

    public class CsvwtManifestAttribute : DataAttribute
    {
        private readonly IGraph _manifestGraph;
        private readonly List<INode> _testNodes;

        public CsvwtManifestAttribute(string manifestFilePath, params string[] skipTests)
        {
            _manifestGraph = new Graph();
            _manifestGraph.LoadFromFile(manifestFilePath);
            _manifestGraph.NamespaceMap.AddNamespace("csvwt", new Uri("http://www.w3.org/2013/csvw/tests/vocab#"));
            _manifestGraph.NamespaceMap.AddNamespace("mf", new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#"));
            _manifestGraph.NamespaceMap.AddNamespace("rdft", new Uri("http://www.w3.org/ns/rdftest#"));
            _manifestGraph.NamespaceMap.AddNamespace("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            var rdfType = _manifestGraph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            var mfManifest = _manifestGraph.CreateUriNode("mf:Manifest");

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
            var manifestReader = new CsvwtManifestReader(_manifestGraph);
            foreach (var test in manifestReader.ReadAllTests())
            {
                yield return new object[] {test.Id, test};
            }
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
        public CsvwTestFileDescription Action;
        public CsvwTestFileDescription Result;
        public List<CsvwTestFileDescription> Implicit;
        public string HttpLink;
    }

    public class CsvwTestFileDescription
    {
        public Uri Uri;
        public string LocalFilePath;
    }

    public class CsvwOptions
    {
        public bool NoProv;
        public Uri Metadata;
        public bool Minimal;
    }

    public enum CsvwTestType
    {
        NegativeRdfTest,
        ToRdfTest,
        ToRdfTestWithWarnings
    }
}
