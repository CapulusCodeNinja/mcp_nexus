using mcp_nexus.Utilities;
using Xunit;

namespace mcp_nexus_tests.Utilities
{
    /// <summary>
    /// Tests for the CommandPreprocessor utility class.
    /// </summary>
    public class CommandPreprocessorTests
    {
        [Theory]
        [InlineData(".srcpath \"srv\\*/mnt/c/inetpub/wwwroot/workingdir/work_20251006_185410_082\\source\"",
                    ".srcpath \"srv*;C:\\inetpub\\wwwroot\\workingdir\\work_20251006_185410_082\\source\"")]
        [InlineData(".srcpath srv\\*/mnt/c/inetpub/wwwroot/workingdir/work_20251006_185410_082\\source",
                    ".srcpath \"srv*;C:\\inetpub\\wwwroot\\workingdir\\work_20251006_185410_082\\source\"")]
        [InlineData(".srcpath \"srv*;C:\\inetpub\\wwwroot\\workingdir\\Sources\"",
                    ".srcpath \"srv*;C:\\inetpub\\wwwroot\\workingdir\\Sources\"")]
        [InlineData(".srcpath \"srv*;/mnt/c/inetpub/wwwroot/workingdir/Sources\"",
                    ".srcpath \"srv*;C:\\inetpub\\wwwroot\\workingdir\\Sources\"")]
        [InlineData(".srcpath \"C:\\already\\windows\\path\"",
                    ".srcpath \"C:\\already\\windows\\path\"")]
        [InlineData(".srcpath+ \"srv*;C:\\additional\\path\"",
                    ".srcpath+ \"srv*;C:\\additional\\path\"")]
        public void PreprocessCommand_WithSrcPathCommands_FixesSyntaxAndPaths(string input, string expected)
        {
            // Act
            var result = CommandPreprocessor.PreprocessCommand(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("lm")]
        [InlineData("!analyze -v")]
        [InlineData(".echo test")]
        [InlineData("k")]
        public void PreprocessCommand_WithNonSrcPathCommands_ReturnsUnchanged(string input)
        {
            // Act
            var result = CommandPreprocessor.PreprocessCommand(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void PreprocessCommand_WithEmptyInput_ReturnsUnchanged()
        {
            // Act
            var result = CommandPreprocessor.PreprocessCommand("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void PreprocessCommand_WithWhitespaceInput_ReturnsUnchanged()
        {
            // Act
            var result = CommandPreprocessor.PreprocessCommand("   ");

            // Assert
            Assert.Equal("   ", result);
        }

        [Fact]
        public void PreprocessCommand_WithNullInput_ReturnsNull()
        {
            // Act
            var result = CommandPreprocessor.PreprocessCommand(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void PreprocessCommand_WithMultiplePaths_FixesAllPaths()
        {
            // Arrange
            var input = ".srcpath \"srv\\*;/mnt/c/path1;C:\\path2;/mnt/d/path3\"";

            // Act
            var result = CommandPreprocessor.PreprocessCommand(input);

            // Assert
            Assert.Equal(".srcpath \"srv*;C:\\path1;C:\\path2;D:\\path3\"", result);
        }

        [Fact]
        public void PreprocessCommand_WithUnquotedPaths_AddsQuotes()
        {
            // Arrange
            var input = ".srcpath srv*;C:\\path1;C:\\path2";

            // Act
            var result = CommandPreprocessor.PreprocessCommand(input);

            // Assert
            Assert.Equal(".srcpath \"srv*;C:\\path1;C:\\path2\"", result);
        }

        [Fact]
        public void PreprocessCommand_WithNonExistentDirectory_CreatesDirectory()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_srcpath_" + Guid.NewGuid().ToString("N")[..8]);
            var input = $".srcpath \"srv*;{tempDir}\"";

            try
            {
                // Ensure directory doesn't exist initially
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }

                // Act
                var result = CommandPreprocessor.PreprocessCommand(input);

                // Assert
                Assert.Equal($".srcpath \"srv*;{tempDir}\"", result);
                Assert.True(Directory.Exists(tempDir), "Directory should be created automatically");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void PreprocessCommand_WithExistingDirectory_DoesNotFail()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_srcpath_existing_" + Guid.NewGuid().ToString("N")[..8]);
            var input = $".srcpath \"srv*;{tempDir}\"";

            try
            {
                // Create directory first
                Directory.CreateDirectory(tempDir);

                // Act
                var result = CommandPreprocessor.PreprocessCommand(input);

                // Assert
                Assert.Equal($".srcpath \"srv*;{tempDir}\"", result);
                Assert.True(Directory.Exists(tempDir), "Directory should still exist");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
