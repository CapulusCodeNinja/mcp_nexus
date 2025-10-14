using mcp_nexus.Utilities;
using mcp_nexus_tests.Mocks;
using System.IO;
using Xunit;

namespace mcp_nexus_tests.Utilities
{
    /// <summary>
    /// Tests for the CommandPreprocessor utility class using mocked WSL converter.
    /// This makes tests portable and deterministic across all systems.
    /// </summary>
    public class CommandPreprocessorTests
    {
        private readonly ICommandPreprocessor m_CommandPreprocessor;

        public CommandPreprocessorTests()
        {
            // Create command preprocessor with mocked path handler
            var mockWslConverter = new MockWslPathConverter();
            var pathHandler = new PathHandler(mockWslConverter);
            m_CommandPreprocessor = new CommandPreprocessor(pathHandler);
        }

        [Theory]
        [InlineData(".srcpath \"C:\\already\\windows\\path\"",
                    ".srcpath \"C:\\already\\windows\\path\"")]
        [InlineData(".srcpath srv*Q:\\Workbench\\Analyses\\test-dmp_src",
                    ".srcpath srv*Q:\\Workbench\\Analyses\\test-dmp_src")]
        [InlineData(".srcpath srv*C:\\symbols",
                    ".srcpath srv*C:\\symbols")]
        [InlineData("!analyze -v",
                    "!analyze -v")]
        public void PreprocessCommand_WithPathConversion_ConvertsWSLPaths(string input, string expected)
        {
            try
            {
                // Act
                var result = m_CommandPreprocessor.PreprocessCommand(input);

                // Assert
                Assert.Equal(expected, result);
            }
            finally
            {
                CleanupCreatedDirectoriesFromSrcpath(expected);
            }
        }

        [Theory]
        [InlineData("lm")]
        [InlineData(".echo test")]
        [InlineData("k")]
        public void PreprocessCommand_WithNonPathCommands_ReturnsUnchanged(string input)
        {
            // Act
            var result = m_CommandPreprocessor.PreprocessCommand(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void PreprocessCommand_WithEmptyInput_ReturnsUnchanged()
        {
            // Act
            var result = m_CommandPreprocessor.PreprocessCommand("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void PreprocessCommand_WithWhitespaceInput_ReturnsUnchanged()
        {
            // Act
            var result = m_CommandPreprocessor.PreprocessCommand("   ");

            // Assert
            Assert.Equal("   ", result);
        }

        [Fact]
        public void PreprocessCommand_WithNullInput_ReturnsNull()
        {
            // Act
            var result = m_CommandPreprocessor.PreprocessCommand(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void PreprocessCommand_WithMultipleWSLPaths_ConvertsAll()
        {
            // Arrange
            var input = ".srcpath \"/mnt/c/path1;C:\\path2;/mnt/d/path3\"";

            string result = string.Empty;
            try
            {
                // Act
                result = m_CommandPreprocessor.PreprocessCommand(input);

                // Assert
                Assert.Equal(".srcpath \"C:\\path1;C:\\path2;D:\\path3\"", result);
            }
            finally
            {
                CleanupCreatedDirectoriesFromSrcpath(result);
            }
        }

        [Theory]
        [InlineData(".sympath srv*C:\\symbols;C:\\cache;http://msdl.microsoft.com/download/symbols", ".sympath srv*C:\\symbols;C:\\cache;http://msdl.microsoft.com/download/symbols")]
        [InlineData(".sympath \"srv*;C:\\symcache;C:\\extra\"", ".sympath \"srv*;C:\\symcache;C:\\extra\"")]
        [InlineData(".sympath /mnt/c/symcache;/mnt/d/extra", ".sympath C:\\symcache;D:\\extra")]
        public void PreprocessCommand_Sympath_ConvertsWsl_AndPreservesTokens(string input, string expected)
        {
            // Act
            var result = m_CommandPreprocessor.PreprocessCommand(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void PreprocessCommand_Symfix_CreatesLocalStore_ForWindowsPath()
        {
            var temp = Path.Combine(Path.GetTempPath(), "symfix_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                var input = $".symfix {temp}";
                var result = m_CommandPreprocessor.PreprocessCommand(input);
                Assert.Equal($".symfix {temp}", result);
                Assert.True(Directory.Exists(temp));
            }
            finally
            {
                try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { }
            }
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
                var result = m_CommandPreprocessor.PreprocessCommand(input);

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
                var result = m_CommandPreprocessor.PreprocessCommand(input);

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

        [Theory]
        [InlineData("!homedir C:\\my\\home\\dir", "!homedir C:/my/home/dir")]
        [InlineData("!homedir \"C:\\my\\home\\dir\"", "!homedir \"C:/my/home/dir\"")]
        [InlineData("!homedir /mnt/c/my/home/dir", "!homedir C:/my/home/dir")]
        [InlineData("!homedir /mnt/analysis/test-path", "!homedir C:/analysis/test-path")]
        [InlineData("!homedir /mnt/share/folder", "!homedir C:/share/folder")]
        [InlineData("!homedir \"/mnt/share/folder\"", "!homedir \"C:/share/folder\"")]
        [InlineData("!HOMEDIR C:\\test", "!HOMEDIR C:/test")]
        public void PreprocessCommand_Homedir_ConvertsWslPaths(string input, string expected)
        {
            // Arrange
            string result = string.Empty;

            try
            {
                // Act
                result = m_CommandPreprocessor.PreprocessCommand(input);

                // Assert
                Assert.Equal(expected, result);
            }
            finally
            {
                // Cleanup - extract path from result for cleanup
                CleanupHomedirPath(result);
            }
        }

        [Fact]
        public void PreprocessCommand_Homedir_CreatesDirectoryIfNotExists()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_homedir_" + Guid.NewGuid().ToString("N")[..8]);
            var input = $"!homedir {tempDir}";
            var expectedPath = tempDir.Replace('\\', '/');

            try
            {
                // Ensure directory doesn't exist initially
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }

                // Act
                var result = m_CommandPreprocessor.PreprocessCommand(input);

                // Assert
                Assert.Equal($"!homedir {expectedPath}", result);
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
        public void PreprocessCommand_Homedir_WithExistingDirectory_DoesNotFail()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_homedir_existing_" + Guid.NewGuid().ToString("N")[..8]);
            var input = $"!homedir \"{tempDir}\"";
            var expectedPath = tempDir.Replace('\\', '/');

            try
            {
                // Create directory first
                Directory.CreateDirectory(tempDir);

                // Act
                var result = m_CommandPreprocessor.PreprocessCommand(input);

                // Assert
                Assert.Equal($"!homedir \"{expectedPath}\"", result);
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

        /// <summary>
        /// Best-effort cleanup for any directories that may be created by CommandPreprocessor during tests.
        /// Parses .srcpath arguments and removes non-srv, non-UNC directories.
        /// </summary>
        internal static void CleanupCreatedDirectoriesFromSrcpath(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            if (!command.StartsWith(".srcpath", StringComparison.OrdinalIgnoreCase)) return;

            // Extract argument after .srcpath/.srcpath+
            var firstSpace = command.IndexOf(' ');
            if (firstSpace < 0 || firstSpace + 1 >= command.Length) return;
            var arg = command[(firstSpace + 1)..].Trim().Trim('"');
            var tokens = arg.Split([';'], StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                var path = token.Trim().Trim('"');
                if (path.StartsWith("srv", StringComparison.OrdinalIgnoreCase) || path.StartsWith("\\\\"))
                    continue;
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        /// <summary>
        /// Best-effort cleanup for any directories that may be created by CommandPreprocessor during !homedir tests.
        /// Parses !homedir argument and removes the directory.
        /// </summary>
        internal static void CleanupHomedirPath(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            if (!command.StartsWith("!homedir", StringComparison.OrdinalIgnoreCase)) return;

            // Extract argument after !homedir
            var firstSpace = command.IndexOf(' ');
            if (firstSpace < 0 || firstSpace + 1 >= command.Length) return;
            var path = command[(firstSpace + 1)..].Trim().Trim('"').Trim('\'');

            // Skip UNC paths
            if (path.StartsWith("\\\\")) return;

            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
