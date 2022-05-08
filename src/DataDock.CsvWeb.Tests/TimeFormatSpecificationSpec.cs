using System;
using DataDock.CsvWeb.Metadata;
using FluentAssertions;
using Xunit;

namespace DataDock.CsvWeb.Tests
{
    public class TimeFormatSpecificationSpec
    {
        [Theory]
        [InlineData("HH:mm:ss.SSS", "15:02:37.143", true, "15:02:37.143")]
        [InlineData("HH:mm:ss", "15:02:37", true, "15:02:37")]
        [InlineData("HH:mm", "15:02", true, "15:02:00")]
        [InlineData("HH:mm:ss.S", "15:02:37.1", true, "15:02:37.1")]
        [InlineData("HHmmss", "150237", true, "15:02:37")]
        [InlineData("HHmm", "1502", true, "15:02:00")]
        [InlineData("HH:mm:ss.SSSX", "15:02:37.143Z", true, "15:02:37.143Z")]
        [InlineData("HH:mm:ss.SSSX", "15:02:37.143-08", true, "15:02:37.143-08")]
        [InlineData("HH:mm:ss.SSSX", "15:02:37.143+0530", true, "15:02:37.143+05:30")]
        [InlineData("HH:mm:ss.SSSXX", "15:02:37.143Z", true, "15:02:37.143Z")]
        [InlineData("HH:mm:ss.SSSXX", "15:02:37.143-08", false, null)]
        [InlineData("HH:mm:ss.SSSXX", "15:02:37.143-0800", true, "15:02:37.143-08")]
        [InlineData("HH:mm:ss.SSSXX", "15:02:37.143+0530", true, "15:02:37.143+05:30")]
        [InlineData("HH:mm:ss.SSSXXX", "15:02:37.143Z", true, "15:02:37.143Z")]
        [InlineData("HH:mm:ss.SSSXXX", "15:02:37.143-08", false, null)]
        [InlineData("HH:mm:ss.SSSXXX", "15:02:37.143-0800", false, null)]
        [InlineData("HH:mm:ss.SSSXXX", "15:02:37.143+0530", false, null)]
        [InlineData("HH:mm:ss.SSSXXX", "15:02:37.143-08:00", true, "15:02:37.143-08")]
        [InlineData("HH:mm:ss.SSSXXX", "15:02:37.143+05:30", true, "15:02:37.143+05:30")]
        [InlineData("HH:mm:ss.SSSx", "15:02:37.143Z", false, null)]
        [InlineData("HH:mm:ss.SSSx", "15:02:37.143+00", true, "15:02:37.143Z")]
        [InlineData("HH:mm:ss.SSSx", "15:02:37.143-08", true, "15:02:37.143-08")]
        [InlineData("HH:mm:ss.SSSx", "15:02:37.143+0530", true, "15:02:37.143+05:30")]
        [InlineData("HH:mm:ss.SSSxx", "15:02:37.143Z", false, null)]
        [InlineData("HH:mm:ss.SSSxx", "15:02:37.143+0000", true, "15:02:37.143Z")]
        [InlineData("HH:mm:ss.SSSxx", "15:02:37.143-08", false, null)]
        [InlineData("HH:mm:ss.SSSxx", "15:02:37.143-0800", true, "15:02:37.143-08")]
        [InlineData("HH:mm:ss.SSSxx", "15:02:37.143+0530", true, "15:02:37.143+05:30")]
        [InlineData("HH:mm:ss.SSSxxx", "15:02:37.143Z", false, null)]
        [InlineData("HH:mm:ss.SSSxxx", "15:02:37.143+00:00", true, "15:02:37.143Z")]
        [InlineData("HH:mm:ss.SSSxxx", "15:02:37.143-08", false, null)]
        [InlineData("HH:mm:ss.SSSxxx", "15:02:37.143-0800", false, null)]
        [InlineData("HH:mm:ss.SSSxxx", "15:02:37.143+0530", false, null)]
        [InlineData("HH:mm:ss.SSSxxx", "15:02:37.143-08:00", true, "15:02:37.143-08")]
        [InlineData("HH:mm:ss.SSSxxx", "15:02:37.143+05:30", true, "15:02:37.143+05:30")]
        public void TestDateTimeValidation(string formatString, string inputString, bool expectValid,
            string expectNormalized)
        {
            var formatSpec = new TimeFormatSpecification(formatString);
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
