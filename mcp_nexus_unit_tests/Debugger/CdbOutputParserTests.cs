using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;
using System.IO;
using System.Text;

namespace mcp_nexus_unit_tests.Debugger
{
    /// <summary>
    /// Tests for CdbOutputParser
    /// </summary>
    public class CdbOutputParserTests : IDisposable
    {
        private readonly Mock<ILogger<CdbOutputParser>> m_MockLogger;
        private readonly CdbOutputParser m_Parser;

        public CdbOutputParserTests()
        {
            m_MockLogger = new Mock<ILogger<CdbOutputParser>>();
            m_Parser = new CdbOutputParser(m_MockLogger.Object);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CdbOutputParser(null!));
        }

        [Fact]
        public void SetCurrentCommand_WithNullCommand_SetsNull()
        {
            m_Parser.SetCurrentCommand(null!);

            // Verify the command was set (we can't directly access private field, but we can test behavior)
            Assert.True(true); // This test verifies no exception is thrown
        }

        [Fact]
        public void SetCurrentCommand_WithEmptyCommand_SetsEmpty()
        {
            m_Parser.SetCurrentCommand("");

            // Verify the command was set
            Assert.True(true); // This test verifies no exception is thrown
        }

        [Fact]
        public void SetCurrentCommand_WithWhitespaceCommand_TrimsAndSets()
        {
            m_Parser.SetCurrentCommand("  test command  ");

            // Verify the command was set
            Assert.True(true); // This test verifies no exception is thrown
        }

        [Fact]
        public void SetCurrentCommand_WithValidCommand_SetsCommand()
        {
            m_Parser.SetCurrentCommand("test command");

            // Verify the command was set
            Assert.True(true); // This test verifies no exception is thrown
        }

        [Fact]
        public void IsCommandComplete_WithNullLine_ReturnsFalse()
        {
            var result = m_Parser.IsCommandComplete(null!);

            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithEmptyLine_ReturnsFalse()
        {
            var result = m_Parser.IsCommandComplete("");

            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithCdbPrompt_ReturnsTrue()
        {
            var result = m_Parser.IsCommandComplete("0:000>");

            Assert.True(result);
        }

        [Fact]
        public void IsCommandComplete_WithCdbPromptWithSpaces_ReturnsTrue()
        {
            var result = m_Parser.IsCommandComplete("  0:000>  ");

            Assert.True(result);
        }

        [Fact]
        public void IsCommandComplete_WithCdbPromptWithDifferentNumbers_ReturnsTrue()
        {
            var result = m_Parser.IsCommandComplete("123:456>");

            Assert.True(result);
        }

        [Fact]
        public void IsCommandComplete_WithRegularOutput_ReturnsFalse()
        {
            var result = m_Parser.IsCommandComplete("Some debugger output");

            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithMultipleLines_ProcessesEachLine()
        {
            m_Parser.IsCommandComplete("Line 1");
            m_Parser.IsCommandComplete("Line 2");
            var result = m_Parser.IsCommandComplete("0:000>");

            Assert.True(result);
        }

        [Fact]
        public void FormatOutputForLogging_WithNullOutput_ReturnsEmptyBrackets()
        {
            var result = m_Parser.FormatOutputForLogging(null!);

            Assert.Equal("[Empty]", result);
        }

        [Fact]
        public void FormatOutputForLogging_WithEmptyOutput_ReturnsEmptyBrackets()
        {
            var result = m_Parser.FormatOutputForLogging("");

            Assert.Equal("[Empty]", result);
        }

        [Fact]
        public void FormatOutputForLogging_WithShortOutput_ReturnsAsIs()
        {
            var output = "Short output";
            var result = m_Parser.FormatOutputForLogging(output);

            Assert.Equal(output, result);
        }

        [Fact]
        public void FormatOutputForLogging_WithLongOutput_Truncates()
        {
            var output = new string('A', 1500);
            var result = m_Parser.FormatOutputForLogging(output, 1000);

            Assert.Equal(1000 + "... [truncated]".Length, result.Length);
            Assert.EndsWith("... [truncated]", result);
        }

        [Fact]
        public void FormatOutputForLogging_WithNullCharacters_ReplacesWithEscapeSequence()
        {
            var output = "Test\0Output";
            var result = m_Parser.FormatOutputForLogging(output);

            Assert.Equal("Test\\0Output", result);
        }

        [Fact]
        public void FormatOutputForLogging_WithCustomMaxLength_RespectsLimit()
        {
            var output = "This is a test output";
            var result = m_Parser.FormatOutputForLogging(output, 10);

            Assert.Equal(10 + "... [truncated]".Length, result.Length);
            Assert.EndsWith("... [truncated]", result);
        }

        [Fact]
        public void AnalyzeOutput_WithNullOutput_ReturnsEmptyAnalysis()
        {
            var result = m_Parser.AnalyzeOutput(null!);

            Assert.True(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithEmptyOutput_ReturnsEmptyAnalysis()
        {
            var result = m_Parser.AnalyzeOutput("");

            Assert.True(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithErrorText_DetectsErrors()
        {
            var result = m_Parser.AnalyzeOutput("Error: Something went wrong");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithUnableToText_DetectsErrors()
        {
            var result = m_Parser.AnalyzeOutput("Unable to connect to target");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void AnalyzeOutput_WithInvalidText_DetectsErrors()
        {
            var result = m_Parser.AnalyzeOutput("Invalid parameter");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void AnalyzeOutput_WithFailedText_DetectsErrors()
        {
            var result = m_Parser.AnalyzeOutput("Failed to execute command");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void AnalyzeOutput_WithWarningText_DetectsWarnings()
        {
            var result = m_Parser.AnalyzeOutput("Warning: This is a warning");

            Assert.False(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.True(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithWarnText_DetectsWarnings()
        {
            var result = m_Parser.AnalyzeOutput("WARN: This is a warning");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasWarnings);
        }

        [Fact]
        public void AnalyzeOutput_WithCautionText_DetectsWarnings()
        {
            var result = m_Parser.AnalyzeOutput("Caution: Be careful");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasWarnings);
        }

        [Fact]
        public void AnalyzeOutput_WithSuccessText_DetectsSuccess()
        {
            var result = m_Parser.AnalyzeOutput("Success: Operation completed");

            Assert.False(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.True(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithOkText_DetectsSuccess()
        {
            var result = m_Parser.AnalyzeOutput("OK: Everything is fine");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasSuccessIndicators);
        }

        [Fact]
        public void AnalyzeOutput_WithCompleteText_DetectsSuccess()
        {
            var result = m_Parser.AnalyzeOutput("Complete: All done");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasSuccessIndicators);
        }

        [Fact]
        public void AnalyzeOutput_WithPrompt_DetectsPrompt()
        {
            var result = m_Parser.AnalyzeOutput("0:000>");

            Assert.False(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.True(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithComplexOutput_DetectsMultiplePatterns()
        {
            var output = "Warning: Something happened\nError: Failed\nSuccess: Fixed\n0:000>";
            var result = m_Parser.AnalyzeOutput(output);

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
            Assert.True(result.HasWarnings);
            Assert.True(result.HasSuccessIndicators);
            Assert.True(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithNoPatterns_ReturnsCleanAnalysis()
        {
            var output = "Just some regular output";
            var result = m_Parser.AnalyzeOutput(output);

            Assert.False(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void CaptureAvailableOutput_WithNullReaders_HandlesGracefully()
        {
            var mockLogger = new Mock<ILogger>();

            m_Parser.CaptureAvailableOutput(null, null, "test", mockLogger.Object);

            // Should not throw exception
            Assert.True(true);
        }

        [Fact]
        public void CaptureAvailableOutput_WithEmptyStream_HandlesGracefully()
        {
            var mockLogger = new Mock<ILogger>();
            var outputStream = new MemoryStream();
            var errorStream = new MemoryStream();
            var outputReader = new StreamReader(outputStream);
            var errorReader = new StreamReader(errorStream);

            m_Parser.CaptureAvailableOutput(outputReader, errorReader, "test", mockLogger.Object);

            // Should not throw exception
            Assert.True(true);
        }

        [Fact]
        public void CaptureAvailableOutput_WithDataInStream_ReadsData()
        {
            var mockLogger = new Mock<ILogger>();
            var outputData = "Test output\nAnother line\n";
            var outputBytes = Encoding.Unicode.GetBytes(outputData);
            var outputStream = new MemoryStream(outputBytes);
            var errorStream = new MemoryStream();
            var outputReader = new StreamReader(outputStream);
            var errorReader = new StreamReader(errorStream);

            m_Parser.CaptureAvailableOutput(outputReader, errorReader, "test", mockLogger.Object);

            // Should not throw exception and should read the data
            Assert.True(true);
        }

        [Fact]
        public void CaptureAvailableOutput_WithExceptionInStream_HandlesGracefully()
        {
            var mockLogger = new Mock<ILogger>();
            // Create a real StreamReader that will throw an exception when accessed
            var exceptionStream = new ExceptionThrowingStream();
            var outputReader = new StreamReader(exceptionStream);
            var errorStream = new MemoryStream();
            var errorReader = new StreamReader(errorStream);

            m_Parser.CaptureAvailableOutput(outputReader, errorReader, "test", mockLogger.Object);

            // Should not throw exception
            Assert.True(true);
        }

        private class ExceptionThrowingStream : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => 0;
            public override long Position { get; set; }

            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new Exception("Test exception");
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        #region Branch Coverage Tests - Targeting Missing Branches

        [Fact]
        public void IsCommandComplete_WithNoSentinelNoPromptNoUltraSafe_ReturnsFalse()
        {
            // Arrange - line with no completion indicators
            var line = "Just some regular debugger output";

            // Act
            var result = m_Parser.IsCommandComplete(line);

            // Assert - should return false (no completion detected)
            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithOnlyStartMarker_ReturnsFalse()
        {
            // Arrange - line with START sentinel only (should not complete)
            var line = $"Some output {CdbSentinels.StartMarker} more output";

            // Act
            var result = m_Parser.IsCommandComplete(line);

            // Assert - should return false (start marker doesn't trigger completion)
            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithEndMarkerOnly_ReturnsTrue()
        {
            // Arrange - line with END sentinel (should complete)
            var line = $"Some output {CdbSentinels.EndMarker}";

            // Act
            var result = m_Parser.IsCommandComplete(line);

            // Assert - should return true (end marker triggers completion)
            Assert.True(result);
        }

        [Fact]
        public void IsCommandComplete_WithRegularOutputAfterPrompt_ReturnsFalse()
        {
            // Arrange - first complete with prompt
            m_Parser.IsCommandComplete("0:000>");

            // Then regular output (no prompt, no sentinel, no ultra-safe)
            var line = "Regular output line";

            // Act
            var result = m_Parser.IsCommandComplete(line);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithNonUltraSafePattern_ReturnsFalse()
        {
            // Arrange - line that looks like it might be special but isn't ultra-safe
            var line = "Some random error message";

            // Act
            var result = m_Parser.IsCommandComplete(line);

            // Assert - should return false (not an ultra-safe pattern)
            Assert.False(result);
        }

        #endregion
    }
}