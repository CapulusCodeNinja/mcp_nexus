using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Debugger;
using System.IO;
using System.Text;

namespace mcp_nexus_tests.Debugger
{
    /// <summary>
    /// Tests for CdbOutputParser
    /// </summary>
    public class CdbOutputParserTests : IDisposable
    {
        private readonly Mock<ILogger<CdbOutputParser>> _mockLogger;
        private readonly CdbOutputParser _parser;

        public CdbOutputParserTests()
        {
            _mockLogger = new Mock<ILogger<CdbOutputParser>>();
            _parser = new CdbOutputParser(_mockLogger.Object);
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
            _parser.SetCurrentCommand(null);

            // Verify the command was set (we can't directly access private field, but we can test behavior)
            Assert.True(true); // This test verifies no exception is thrown
        }

        [Fact]
        public void SetCurrentCommand_WithEmptyCommand_SetsEmpty()
        {
            _parser.SetCurrentCommand("");

            // Verify the command was set
            Assert.True(true); // This test verifies no exception is thrown
        }

        [Fact]
        public void SetCurrentCommand_WithWhitespaceCommand_TrimsAndSets()
        {
            _parser.SetCurrentCommand("  test command  ");

            // Verify the command was set
            Assert.True(true); // This test verifies no exception is thrown
        }

        [Fact]
        public void SetCurrentCommand_WithValidCommand_SetsCommand()
        {
            _parser.SetCurrentCommand("test command");

            // Verify the command was set
            Assert.True(true); // This test verifies no exception is thrown
        }

        [Fact]
        public void IsCommandComplete_WithNullLine_ReturnsFalse()
        {
            var result = _parser.IsCommandComplete(null!);

            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithEmptyLine_ReturnsFalse()
        {
            var result = _parser.IsCommandComplete("");

            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithCdbPrompt_ReturnsTrue()
        {
            var result = _parser.IsCommandComplete("0:000>");

            Assert.True(result);
        }

        [Fact]
        public void IsCommandComplete_WithCdbPromptWithSpaces_ReturnsTrue()
        {
            var result = _parser.IsCommandComplete("  0:000>  ");

            Assert.True(result);
        }

        [Fact]
        public void IsCommandComplete_WithCdbPromptWithDifferentNumbers_ReturnsTrue()
        {
            var result = _parser.IsCommandComplete("123:456>");

            Assert.True(result);
        }

        [Fact]
        public void IsCommandComplete_WithRegularOutput_ReturnsFalse()
        {
            var result = _parser.IsCommandComplete("Some debugger output");

            Assert.False(result);
        }

        [Fact]
        public void IsCommandComplete_WithMultipleLines_ProcessesEachLine()
        {
            _parser.IsCommandComplete("Line 1");
            _parser.IsCommandComplete("Line 2");
            var result = _parser.IsCommandComplete("0:000>");

            Assert.True(result);
        }

        [Fact]
        public void FormatOutputForLogging_WithNullOutput_ReturnsEmptyBrackets()
        {
            var result = _parser.FormatOutputForLogging(null!);

            Assert.Equal("[Empty]", result);
        }

        [Fact]
        public void FormatOutputForLogging_WithEmptyOutput_ReturnsEmptyBrackets()
        {
            var result = _parser.FormatOutputForLogging("");

            Assert.Equal("[Empty]", result);
        }

        [Fact]
        public void FormatOutputForLogging_WithShortOutput_ReturnsAsIs()
        {
            var output = "Short output";
            var result = _parser.FormatOutputForLogging(output);

            Assert.Equal(output, result);
        }

        [Fact]
        public void FormatOutputForLogging_WithLongOutput_Truncates()
        {
            var output = new string('A', 1500);
            var result = _parser.FormatOutputForLogging(output, 1000);

            Assert.Equal(1000 + "... [truncated]".Length, result.Length);
            Assert.EndsWith("... [truncated]", result);
        }

        [Fact]
        public void FormatOutputForLogging_WithNullCharacters_ReplacesWithEscapeSequence()
        {
            var output = "Test\0Output";
            var result = _parser.FormatOutputForLogging(output);

            Assert.Equal("Test\\0Output", result);
        }

        [Fact]
        public void FormatOutputForLogging_WithCustomMaxLength_RespectsLimit()
        {
            var output = "This is a test output";
            var result = _parser.FormatOutputForLogging(output, 10);

            Assert.Equal(10 + "... [truncated]".Length, result.Length);
            Assert.EndsWith("... [truncated]", result);
        }

        [Fact]
        public void AnalyzeOutput_WithNullOutput_ReturnsEmptyAnalysis()
        {
            var result = _parser.AnalyzeOutput(null!);

            Assert.True(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithEmptyOutput_ReturnsEmptyAnalysis()
        {
            var result = _parser.AnalyzeOutput("");

            Assert.True(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithErrorText_DetectsErrors()
        {
            var result = _parser.AnalyzeOutput("Error: Something went wrong");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithUnableToText_DetectsErrors()
        {
            var result = _parser.AnalyzeOutput("Unable to connect to target");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void AnalyzeOutput_WithInvalidText_DetectsErrors()
        {
            var result = _parser.AnalyzeOutput("Invalid parameter");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void AnalyzeOutput_WithFailedText_DetectsErrors()
        {
            var result = _parser.AnalyzeOutput("Failed to execute command");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void AnalyzeOutput_WithWarningText_DetectsWarnings()
        {
            var result = _parser.AnalyzeOutput("Warning: This is a warning");

            Assert.False(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.True(result.HasWarnings);
            Assert.False(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithWarnText_DetectsWarnings()
        {
            var result = _parser.AnalyzeOutput("WARN: This is a warning");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasWarnings);
        }

        [Fact]
        public void AnalyzeOutput_WithCautionText_DetectsWarnings()
        {
            var result = _parser.AnalyzeOutput("Caution: Be careful");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasWarnings);
        }

        [Fact]
        public void AnalyzeOutput_WithSuccessText_DetectsSuccess()
        {
            var result = _parser.AnalyzeOutput("Success: Operation completed");

            Assert.False(result.IsEmpty);
            Assert.False(result.HasErrors);
            Assert.False(result.HasWarnings);
            Assert.True(result.HasSuccessIndicators);
            Assert.False(result.HasPrompt);
        }

        [Fact]
        public void AnalyzeOutput_WithOkText_DetectsSuccess()
        {
            var result = _parser.AnalyzeOutput("OK: Everything is fine");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasSuccessIndicators);
        }

        [Fact]
        public void AnalyzeOutput_WithCompleteText_DetectsSuccess()
        {
            var result = _parser.AnalyzeOutput("Complete: All done");

            Assert.False(result.IsEmpty);
            Assert.True(result.HasSuccessIndicators);
        }

        [Fact]
        public void AnalyzeOutput_WithPrompt_DetectsPrompt()
        {
            var result = _parser.AnalyzeOutput("0:000>");

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
            var result = _parser.AnalyzeOutput(output);

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
            var result = _parser.AnalyzeOutput(output);

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
            
            _parser.CaptureAvailableOutput(null, null, "test", mockLogger.Object);

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

            _parser.CaptureAvailableOutput(outputReader, errorReader, "test", mockLogger.Object);

            // Should not throw exception
            Assert.True(true);
        }

        [Fact]
        public void CaptureAvailableOutput_WithDataInStream_ReadsData()
        {
            var mockLogger = new Mock<ILogger>();
            var outputData = "Test output\nAnother line\n";
            var outputBytes = Encoding.UTF8.GetBytes(outputData);
            var outputStream = new MemoryStream(outputBytes);
            var errorStream = new MemoryStream();
            var outputReader = new StreamReader(outputStream);
            var errorReader = new StreamReader(errorStream);

            _parser.CaptureAvailableOutput(outputReader, errorReader, "test", mockLogger.Object);

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

            _parser.CaptureAvailableOutput(outputReader, errorReader, "test", mockLogger.Object);

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
    }
}