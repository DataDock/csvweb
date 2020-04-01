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
using DataDock.CsvWeb.Metadata;
using DataDock.CsvWeb.Parsing;
using FluentAssertions;
using Xunit;

namespace DataDock.CsvWeb.Tests
{
    public class JsonMetadataParserSpec
    {
        private static TableGroup ParseMetadata(string jsonPath, Uri baseUri)
        {
            var metadataParser = new JsonMetadataParser(new DefaultResolver(), baseUri ?? new Uri("http://localhost/example.json"));
            using var fileStream = File.OpenText(jsonPath);
            return metadataParser.Parse(fileStream);
        }

        private static TableGroup ParseMetadataFromJson(string json, Uri baseUri)
        {
            var metadataParser = new JsonMetadataParser(new DefaultResolver(), baseUri ?? new Uri("http://localhost/example.json"));
            using var reader = new StringReader(json);
            return metadataParser.Parse(reader);
        }

        [Fact]
        public void TestMinimalMetadata()
        {
            var tg = new TableGroup();
            var _ = new Table(tg) { Url = new Uri("http://example.org/countries.csv") };
            var actual = ParseMetadata("data\\valid-table-1.json", null);
            actual.Should().BeEquivalentTo(tg, options => options.Excluding(o => o.SelectedMemberPath.EndsWith(".Parent")));
        }

        [Fact]
        public void TestTableWithVirtualColumn()
        {
            var json = @"{ 
                'url': 'http://example.org/countries.csv',
                'tableSchema': {
                    'columns': [
                        {
                            'virtual': true,
                            'aboutUrl': 'http://example.org/row/{_row}',
                            'propertyUrl': 'http://example.org/p',
                            'valueUrl': 'http://example.org/o'
                        }
                    ]
                }
            }";

            var expected = new TableGroup();
            var t = new Table(expected)
            {
                Url = new Uri("http://example.org/countries.csv")
            };
            t.TableSchema = new Schema(t) {Columns = new List<ColumnDescription>()};
            var c = new ColumnDescription(t.TableSchema);
            t.TableSchema.Columns.Add(c);
            c.Name = "_col.1";
            c.Virtual = true;
            c.AboutUrl = new UriTemplate("http://example.org/row/{_row}");
            c.PropertyUrl = new UriTemplate("http://example.org/p");
            c.ValueUrl = new UriTemplate("http://example.org/o");

            var actual = ParseMetadataFromJson(json, null);
            actual.Should().BeEquivalentTo(expected, options => options.Excluding(o => o.SelectedMemberPath.EndsWith(".Parent")));

        }

        [Fact]
        public void TestLiteralColumnsWithNamesAndDatatypes()
        {
            var expected = new TableGroup();
            var t = new Table(expected) { Url = new Uri("http://example.org/countries.csv") };
            t.TableSchema = new Schema(t) { Columns = new List<ColumnDescription>() };
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

            var actual = ParseMetadata("data\\valid-table-2.json", null);
            actual.Should().BeEquivalentTo(expected, options => options.Excluding(o => o.SelectedMemberPath.EndsWith(".Parent")));
        }

        [Fact]
        public void TestTableWithAboutUrl()
        {
            var expected = new TableGroup();
            var t = new Table(expected) { Url = new Uri("http://example.org/countries.csv") };
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

            var actual = ParseMetadata("data\\valid-table-3.json", null);
            actual.Should().BeEquivalentTo(expected, options => options.Excluding(o => o.SelectedMemberPath.EndsWith(".Parent")));
        }

        [Fact]
        public void TestColumnWithPropertyUrl()
        {
            var expected = new TableGroup();
            var t  = new Table(expected) { Url = new Uri("http://example.org/countries.csv") };
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

            var actual = ParseMetadata("data\\valid-table-6.json", null);
            actual.Should().BeEquivalentTo(expected, options => options.Excluding(o => o.SelectedMemberPath.EndsWith(".Parent")));
        }

        [Fact]
        public void TestColumnWithValueConstraints()
        {
            var expected = new TableGroup();
            var t = new Table(expected) { Url = new Uri("http://example.org/countries.csv") };
            t.TableSchema = new Schema(t)
            {
                AboutUrl = new UriTemplate("http://example.org/countries/{countryCode}"),
                Columns = new List<ColumnDescription>()
            };
            t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
            {
                Name = "cc",
                PropertyUrl = new UriTemplate("http://example.org/countries.csv/def/countryCode")
            });
            t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
            {
                Name = "latitude",
                Datatype = new DatatypeDescription
                {
                    Base = "decimal",
                    Format = new NumericFormatSpecification("#.##"),
                    Constraints =
                        {
                            new ValueConstraint{ConstraintType = ValueConstraintType.Min, NumericThreshold = -90.0},
                            new ValueConstraint{ConstraintType = ValueConstraintType.Max, NumericThreshold = 90.0}
                        }
                }
            });
            t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
            {
                Name = "longitude",
                Datatype = new DatatypeDescription
                {
                    Base = "decimal",
                    Format = new NumericFormatSpecification("#.##"),
                    Constraints =
                        {
                            new ValueConstraint{ConstraintType = ValueConstraintType.MinExclusive, NumericThreshold = -180.0},
                            new ValueConstraint{ConstraintType = ValueConstraintType.MaxExclusive, NumericThreshold = 180.0}
                        }
                }
            });
            t.TableSchema.Columns.Add(new ColumnDescription(t.TableSchema)
            {
                Name = "name"
            });

            var actual = ParseMetadata("data\\valid-table-8.json", null);
            actual.Should().BeEquivalentTo(expected, options => options.Excluding(o => o.SelectedMemberPath.EndsWith(".Parent")));
        }
    }
}
