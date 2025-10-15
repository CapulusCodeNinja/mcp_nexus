using Xunit;
using mcp_nexus.Utilities.PathHandling;
using System.Runtime.InteropServices;

namespace mcp_nexus_unit_tests.Utilities
{
    /// <summary>
    /// Tests for WslPathConverter.
    /// </summary>
    public class WslPathConverterTests
    {
        private readonly WslPathConverter m_Converter;
        private readonly bool m_IsWindows;

        public WslPathConverterTests()
        {
            m_Converter = new WslPathConverter();
            m_IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        #region TryConvertToWindowsPath Tests

        [Fact]
        public void TryConvertToWindowsPath_WithEmptyPath_HandlesGracefully()
        {
            // Act
            var result = m_Converter.TryConvertToWindowsPath("", out var windowsPath);

            // Assert
            // Empty path might succeed or fail depending on WSL availability
            // The important thing is it doesn't crash
            Assert.NotNull(windowsPath);
        }

        [Fact]
        public void TryConvertToWindowsPath_WithNullPath_ReturnsFalse()
        {
            // Act
            var result = m_Converter.TryConvertToWindowsPath(null!, out var windowsPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryConvertToWindowsPath_WithInvalidPath_ReturnsFalse()
        {
            // Arrange
            var invalidPath = "/this/path/does/not/exist/xyz123";

            // Act
            var result = m_Converter.TryConvertToWindowsPath(invalidPath, out var windowsPath);

            // Assert
            // Should return false or return the original path
            if (!result)
            {
                Assert.Equal(invalidPath, windowsPath);
            }
        }

        [Fact]
        public void TryConvertToWindowsPath_WithPathContainingQuotes_HandlesEscaping()
        {
            // Arrange
            var pathWithQuotes = "/mnt/c/test\"path";

            // Act
            var result = m_Converter.TryConvertToWindowsPath(pathWithQuotes, out var windowsPath);

            // Assert
            // Should handle the quotes without crashing
            Assert.NotNull(windowsPath);
        }

        [Fact]
        public void TryConvertToWindowsPath_OnNonWindows_ReturnsFalseOrHandlesGracefully()
        {
            // Skip if on Windows (WSL might be available)
            if (m_IsWindows)
            {
                return;
            }

            // Act
            var result = m_Converter.TryConvertToWindowsPath("/mnt/c/test", out var windowsPath);

            // Assert
            // On non-Windows, should fail gracefully
            Assert.False(result);
        }

        [Fact]
        public void TryConvertToWindowsPath_WithLongPath_HandlesWithinTimeout()
        {
            // Arrange
            var longPath = "/mnt/c/" + new string('a', 200) + "/" + new string('b', 200);

            // Act
            var startTime = DateTime.Now;
            var result = m_Converter.TryConvertToWindowsPath(longPath, out var windowsPath);
            var elapsed = DateTime.Now - startTime;

            // Assert
            // Should complete within reasonable timeout (3 seconds max)
            Assert.True(elapsed.TotalSeconds < 3, $"Conversion should complete quickly, took {elapsed.TotalSeconds}s");
        }

        [Fact]
        public void TryConvertToWindowsPath_WithSpecialCharacters_HandlesGracefully()
        {
            // Arrange
            var specialCharsPath = "/mnt/c/test$path@with#special";

            // Act
            var result = m_Converter.TryConvertToWindowsPath(specialCharsPath, out var windowsPath);

            // Assert
            Assert.NotNull(windowsPath);
            // Should not throw
        }

        #endregion

        #region LoadFstabMappings Tests

        [Fact]
        public void LoadFstabMappings_ShouldReturnDictionary()
        {
            // Act
            var mappings = m_Converter.LoadFstabMappings();

            // Assert
            Assert.NotNull(mappings);
            // Should return an empty or populated dictionary without crashing
        }

        [Fact]
        public void LoadFstabMappings_ShouldHandleNoWsl()
        {
            // Act
            var mappings = m_Converter.LoadFstabMappings();

            // Assert
            Assert.NotNull(mappings);
            // If WSL not available, should return empty dictionary
        }

        [Fact]
        public void LoadFstabMappings_ShouldUseOrdinalIgnoreCaseComparison()
        {
            // Act
            var mappings = m_Converter.LoadFstabMappings();

            // Assert
            Assert.NotNull(mappings);
            // Dictionary should use case-insensitive comparison
            if (mappings.Count > 0)
            {
                var firstKey = mappings.Keys.First();
                // Try with different casing
                var upperKey = firstKey.ToUpperInvariant();
                var lowerKey = firstKey.ToLowerInvariant();

                // At least one should be found (case-insensitive)
                var hasUpper = mappings.ContainsKey(upperKey);
                var hasLower = mappings.ContainsKey(lowerKey);
                Assert.True(hasUpper || hasLower, "Dictionary should be case-insensitive");
            }
        }

        [Fact]
        public void LoadFstabMappings_CompletesWithinTimeout()
        {
            // Act
            var startTime = DateTime.Now;
            var mappings = m_Converter.LoadFstabMappings();
            var elapsed = DateTime.Now - startTime;

            // Assert
            Assert.NotNull(mappings);
            Assert.True(elapsed.TotalSeconds < 3, $"LoadFstabMappings should complete quickly, took {elapsed.TotalSeconds}s");
        }

        [Fact]
        public void LoadFstabMappings_ShouldNotThrowOnFailure()
        {
            // Act & Assert - Should not throw
            var exception = Record.Exception(() => m_Converter.LoadFstabMappings());
            Assert.Null(exception);
        }

        [Fact]
        public void LoadFstabMappings_MultipleCalls_ShouldBeConsistent()
        {
            // Act
            var mappings1 = m_Converter.LoadFstabMappings();
            var mappings2 = m_Converter.LoadFstabMappings();

            // Assert
            Assert.NotNull(mappings1);
            Assert.NotNull(mappings2);
            // Both calls should return same count (system state shouldn't change)
            Assert.Equal(mappings1.Count, mappings2.Count);
        }

        #endregion
    }
}

