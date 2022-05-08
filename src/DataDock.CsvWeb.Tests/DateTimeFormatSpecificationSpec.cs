using DataDock.CsvWeb.Metadata;
using FluentAssertions;
using System;
using Xunit;

namespace DataDock.CsvWeb.Tests
{
    public class DateTimeFormatSpecificationSpec
    {
        [Theory]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSS", "2015-03-15T15:02:37.143", true, "2015-03-15T15:02:37.143")]
        [InlineData("yyyy-MM-ddTHH:mm:ss", "2015-03-15T15:02:37", true, "2015-03-15T15:02:37")]
        [InlineData("yyyy-MM-ddTHH:mm", "2015-03-15T15:02", true, "2015-03-15T15:02:00")]
        [InlineData("dd-MM-yyyy HH:mm:ss.S", "15-03-2015 15:02:37.1", true, "2015-03-15T15:02:37.1")]
        [InlineData("d/M/yyyy HH:mm:ss", "15/3/2015 15:02:37", true, "2015-03-15T15:02:37")]
        [InlineData("M/d/yyyy HHmmss", "3/15/2015 150237", true, "2015-03-15T15:02:37")]
        [InlineData("dd.MM.yyyy HH:mm", "15.03.2015 15:02",true, "2015-03-15T15:02:00")]
        [InlineData("M.d.yyyy HHmm", "3.15.2015 1502", true, "2015-03-15T15:02:00")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSX", "2015-03-15T15:02:37.143Z", true, "2015-03-15T15:02:37.143Z")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSX", "2015-03-15T15:02:37.143-08", true, "2015-03-15T15:02:37.143-08")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSX", "2015-03-15T15:02:37.143+0530", true, "2015-03-15T15:02:37.143+05:30")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXX", "2015-03-15T15:02:37.143Z", true, "2015-03-15T15:02:37.143Z")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXX", "2015-03-15T15:02:37.143-08", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXX", "2015-03-15T15:02:37.143-0800", true, "2015-03-15T15:02:37.143-08")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXX", "2015-03-15T15:02:37.143+0530", true, "2015-03-15T15:02:37.143+05:30")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSZ", "2015-03-15T15:02:37.143Z", true, "2015-03-15T15:02:37.143Z")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSZ", "2015-03-15T15:02:37.143-08", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSZ", "2015-03-15T15:02:37.143-0800", true, "2015-03-15T15:02:37.143-08")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSZ", "2015-03-15T15:02:37.143+0530", true, "2015-03-15T15:02:37.143+05:30")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXXX", "2015-03-15T15:02:37.143Z", true, "2015-03-15T15:02:37.143Z")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXXX", "2015-03-15T15:02:37.143-08", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXXX", "2015-03-15T15:02:37.143-0800", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXXX", "2015-03-15T15:02:37.143+0530", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXXX", "2015-03-15T15:02:37.143-08:00", true, "2015-03-15T15:02:37.143-08")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSXXX", "2015-03-15T15:02:37.143+05:30", true, "2015-03-15T15:02:37.143+05:30")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSx", "2015-03-15T15:02:37.143Z", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSx", "2015-03-15T15:02:37.143+00", true, "2015-03-15T15:02:37.143Z")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSx", "2015-03-15T15:02:37.143-08", true, "2015-03-15T15:02:37.143-08")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSx", "2015-03-15T15:02:37.143+0530", true, "2015-03-15T15:02:37.143+05:30")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxx", "2015-03-15T15:02:37.143Z", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxx", "2015-03-15T15:02:37.143+0000", true, "2015-03-15T15:02:37.143Z")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxx", "2015-03-15T15:02:37.143-08", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxx", "2015-03-15T15:02:37.143-0800", true, "2015-03-15T15:02:37.143-08")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxx", "2015-03-15T15:02:37.143+0530", true, "2015-03-15T15:02:37.143+05:30")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxxx", "2015-03-15T15:02:37.143Z", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxxx", "2015-03-15T15:02:37.143+00:00", true, "2015-03-15T15:02:37.143Z")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxxx", "2015-03-15T15:02:37.143-08", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxxx", "2015-03-15T15:02:37.143-0800", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxxx", "2015-03-15T15:02:37.143+0530", false, null)]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxxx", "2015-03-15T15:02:37.143-08:00", true, "2015-03-15T15:02:37.143-08")]
        [InlineData("yyyy-MM-ddTHH:mm:ss.SSSxxx", "2015-03-15T15:02:37.143+05:30", true, "2015-03-15T15:02:37.143+05:30")]
        [InlineData("yyyy-M-dTH:m:sZ", "2019-03-16T15:03:23Z", true, "2019-03-16T15:03:23Z")]
        public void TestDateTimeValidation(string formatString, string inputString, bool expectValid,
            string expectNormalized)
        {
            var formatSpec = new DateTimeFormatSpecification(formatString);
            formatSpec.IsValid(inputString).Should().Be(expectValid);
            if (expectValid)
            {
                formatSpec.Normalize(inputString).Should().Be(expectNormalized);
            }
            else
            {
                Assert.ThrowsAny<FormatException>(() => formatSpec.Normalize(inputString));
            }
        }
    }
}
