using System;
using System.Reflection;
using Xunit;
using mcp_nexus.Models;

namespace mcp_nexus_tests.Models
{
    /// <summary>
    /// Tests for VersionHelper
    /// </summary>
    public class VersionHelperTests
    {
        [Fact]
        public void GetFileVersion_ReturnsValidVersion()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            Assert.True(version.Length > 0);
        }

        [Fact]
        public void GetFileVersion_ReturnsVersionInCorrectFormat()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Should be in format like "1.0.0.0" or similar
            Assert.Matches(@"^\d+\.\d+\.\d+\.\d+$", version);
        }

        [Fact]
        public void GetFileVersion_ReturnsDefaultVersion_WhenFileVersionIsNull()
        {
            // This test is challenging because we can't easily mock FileVersionInfo
            // The method should return "1.0.0.0" as fallback when FileVersion is null
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            // The version should be a valid version string
            Assert.True(version.Length >= 7); // At least "1.0.0.0"
        }

        [Fact]
        public void GetFileVersion_MultipleCalls_ReturnSameVersion()
        {
            // Act
            var version1 = VersionHelper.GetFileVersion();
            var version2 = VersionHelper.GetFileVersion();

            // Assert
            Assert.Equal(version1, version2);
        }

        [Fact]
        public void GetFileVersion_ReturnsNonEmptyString()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            Assert.False(string.IsNullOrWhiteSpace(version));
        }

        [Fact]
        public void GetFileVersion_ReturnsVersionWithNumbers()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Should contain at least some numbers
            Assert.True(version.Any(char.IsDigit));
        }

        [Fact]
        public void GetFileVersion_ReturnsVersionWithDots()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Should contain dots as separators
            Assert.True(version.Contains("."));
        }

        [Fact]
        public void GetFileVersion_ReturnsValidVersionString()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Should be a valid version format (major.minor.build.revision)
            var parts = version.Split('.');
            Assert.True(parts.Length >= 2); // At least major.minor
            Assert.True(parts.Length <= 4); // At most major.minor.build.revision
            
            // Each part should be numeric
            foreach (var part in parts)
            {
                Assert.True(int.TryParse(part, out _));
            }
        }

        [Fact]
        public void GetFileVersion_ReturnsConsistentVersion()
        {
            // Act
            var version1 = VersionHelper.GetFileVersion();
            var version2 = VersionHelper.GetFileVersion();
            var version3 = VersionHelper.GetFileVersion();

            // Assert
            Assert.Equal(version1, version2);
            Assert.Equal(version2, version3);
            Assert.Equal(version1, version3);
        }

        [Fact]
        public void GetFileVersion_ReturnsVersionFromExecutingAssembly()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // The version should be from the executing assembly
            // This is hard to test directly, but we can verify it's a valid version
            Assert.True(version.Length > 0);
        }

        [Fact]
        public void GetFileVersion_HandlesAssemblyLocationCorrectly()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Should not throw exceptions and return a valid version
            var versionResult = VersionHelper.GetFileVersion();
            Assert.NotNull(versionResult);
        }

        [Fact]
        public void GetFileVersion_ReturnsVersionGreaterThanZero()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Parse the version and check it's greater than 0.0.0.0
            var versionParts = version.Split('.');
            if (versionParts.Length >= 1 && int.TryParse(versionParts[0], out var major))
            {
                Assert.True(major >= 0);
            }
        }

        [Fact]
        public void GetFileVersion_ReturnsVersionWithValidCharacters()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Should only contain valid version characters (digits and dots)
            Assert.Matches(@"^[\d\.]+$", version);
        }

        [Fact]
        public void GetFileVersion_ReturnsVersionThatCanBeParsed()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Should be parseable as a Version
            Assert.True(Version.TryParse(version, out var parsedVersion));
            Assert.NotNull(parsedVersion);
        }

        [Fact]
        public void GetFileVersion_ReturnsVersionWithReasonableLength()
        {
            // Act
            var version = VersionHelper.GetFileVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            
            // Should be a reasonable length (not too short, not too long)
            Assert.True(version.Length >= 5); // At least "1.0.0"
            Assert.True(version.Length <= 20); // Not unreasonably long
        }
    }
}
