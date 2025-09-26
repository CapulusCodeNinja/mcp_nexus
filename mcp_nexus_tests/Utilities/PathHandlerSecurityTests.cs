using Xunit;
using mcp_nexus.Utilities;
using System;

namespace mcp_nexus_tests.Utilities
{
    /// <summary>
    /// Tests for PathHandler security validation
    /// </summary>
    public class PathHandlerSecurityTests
    {
        [Theory]
        [InlineData("/mnt/c/../etc/passwd")]
        [InlineData("C:\\temp\\..\\..\\windows\\system32")]
        [InlineData("/mnt/c/temp~backup/file.dmp")]
        [InlineData("/mnt/c/temp\0injection/file.dmp")]
        [InlineData("/mnt/c/file%USERPROFILE%/file.dmp")]
        [InlineData("/mnt/c/file$HOME/file.dmp")]
        public void ConvertToWindowsPath_WithDangerousPath_ThrowsArgumentException(string dangerousPath)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PathHandler.ConvertToWindowsPath(dangerousPath));
            Assert.Contains("dangerous pattern", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("\\\\malicious-server\\share\\file.dmp")]
        [InlineData("//malicious-server/share/file.dmp")]
        public void ConvertToWindowsPath_WithUNCPath_ThrowsArgumentException(string uncPath)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PathHandler.ConvertToWindowsPath(uncPath));
            Assert.Contains("UNC paths are not allowed", exception.Message);
        }

        [Theory]
        [InlineData("/mnt/c/temp/dump.dmp")]
        [InlineData("/mnt/d/symbols/file.pdb")]
        [InlineData("C:\\temp\\dump.dmp")]
        [InlineData("/valid/unix/path")]
        public void ConvertToWindowsPath_WithValidPath_ReturnsExpectedResult(string validPath)
        {
            // Act & Assert - Should not throw
            var result = PathHandler.ConvertToWindowsPath(validPath);
            Assert.NotNull(result);
        }

        [Fact]
        public void ConvertToWindowsPath_WithExcessivelyLongPath_ThrowsArgumentException()
        {
            // Arrange - Create a path longer than MAX_PATH (260 characters)
            var longPath = "/mnt/c/" + new string('a', 300);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PathHandler.ConvertToWindowsPath(longPath));
            Assert.Contains("exceeds maximum allowed length", exception.Message);
        }

        [Theory]
        [InlineData("/mnt/c/file\x01control")]
        public void ConvertToWindowsPath_WithControlCharacters_ThrowsArgumentException(string pathWithControlChars)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PathHandler.ConvertToWindowsPath(pathWithControlChars));
            Assert.Contains("control characters", exception.Message);
        }

        [Fact]
        public void ConvertToWindowsPath_WithCarriageReturnLineFeed_ThrowsArgumentException()
        {
            // Arrange
            var pathWithCRLF = "/mnt/c/file\r\nmalicious";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PathHandler.ConvertToWindowsPath(pathWithCRLF));
            Assert.Contains("dangerous pattern", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ConvertToWindowsPath_WithNullOrWhitespace_ReturnsInput(string input)
        {
            // Act
            var result = PathHandler.ConvertToWindowsPath(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void ConvertToWindowsPath_WithNull_ReturnsNull()
        {
            // Act
            var result = PathHandler.ConvertToWindowsPath(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ConvertToWslPath_WithDangerousPath_ThrowsArgumentException()
        {
            // Arrange
            var dangerousPath = "C:\\temp\\..\\..\\windows\\system32";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PathHandler.ConvertToWslPath(dangerousPath));
            Assert.Contains("dangerous pattern", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
