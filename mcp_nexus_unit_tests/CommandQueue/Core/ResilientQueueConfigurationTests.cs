using Xunit;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    /// <summary>
    /// Tests for ResilientQueueConfiguration
    /// </summary>
    public class ResilientQueueConfigurationTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultParameters_CreatesInstanceWithDefaults()
        {
            // Act
            var config = new ResilientQueueConfiguration();

            // Assert
            Assert.NotNull(config);
            Assert.Equal("unknown", config.SessionId);
            Assert.True(config.DefaultCommandTimeout > TimeSpan.Zero);
            Assert.True(config.ComplexCommandTimeout > TimeSpan.Zero);
            Assert.True(config.MaxCommandTimeout > TimeSpan.Zero);
        }

        [Fact]
        public void Constructor_WithNullSessionId_UsesDefaultSessionId()
        {
            // Act
            var config = new ResilientQueueConfiguration(sessionId: null!);

            // Assert
            Assert.Equal("unknown", config.SessionId);
        }

        [Fact]
        public void Constructor_WithCustomSessionId_UsesProvidedSessionId()
        {
            // Act
            var config = new ResilientQueueConfiguration(sessionId: "custom-session");

            // Assert
            Assert.Equal("custom-session", config.SessionId);
        }

        [Fact]
        public void Constructor_WithCustomTimeouts_UsesProvidedValues()
        {
            // Arrange
            var defaultTimeout = TimeSpan.FromMinutes(5);
            var complexTimeout = TimeSpan.FromMinutes(10);
            var maxTimeout = TimeSpan.FromMinutes(30);

            // Act
            var config = new ResilientQueueConfiguration(
                defaultCommandTimeout: defaultTimeout,
                complexCommandTimeout: complexTimeout,
                maxCommandTimeout: maxTimeout);

            // Assert
            Assert.Equal(defaultTimeout, config.DefaultCommandTimeout);
            Assert.Equal(complexTimeout, config.ComplexCommandTimeout);
            Assert.Equal(maxTimeout, config.MaxCommandTimeout);
        }

        #endregion

        #region DetermineCommandTimeout Tests

        [Fact]
        public void DetermineCommandTimeout_WithNullCommand_ReturnsDefaultTimeout()
        {
            // Arrange
            var config = new ResilientQueueConfiguration();

            // Act
            var timeout = config.DetermineCommandTimeout(null!);

            // Assert
            Assert.Equal(config.DefaultCommandTimeout, timeout);
        }

        [Fact]
        public void DetermineCommandTimeout_WithEmptyCommand_ReturnsDefaultTimeout()
        {
            // Arrange
            var config = new ResilientQueueConfiguration();

            // Act
            var timeout = config.DetermineCommandTimeout("");

            // Assert
            Assert.Equal(config.DefaultCommandTimeout, timeout);
        }

        [Fact]
        public void DetermineCommandTimeout_WithWhitespaceCommand_ReturnsDefaultTimeout()
        {
            // Arrange
            var config = new ResilientQueueConfiguration();

            // Act
            var timeout = config.DetermineCommandTimeout("   ");

            // Assert
            Assert.Equal(config.DefaultCommandTimeout, timeout);
        }

        [Theory]
        [InlineData("!analyze -v")]
        [InlineData("!heap")]
        [InlineData("!poolused")]
        [InlineData("!verifier")]
        [InlineData("!locks")]
        [InlineData("!deadlock")]
        public void DetermineCommandTimeout_WithLongRunningCommands_ReturnsMaxTimeout(string command)
        {
            // Arrange
            var config = new ResilientQueueConfiguration();

            // Act
            var timeout = config.DetermineCommandTimeout(command);

            // Assert
            Assert.Equal(config.MaxCommandTimeout, timeout);
        }

        [Theory]
        [InlineData("!stack")]
        [InlineData("!clrstack")]
        [InlineData("!dumpheap")]
        [InlineData("!gcroot")]
        [InlineData("!syncblk")]
        [InlineData("!peb")]
        public void DetermineCommandTimeout_WithComplexCommands_ReturnsComplexTimeout(string command)
        {
            // Arrange
            var config = new ResilientQueueConfiguration();

            // Act
            var timeout = config.DetermineCommandTimeout(command);

            // Assert
            Assert.Equal(config.ComplexCommandTimeout, timeout);
        }

        [Theory]
        [InlineData("version")]
        [InlineData("help")]
        [InlineData("r")]
        [InlineData("lm")]
        public void DetermineCommandTimeout_WithSimpleCommands_ReturnsSimpleTimeout(string command)
        {
            // Arrange
            var config = new ResilientQueueConfiguration();

            // Act
            var timeout = config.DetermineCommandTimeout(command);

            // Assert
            // Should return simple command timeout (not default, complex, or max)
            Assert.NotEqual(config.DefaultCommandTimeout, timeout);
            Assert.NotEqual(config.ComplexCommandTimeout, timeout);
            Assert.NotEqual(config.MaxCommandTimeout, timeout);
        }

        #endregion

        #region GenerateHeartbeatDetails Tests

        [Fact]
        public void GenerateHeartbeatDetails_WithAnalyzeCommand_LessThan2Minutes_ReturnsInitializingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!analyze -v",
                TimeSpan.FromMinutes(1));

            // Assert
            Assert.Equal("Initializing crash analysis...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithAnalyzeCommand_Between2And5Minutes_ReturnsAnalyzingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!analyze -v",
                TimeSpan.FromMinutes(3));

            // Assert
            Assert.Equal("Analyzing crash dump structure...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithAnalyzeCommand_Between5And10Minutes_ReturnsProcessingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!analyze -v",
                TimeSpan.FromMinutes(7));

            // Assert
            Assert.Equal("Processing stack traces and modules...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithAnalyzeCommand_GreaterThan10Minutes_ReturnsDeepAnalysisMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!analyze -v",
                TimeSpan.FromMinutes(12));

            // Assert
            Assert.Equal("Performing deep analysis (this may take several more minutes)...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithHeapCommand_LessThan30Seconds_ReturnsScanningMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!heap",
                TimeSpan.FromSeconds(20));

            // Assert
            Assert.Equal("Scanning heap structures...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithHeapCommand_Between30SecondsAnd2Minutes_ReturnsAnalyzingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!heap",
                TimeSpan.FromMinutes(1));

            // Assert
            Assert.Equal("Analyzing heap allocations...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithHeapCommand_GreaterThan2Minutes_ReturnsProcessingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!heap",
                TimeSpan.FromMinutes(3));

            // Assert
            Assert.Equal("Processing large heap data (please wait)...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithDumpHeapCommand_LessThan15Seconds_ReturnsEnumeratingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!dumpheap",
                TimeSpan.FromSeconds(10));

            // Assert
            Assert.Equal("Enumerating managed objects...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithDumpHeapCommand_Between15SecondsAnd1Minute_ReturnsCollectingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!dumpheap",
                TimeSpan.FromSeconds(45));

            // Assert
            Assert.Equal("Collecting object statistics...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithDumpHeapCommand_GreaterThan1Minute_ReturnsProcessingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "!dumpheap",
                TimeSpan.FromMinutes(2));

            // Assert
            Assert.Equal("Processing large object heap...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithUnknownCommand_LessThan30Seconds_ReturnsExecutingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "some_custom_command",
                TimeSpan.FromSeconds(20));

            // Assert
            Assert.Equal("Executing command...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithUnknownCommand_Between30SecondsAnd2Minutes_ReturnsProcessingMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "some_custom_command",
                TimeSpan.FromMinutes(1));

            // Assert
            Assert.Equal("Processing command (complex operation)...", message);
        }

        [Fact]
        public void GenerateHeartbeatDetails_WithUnknownCommand_GreaterThan2Minutes_ReturnsLongRunningMessage()
        {
            // Act
            var message = ResilientQueueConfiguration.GenerateHeartbeatDetails(
                "some_custom_command",
                TimeSpan.FromMinutes(3));

            // Assert
            Assert.Equal("Long-running operation in progress...", message);
        }

        #endregion

        [Fact]
        public void ResilientQueueConfiguration_Class_Exists()
        {
            // This test verifies that the ResilientQueueConfiguration class exists and can be instantiated
            Assert.NotNull(typeof(ResilientQueueConfiguration));
        }
    }
}

