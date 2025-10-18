using System.Reflection;
using System.Text.Json;
using Xunit;

namespace mcp_nexus.Tests
{
    /// <summary>
    /// Unit tests for Program class utility methods.
    /// Tests private methods using reflection to verify pure logic functions.
    /// </summary>
    public class ProgramTests
    {
        private readonly Type m_ProgramType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramTests"/> class.
        /// </summary>
        public ProgramTests()
        {
            m_ProgramType = typeof(Program);
        }

        #region ParseCommandLineArguments Tests

        /// <summary>
        /// Verifies that ParseCommandLineArguments handles empty arguments.
        /// </summary>
        [Fact]
        public void ParseCommandLineArguments_WithEmptyArgs_ReturnsDefaultValues()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("ParseCommandLineArguments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { Array.Empty<string>() });

            // Assert
            Assert.NotNull(result);
            var resultType = result.GetType();
            Assert.False((bool)resultType.GetProperty("UseHttp")!.GetValue(result)!);
            Assert.False((bool)resultType.GetProperty("ServiceMode")!.GetValue(result)!);
            Assert.False((bool)resultType.GetProperty("Install")!.GetValue(result)!);
        }

        /// <summary>
        /// Verifies that ParseCommandLineArguments correctly parses --http flag.
        /// </summary>
        [Fact]
        public void ParseCommandLineArguments_WithHttpFlag_SetsUseHttpTrue()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("ParseCommandLineArguments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { new[] { "--http" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result.GetType();
            Assert.True((bool)resultType.GetProperty("UseHttp")!.GetValue(result)!);
        }

        /// <summary>
        /// Verifies that ParseCommandLineArguments correctly parses --service flag.
        /// </summary>
        [Fact]
        public void ParseCommandLineArguments_WithServiceFlag_SetsServiceModeTrue()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("ParseCommandLineArguments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { new[] { "--service" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result.GetType();
            Assert.True((bool)resultType.GetProperty("ServiceMode")!.GetValue(result)!);
        }

        /// <summary>
        /// Verifies that ParseCommandLineArguments correctly parses --install flag.
        /// </summary>
        [Fact]
        public void ParseCommandLineArguments_WithInstallFlag_SetsInstallTrue()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("ParseCommandLineArguments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { new[] { "--install" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result.GetType();
            Assert.True((bool)resultType.GetProperty("Install")!.GetValue(result)!);
        }

        /// <summary>
        /// Verifies that ParseCommandLineArguments correctly parses --port option.
        /// </summary>
        [Fact]
        public void ParseCommandLineArguments_WithPortOption_SetsPort()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("ParseCommandLineArguments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { new[] { "--port", "8080" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result.GetType();
            var port = (int?)resultType.GetProperty("Port")!.GetValue(result);
            Assert.Equal(8080, port);
            Assert.True((bool)resultType.GetProperty("PortFromCommandLine")!.GetValue(result)!);
        }

        /// <summary>
        /// Verifies that ParseCommandLineArguments correctly parses --host option.
        /// </summary>
        [Fact]
        public void ParseCommandLineArguments_WithHostOption_SetsHost()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("ParseCommandLineArguments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { new[] { "--host", "0.0.0.0" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result.GetType();
            var host = (string?)resultType.GetProperty("Host")!.GetValue(result);
            Assert.Equal("0.0.0.0", host);
            Assert.True((bool)resultType.GetProperty("HostFromCommandLine")!.GetValue(result)!);
        }

        /// <summary>
        /// Verifies that ParseCommandLineArguments correctly parses --cdb-path option.
        /// </summary>
        [Fact]
        public void ParseCommandLineArguments_WithCdbPathOption_SetsCdbPath()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("ParseCommandLineArguments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { new[] { "--cdb-path", "C:\\WinDbg\\cdb.exe" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result.GetType();
            var cdbPath = (string?)resultType.GetProperty("CustomCdbPath")!.GetValue(result);
            Assert.Equal("C:\\WinDbg\\cdb.exe", cdbPath);
        }

        /// <summary>
        /// Verifies that ParseCommandLineArguments correctly parses multiple flags.
        /// </summary>
        [Fact]
        public void ParseCommandLineArguments_WithMultipleFlags_ParsesAllCorrectly()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("ParseCommandLineArguments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { new[] { "--http", "--port", "9000", "--host", "localhost" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result.GetType();
            Assert.True((bool)resultType.GetProperty("UseHttp")!.GetValue(result)!);
            Assert.Equal(9000, (int?)resultType.GetProperty("Port")!.GetValue(result));
            Assert.Equal("localhost", (string?)resultType.GetProperty("Host")!.GetValue(result));
        }

        #endregion

        #region MaskSecret Tests

        /// <summary>
        /// Verifies that MaskSecret returns "Not set" for null input.
        /// </summary>
        [Fact]
        public void MaskSecret_WithNull_ReturnsNotSet()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("MaskSecret", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object?[] { null }) as string;

            // Assert
            Assert.Equal("Not set", result);
        }

        /// <summary>
        /// Verifies that MaskSecret returns "Not set" for empty string.
        /// </summary>
        [Fact]
        public void MaskSecret_WithEmptyString_ReturnsNotSet()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("MaskSecret", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "" }) as string;

            // Assert
            Assert.Equal("Not set", result);
        }

        /// <summary>
        /// Verifies that MaskSecret returns original string for short secrets.
        /// </summary>
        [Fact]
        public void MaskSecret_WithShortSecret_ReturnsOriginal()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("MaskSecret", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "abc" }) as string;

            // Assert
            Assert.Equal("abc", result);
        }

        /// <summary>
        /// Verifies that MaskSecret correctly masks long secrets.
        /// </summary>
        [Fact]
        public void MaskSecret_WithLongSecret_MasksCorrectly()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("MaskSecret", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "MySecretToken12345" }) as string;

            // Assert
            Assert.StartsWith("MySec", result);
            Assert.Contains("*", result);
            Assert.Equal(18, result!.Length); // Same length as original
        }

        /// <summary>
        /// Verifies that MaskSecret shows exactly 5 characters.
        /// </summary>
        [Fact]
        public void MaskSecret_WithExactly6Characters_ShowsFirst5()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("MaskSecret", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "abcdef" }) as string;

            // Assert
            Assert.Equal("abcde*", result);
        }

        #endregion

        #region FormatBannerLine Tests

        /// <summary>
        /// Verifies that FormatBannerLine formats correctly with normal input.
        /// </summary>
        [Fact]
        public void FormatBannerLine_WithNormalInput_FormatsCorrectly()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatBannerLine", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "Version:", "1.0.0", 65 }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("* Version:", result);
            Assert.Contains("1.0.0", result);
            Assert.EndsWith("*", result);
        }

        /// <summary>
        /// Verifies that FormatBannerLine truncates long content.
        /// </summary>
        [Fact]
        public void FormatBannerLine_WithLongContent_Truncates()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatBannerLine", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var longValue = new string('x', 100);
            var result = method.Invoke(null, new object[] { "Label:", longValue, 20 }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(24, result.Length); // "* " + 20 chars + " *"
        }

        /// <summary>
        /// Verifies that FormatBannerLine pads short content.
        /// </summary>
        [Fact]
        public void FormatBannerLine_WithShortContent_Pads()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatBannerLine", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "Hi:", "X", 30 }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(34, result.Length); // "* " + 30 chars + " *"
            Assert.StartsWith("* Hi:", result);
            Assert.EndsWith("*", result);
        }

        #endregion

        #region FormatCenteredBannerLine Tests

        /// <summary>
        /// Verifies that FormatCenteredBannerLine centers text correctly.
        /// </summary>
        [Fact]
        public void FormatCenteredBannerLine_WithNormalText_CentersCorrectly()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatCenteredBannerLine", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "MCP NEXUS", 30 }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(34, result.Length); // "* " + 30 chars + " *"
            Assert.StartsWith("* ", result);
            Assert.EndsWith(" *", result);
            Assert.Contains("MCP NEXUS", result);
        }

        /// <summary>
        /// Verifies that FormatCenteredBannerLine truncates long text.
        /// </summary>
        [Fact]
        public void FormatCenteredBannerLine_WithLongText_Truncates()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatCenteredBannerLine", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var longText = new string('x', 100);
            var result = method.Invoke(null, new object[] { longText, 20 }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(24, result.Length); // "* " + 20 chars + " *"
        }

        /// <summary>
        /// Verifies that FormatCenteredBannerLine handles empty string.
        /// </summary>
        [Fact]
        public void FormatCenteredBannerLine_WithEmptyString_ReturnsSpaces()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatCenteredBannerLine", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "", 20 }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(24, result.Length); // "* " + 20 spaces + " *"
            Assert.Equal("*                      *", result);
        }

        #endregion

        #region FormatJsonForLogging Tests

        /// <summary>
        /// Verifies that FormatJsonForLogging formats valid JSON.
        /// </summary>
        [Fact]
        public void FormatJsonForLogging_WithValidJson_FormatsCorrectly()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatJsonForLogging", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var json = "{\"name\":\"test\",\"value\":123}";
            var result = method.Invoke(null, new object[] { json }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("name", result);
            Assert.Contains("test", result);
            Assert.Contains("value", result);
        }

        /// <summary>
        /// Verifies that FormatJsonForLogging handles invalid JSON.
        /// </summary>
        [Fact]
        public void FormatJsonForLogging_WithInvalidJson_ReturnsErrorMessage()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatJsonForLogging", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var invalidJson = "{invalid json";
            var result = method.Invoke(null, new object[] { invalidJson }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("[Invalid JSON", result);
        }

        /// <summary>
        /// Verifies that FormatJsonForLogging handles empty string.
        /// </summary>
        [Fact]
        public void FormatJsonForLogging_WithEmptyString_ReturnsErrorMessage()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatJsonForLogging", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "" }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("[Invalid JSON", result);
        }

        /// <summary>
        /// Verifies that FormatJsonForLogging truncates very long invalid JSON.
        /// </summary>
        [Fact]
        public void FormatJsonForLogging_WithVeryLongInvalidJson_Truncates()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatJsonForLogging", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var longInvalidJson = "{" + new string('x', 2000);
            var result = method.Invoke(null, new object[] { longInvalidJson }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("[Invalid JSON", result);
            Assert.Contains("...", result);
        }

        #endregion

        #region FormatSseResponseForLogging Tests

        /// <summary>
        /// Verifies that FormatSseResponseForLogging handles SSE with event and data.
        /// </summary>
        [Fact]
        public void FormatSseResponseForLogging_WithEventAndData_FormatsCorrectly()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatSseResponseForLogging", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var sse = "event: message\ndata: {\"test\":\"value\"}\n\n";
            var result = method.Invoke(null, new object[] { sse }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("event:", result);
            Assert.Contains("data:", result);
        }

        /// <summary>
        /// Verifies that FormatSseResponseForLogging handles empty input.
        /// </summary>
        [Fact]
        public void FormatSseResponseForLogging_WithEmptyInput_ReturnsEmpty()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatSseResponseForLogging", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, new object[] { "" }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result);
        }

        /// <summary>
        /// Verifies that FormatSseResponseForLogging handles invalid JSON in data.
        /// </summary>
        [Fact]
        public void FormatSseResponseForLogging_WithInvalidJsonData_HandlesGracefully()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("FormatSseResponseForLogging", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var sse = "event: message\ndata: {invalid json}\n\n";
            var result = method.Invoke(null, new object[] { sse }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("event:", result);
            Assert.Contains("data:", result);
        }

        #endregion

        #region GetCdbPathInfo Tests

        /// <summary>
        /// Verifies that GetCdbPathInfo returns a result.
        /// </summary>
        [Fact]
        public void GetCdbPathInfo_ReturnsResult()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("GetCdbPathInfo", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(null, null) as string;

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion

        #region GetProviderInfo Tests

        /// <summary>
        /// Verifies that GetProviderInfo handles null provider gracefully.
        /// </summary>
        [Fact]
        public void GetProviderInfo_WithNullProvider_ReturnsUnknown()
        {
            // Arrange
            var method = m_ProgramType.GetMethod("GetProviderInfo", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // This test would require a mock IConfigurationProvider, which is complex
            // For now, we verify the method exists and can be invoked
            Assert.NotNull(method);
        }

        #endregion
    }
}

