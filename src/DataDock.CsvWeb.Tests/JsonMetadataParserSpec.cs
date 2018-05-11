using System;
using System.Collections.Generic;
using System.IO;
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using FluentAssertions;
using Xunit;

namespace DataDock.CsvWeb.Tests
{
    public class JsonMetadataParserSpec
    {
        public static IEnumerable<object[]> ValidParserTests
        {
            get
            {
                var ret = new List<object[]>
                {
                    new object[]
                    {
                        "data\\valid-table-1.json", null, new Table {Url = new Uri("http://example.org/countries.csv")}
                    }
                };


                var t = new Table {Url = new Uri("http://example.org/countries.csv")};
                t.TableSchema = new Schema(t) {Columns = new List<ColumnDescription>()};
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema) {Name = "countryCode"});
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "latitude",
                    Datatype = new DatatypeDescription {Base = "decimal"}
                });
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "longitude",
                    Datatype = new DatatypeDescription {Base = "decimal"}
                });
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "name"
                });

                ret.Add(new object[]
                {
                    "data\\valid-table-2.json", null, t
                });


                t = new Table { Url = new Uri("http://example.org/countries.csv") };
                t.TableSchema = new Schema(t)
                {
                    AboutUrl = new UriTemplate("http://example.org/countries.csv/{_row}"),
                    Columns = new List<ColumnDescription>()
                };
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema) { Name = "countryCode" });
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "latitude",
                    Datatype = new DatatypeDescription { Base = "decimal" }
                });
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "longitude",
                    Datatype = new DatatypeDescription { Base = "decimal" }
                });
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "name"
                });

                ret.Add(new object[] {"data\\valid-table-3.json", null, t});

                t = new Table { Url = new Uri("http://example.org/countries.csv") };
                t.TableSchema = new Schema(t)
                {
                    AboutUrl = new UriTemplate("http://example.org/countries/{countryCode}"),
                    Columns = new List<ColumnDescription>()
                };
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "countryCode",
                    PropertyUrl = new UriTemplate("http://example.org/countries.csv/def/countryCode")
                });
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "latitude",
                    Datatype = new DatatypeDescription { Base = "decimal" }
                });
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "longitude",
                    Datatype = new DatatypeDescription { Base = "decimal" }
                });
                t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
                {
                    Name = "name"
                });

                ret.Add(new object[] {"data\\valid-table-6.json", null, t});
                return ret;
            }
        }

        [Theory]
        [MemberData(nameof(ValidParserTests))]
        public void TestParseOfValidMetadataJson(string jsonPath, Uri baseUri, object expectedResult)
        {
            var metadataParser = new JsonMetadataParser(baseUri);
            using (var fileStream = File.OpenText(jsonPath))
            {
                var parsed = metadataParser.Parse(fileStream);
                parsed.Should().BeEquivalentTo(expectedResult, options=> options.Excluding(o=>o.SelectedMemberPath.EndsWith(".Parent")));
            }
        }
    }
}
