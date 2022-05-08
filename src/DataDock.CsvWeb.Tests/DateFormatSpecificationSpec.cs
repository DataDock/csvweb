using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using DataDock.CsvWeb.Metadata;
using FluentAssertions;
using Xunit;

namespace DataDock.CsvWeb.Tests
{
    public class DateFormatSpecificationSpec
    {
        [Theory]
        [InlineData("yyyy-MM-dd", "2015-03-22", true, "2015-03-22")]
        [InlineData("yyyyMMdd", "20150322", true, "2015-03-22")]
        [InlineData("dd-MM-yyyy", "22-03-2015", true, "2015-03-22")]
        [InlineData("d-M-yyyy", "22-3-2015", true, "2015-03-22")]
        [InlineData("MM-dd-yyyy", "03-22-2015", true, "2015-03-22")]
        [InlineData("M-d-yyyy", "3-22-2015", true, "2015-03-22")]
        [InlineData("dd/MM/yyyy", "22/03/2015", true, "2015-03-22")]
        [InlineData("d/M/yyyy", "22/3/2015", true, "2015-03-22")]
        [InlineData("MM/dd/yyyy", "03/22/2015", true, "2015-03-22")]
        [InlineData("M/d/yyyy", "3/22/2015", true, "2015-03-22")]
        [InlineData("dd.MM.yyyy", "22.03.2015", true, "2015-03-22")]
        [InlineData("d.M.yyyy", "22.3.2015", true, "2015-03-22")]
        [InlineData("MM.dd.yyyy", "03.22.2015", true, "2015-03-22")]
        [InlineData("M.d.yyyy", "3.22.2015", true, "2015-03-22")]
        [InlineData("u-MM-dd", "2015-03-22", true, "2015-03-22")]
        public void TestDateValidation(string formatString, string inputString, bool expectValid, string expectNormalized)
        {
            var formatSpec = new DateFormatSpecification(formatString);
            formatSpec.IsValid(inputString).Should().Be(expectValid);
            if (expectValid && expectNormalized != null)
            {
                formatSpec.Normalize(inputString).Should().Be(expectNormalized);
            }
        }
    }
}
