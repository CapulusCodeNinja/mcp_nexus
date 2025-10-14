using mcp_nexus.Utilities;
using Xunit;

namespace mcp_nexus_tests.Utilities
{
    public class PathHandlerTests
    {
        [Fact]
        public void ConvertToWindowsPath_WslMountPath_ConvertsProperly()
        {
            // Arrange
            var wslPath = "/mnt/c/inetpub/wwwroot/uploads/dump.dmp";
            var expectedWindowsPath = "C:\\inetpub\\wwwroot\\uploads\\dump.dmp";

            // Act
            var result = PathHandler.ConvertToWindowsPath(wslPath);

            // Assert
            Assert.Equal(expectedWindowsPath, result);
        }

        [Fact]
        public void ConvertToWindowsPath_WslMountPathUppercaseDrive_ConvertsProperly()
        {
            // Arrange
            var wslPath = "/mnt/D/symbols";
            var expectedWindowsPath = "D:\\symbols";

            // Act
            var result = PathHandler.ConvertToWindowsPath(wslPath);

            // Assert
            Assert.Equal(expectedWindowsPath, result);
        }

        [Fact]
        public void ConvertToWindowsPath_WslMountRootPath_ConvertsProperly()
        {
            // Arrange
            var wslPath = "/mnt/c/";
            var expectedWindowsPath = "C:\\";

            // Act
            var result = PathHandler.ConvertToWindowsPath(wslPath);

            // Assert
            Assert.Equal(expectedWindowsPath, result);
        }

        [Fact]
        public void ConvertToWindowsPath_WslMountRootPathNoTrailingSlash_ConvertsProperly()
        {
            // Arrange
            var wslPath = "/mnt/c";
            var expectedWindowsPath = "C:\\";

            // Act
            var result = PathHandler.ConvertToWindowsPath(wslPath);

            // Assert
            Assert.Equal(expectedWindowsPath, result);
        }

        [Fact]
        public void ConvertToWindowsPath_AlreadyWindowsPath_ReturnsUnchanged()
        {
            // Arrange
            var windowsPath = "C:\\inetpub\\wwwroot\\uploads\\dump.dmp";

            // Act
            var result = PathHandler.ConvertToWindowsPath(windowsPath);

            // Assert
            Assert.Equal(windowsPath, result);
        }

        [Fact]
        public void ConvertToWindowsPath_UnixPath_ReturnsUnchanged()
        {
            // Arrange
            var unixPath = "/usr/local/bin/tool";

            // Act
            var result = PathHandler.ConvertToWindowsPath(unixPath);

            // Assert
            Assert.Equal(unixPath, result);
        }

        [Fact]
        public void ConvertToWindowsPath_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var emptyPath = "";

            // Act
            var result = PathHandler.ConvertToWindowsPath(emptyPath);

            // Assert
            Assert.Equal(emptyPath, result);
        }

        [Fact]
        public void ConvertToWindowsPath_NullString_ReturnsNull()
        {
            // Arrange
            string? nullPath = null;

            // Act
            var result = PathHandler.ConvertToWindowsPath(nullPath!);

            // Assert
            Assert.Equal(nullPath, result);
        }

        [Fact]
        public void ConvertToWslPath_WindowsPath_ConvertsProperly()
        {
            // Arrange
            var windowsPath = "C:\\inetpub\\wwwroot\\uploads\\dump.dmp";
            var expectedWslPath = "/mnt/c/inetpub/wwwroot/uploads/dump.dmp";

            // Act
            var result = PathHandler.ConvertToWslPath(windowsPath);

            // Assert
            Assert.Equal(expectedWslPath, result);
        }

        [Fact]
        public void ConvertToWslPath_WindowsPathLowercaseDrive_ConvertsProperly()
        {
            // Arrange
            var windowsPath = "d:\\symbols";
            var expectedWslPath = "/mnt/d/symbols";

            // Act
            var result = PathHandler.ConvertToWslPath(windowsPath);

            // Assert
            Assert.Equal(expectedWslPath, result);
        }

        [Fact]
        public void ConvertToWslPath_WindowsRootPath_ConvertsProperly()
        {
            // Arrange
            var windowsPath = "C:\\";
            var expectedWslPath = "/mnt/c";

            // Act
            var result = PathHandler.ConvertToWslPath(windowsPath);

            // Assert
            Assert.Equal(expectedWslPath, result);
        }

        [Fact]
        public void ConvertToWslPath_WindowsDriveOnly_ConvertsProperly()
        {
            // Arrange
            var windowsPath = "C:";
            var expectedWslPath = "/mnt/c";

            // Act
            var result = PathHandler.ConvertToWslPath(windowsPath);

            // Assert
            Assert.Equal(expectedWslPath, result);
        }

        [Fact]
        public void ConvertToWslPath_AlreadyUnixPath_ReturnsNormalized()
        {
            // Arrange
            var unixPath = "/usr/local/bin/tool";

            // Act
            var result = PathHandler.ConvertToWslPath(unixPath);

            // Assert
            Assert.Equal(unixPath, result);
        }

        [Fact]
        public void ConvertToWslPath_UnixPathWithBackslashes_NormalizesSlashes()
        {
            // Arrange
            var pathWithBackslashes = "/usr\\local\\bin\\tool";
            var expectedPath = "/usr/local/bin/tool";

            // Act
            var result = PathHandler.ConvertToWslPath(pathWithBackslashes);

            // Assert
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void IsWslMountPath_ValidWslMountPath_ReturnsTrue()
        {
            // Arrange
            var wslPath = "/mnt/c/inetpub/wwwroot/uploads/dump.dmp";

            // Act
            var result = PathHandler.IsWslMountPath(wslPath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsWslMountPath_ValidWslMountPathUppercase_ReturnsTrue()
        {
            // Arrange
            var wslPath = "/mnt/D/symbols";

            // Act
            var result = PathHandler.IsWslMountPath(wslPath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsWslMountPath_WindowsPath_ReturnsFalse()
        {
            // Arrange
            var windowsPath = "C:\\inetpub\\wwwroot\\uploads\\dump.dmp";

            // Act
            var result = PathHandler.IsWslMountPath(windowsPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWslMountPath_UnixPath_ReturnsFalse()
        {
            // Arrange
            var unixPath = "/usr/local/bin/tool";

            // Act
            var result = PathHandler.IsWslMountPath(unixPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWslMountPath_EmptyString_ReturnsFalse()
        {
            // Arrange
            var emptyPath = "";

            // Act
            var result = PathHandler.IsWslMountPath(emptyPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWslMountPath_NullString_ReturnsFalse()
        {
            // Arrange
            string? nullPath = null;

            // Act
            var result = PathHandler.IsWslMountPath(nullPath!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWindowsPath_ValidWindowsPath_ReturnsTrue()
        {
            // Arrange
            var windowsPath = "C:\\inetpub\\wwwroot\\uploads\\dump.dmp";

            // Act
            var result = PathHandler.IsWindowsPath(windowsPath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsWindowsPath_ValidWindowsPathLowercase_ReturnsTrue()
        {
            // Arrange
            var windowsPath = "d:\\symbols";

            // Act
            var result = PathHandler.IsWindowsPath(windowsPath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsWindowsPath_WslMountPath_ReturnsFalse()
        {
            // Arrange
            var wslPath = "/mnt/c/inetpub/wwwroot/uploads/dump.dmp";

            // Act
            var result = PathHandler.IsWindowsPath(wslPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWindowsPath_UnixPath_ReturnsFalse()
        {
            // Arrange
            var unixPath = "/usr/local/bin/tool";

            // Act
            var result = PathHandler.IsWindowsPath(unixPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWindowsPath_EmptyString_ReturnsFalse()
        {
            // Arrange
            var emptyPath = "";

            // Act
            var result = PathHandler.IsWindowsPath(emptyPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWindowsPath_NullString_ReturnsFalse()
        {
            // Arrange
            string? nullPath = null;

            // Act
            var result = PathHandler.IsWindowsPath(nullPath!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWindowsPath_ShortString_ReturnsFalse()
        {
            // Arrange
            var shortPath = "C";

            // Act
            var result = PathHandler.IsWindowsPath(shortPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void NormalizeForWindows_WslMountPath_ConvertsProperly()
        {
            // Arrange
            var wslPath = "/mnt/c/inetpub/wwwroot/uploads/dump.dmp";
            var expectedWindowsPath = "C:\\inetpub\\wwwroot\\uploads\\dump.dmp";

            // Act
            var result = PathHandler.NormalizeForWindows(wslPath);

            // Assert
            Assert.Equal(expectedWindowsPath, result);
        }

        [Fact]
        public void NormalizeForWindows_WindowsPath_ReturnsUnchanged()
        {
            // Arrange
            var windowsPath = "C:\\inetpub\\wwwroot\\uploads\\dump.dmp";

            // Act
            var result = PathHandler.NormalizeForWindows(windowsPath);

            // Assert
            Assert.Equal(windowsPath, result);
        }

        [Fact]
        public void NormalizeForWindows_UnixPath_ReturnsUnchanged()
        {
            // Arrange
            var unixPath = "/usr/local/bin/tool";

            // Act
            var result = PathHandler.NormalizeForWindows(unixPath);

            // Assert
            Assert.Equal(unixPath, result);
        }

        [Fact]
        public void NormalizeForWindows_ArrayOfPaths_ConvertsProperly()
        {
            // Arrange
            var paths = new[]
            {
                "/mnt/c/inetpub/wwwroot/uploads/dump.dmp",
                "D:\\symbols",
                "/usr/local/bin/tool"
            };
            var expectedPaths = new[]
            {
                "C:\\inetpub\\wwwroot\\uploads\\dump.dmp",
                "D:\\symbols",
                "/usr/local/bin/tool"
            };

            // Act
            var result = PathHandler.NormalizeForWindows(paths);

            // Assert
            Assert.Equal(expectedPaths, result);
        }

        [Fact]
        public void NormalizeForWindows_NullArray_ReturnsEmptyArray()
        {
            // Arrange
            string[]? nullArray = null;

            // Act
            var result = PathHandler.NormalizeForWindows(nullArray!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void NormalizeForWindows_EmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            var emptyArray = Array.Empty<string>();

            // Act
            var result = PathHandler.NormalizeForWindows(emptyArray);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ConvertToWindowsPath_WslPathWithSpaces_ConvertsProperly()
        {
            // Arrange
            var wslPath = "/mnt/c/Program Files/My App/dump file.dmp";
            var expectedWindowsPath = "C:\\Program Files\\My App\\dump file.dmp";

            // Act
            var result = PathHandler.ConvertToWindowsPath(wslPath);

            // Assert
            Assert.Equal(expectedWindowsPath, result);
        }

        [Fact]
        public void ConvertToWslPath_WindowsPathWithSpaces_ConvertsProperly()
        {
            // Arrange
            var windowsPath = "C:\\Program Files\\My App\\dump file.dmp";
            var expectedWslPath = "/mnt/c/Program Files/My App/dump file.dmp";

            // Act
            var result = PathHandler.ConvertToWslPath(windowsPath);

            // Assert
            Assert.Equal(expectedWslPath, result);
        }

        [Fact]
        public void ConvertToWindowsPath_WslPathWithSpecialChars_ConvertsProperly()
        {
            // Arrange
            var wslPath = "/mnt/c/temp/file-name_with.special@chars.dmp";
            var expectedWindowsPath = "C:\\temp\\file-name_with.special@chars.dmp";

            // Act
            var result = PathHandler.ConvertToWindowsPath(wslPath);

            // Assert
            Assert.Equal(expectedWindowsPath, result);
        }

        [Fact]
        public void ConvertToWslPath_WindowsPathWithSpecialChars_ConvertsProperly()
        {
            // Arrange
            var windowsPath = "C:\\temp\\file-name_with.special@chars.dmp";
            var expectedWslPath = "/mnt/c/temp/file-name_with.special@chars.dmp";

            // Act
            var result = PathHandler.ConvertToWslPath(windowsPath);

            // Assert
            Assert.Equal(expectedWslPath, result);
        }
    }
}
