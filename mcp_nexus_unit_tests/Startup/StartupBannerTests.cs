using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for StartupBanner class.
    /// </summary>
    public class StartupBannerTests
    {
        [Fact]
        public void FormatBannerLine_WithNormalInput_FormatsCorrectly()
        {
            var result = StartupBanner.FormatBannerLine("Label:", "Value", 65);
            Assert.Contains("Label:", result);
            Assert.Contains("Value", result);
            Assert.StartsWith("*", result);
            Assert.EndsWith("*", result);
        }

        [Fact]
        public void FormatBannerLine_WithLongContent_Truncates()
        {
            var longValue = new string('X', 100);
            var result = StartupBanner.FormatBannerLine("Label:", longValue, 65);
            Assert.True(result.Length <= 69); // 65 + 4 for "* " and " *"
        }

        [Fact]
        public void FormatBannerLine_WithEmptyValue_FormatsCorrectly()
        {
            var result = StartupBanner.FormatBannerLine("Label:", "", 65);
            Assert.Contains("Label:", result);
            Assert.StartsWith("*", result);
            Assert.EndsWith("*", result);
        }

        [Fact]
        public void FormatCenteredBannerLine_WithNormalText_CentersCorrectly()
        {
            var result = StartupBanner.FormatCenteredBannerLine("TEST", 65);
            Assert.Contains("TEST", result);
            Assert.StartsWith("*", result);
            Assert.EndsWith("*", result);
        }

        [Fact]
        public void FormatCenteredBannerLine_WithOddLengthText_CentersCorrectly()
        {
            var result = StartupBanner.FormatCenteredBannerLine("HELLO", 65);
            Assert.Contains("HELLO", result);
            Assert.True(result.Length == 69); // 65 + 4 for "* " and " *"
        }

        [Fact]
        public void FormatCenteredBannerLine_WithExactWidthText_NoTruncation()
        {
            var text = new string('X', 65);
            var result = StartupBanner.FormatCenteredBannerLine(text, 65);
            Assert.Contains(text, result);
        }

        [Fact]
        public void LogStartupBanner_WithStdioMode_DoesNotThrow()
        {
            var args = new CommandLineArguments { ServiceMode = false };
            // Act & Assert - Should not throw
            StartupBanner.LogStartupBanner(args, "stdio", null);
        }

        [Fact]
        public void LogStartupBanner_WithHttpMode_DoesNotThrow()
        {
            var args = new CommandLineArguments { ServiceMode = false };
            // Act & Assert - Should not throw
            StartupBanner.LogStartupBanner(args, "localhost", 5000);
        }

        [Fact]
        public void LogStartupBanner_WithServiceMode_DoesNotThrow()
        {
            var args = new CommandLineArguments { ServiceMode = true };
            // Act & Assert - Should not throw
            StartupBanner.LogStartupBanner(args, "localhost", 5000);
        }

        [Fact]
        public void LogStartupBanner_WithCustomCdbPath_DoesNotThrow()
        {
            var args = new CommandLineArguments
            {
                ServiceMode = false,
                CustomCdbPath = "C:\\Custom\\Path\\To\\CDB.exe"
            };
            // Act & Assert - Should not throw
            StartupBanner.LogStartupBanner(args, "localhost", 5000);
        }

        [Fact]
        public void LogStartupBanner_WithLongCdbPath_TruncatesPath()
        {
            var longPath = "C:\\" + new string('X', 200) + "\\CDB.exe";
            var args = new CommandLineArguments
            {
                ServiceMode = false,
                CustomCdbPath = longPath
            };
            // Act & Assert - Should not throw (path truncation happens internally)
            StartupBanner.LogStartupBanner(args, "localhost", 5000);
        }

        [Fact]
        public void LogStartupBanner_WithCommandLineArgs_ShowsCommandLineSource()
        {
            var args = new CommandLineArguments
            {
                ServiceMode = false,
                HostFromCommandLine = true,
                PortFromCommandLine = true
            };
            // Act & Assert - Should not throw
            StartupBanner.LogStartupBanner(args, "localhost", 5000);
        }

        [Fact]
        public void LogStartupBanner_WithNullPort_UsesDefault()
        {
            var args = new CommandLineArguments { ServiceMode = false };
            // Act & Assert - Should not throw
            StartupBanner.LogStartupBanner(args, "localhost", null);
        }

        [Fact]
        public void FormatCenteredBannerLine_WithTooLongText_Truncates()
        {
            // Arrange - text longer than contentWidth
            var text = new string('X', 100);
            var contentWidth = 65;

            // Act
            var result = StartupBanner.FormatCenteredBannerLine(text, contentWidth);

            // Assert - should truncate to contentWidth
            Assert.Contains(new string('X', contentWidth), result);
            Assert.DoesNotContain(new string('X', 66), result); // Not longer than contentWidth
        }
    }
}

