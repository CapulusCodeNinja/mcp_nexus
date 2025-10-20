using mcp_nexus.Utilities.Validation;

namespace mcp_nexus_unit_tests.Utilities
{
    /// <summary>
    /// Unit tests for the SymbolPathValidator utility class.
    /// Tests symbol path validation, cleaning, and warning detection functionality.
    /// </summary>
    public class SymbolPathValidatorTests
    {
        /// <summary>
        /// Tests that CleanSymbolPath removes leading and trailing whitespace from path elements.
        /// </summary>
        [Fact]
        public void CleanSymbolPath_WithLeadingTrailingWhitespace_RemovesWhitespace()
        {
            // Arrange
            var symbolPath = "  cache*C:\\Symbols\\Cache  ;  srv*https://symbols.microsoft.com  ";

            // Act
            var result = SymbolPathValidator.CleanSymbolPath(symbolPath);

            // Assert
            Assert.Equal("cache*C:\\Symbols\\Cache;srv*https://symbols.microsoft.com", result);
        }

        /// <summary>
        /// Tests that CleanSymbolPath normalizes path separators consistently.
        /// </summary>
        [Fact]
        public void CleanSymbolPath_WithMixedSeparators_NormalizesSeparators()
        {
            // Arrange
            var symbolPath = "cache*C:/Symbols/Cache;srv*https://symbols.microsoft.com";

            // Act
            var result = SymbolPathValidator.CleanSymbolPath(symbolPath);

            // Assert
            Assert.Equal("cache*C:\\Symbols\\Cache;srv*https://symbols.microsoft.com", result);
        }

        /// <summary>
        /// Tests that CleanSymbolPath removes empty path elements.
        /// </summary>
        [Fact]
        public void CleanSymbolPath_WithEmptyElements_RemovesEmptyElements()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache;;srv*https://symbols.microsoft.com;";

            // Act
            var result = SymbolPathValidator.CleanSymbolPath(symbolPath);

            // Assert
            Assert.Equal("cache*C:\\Symbols\\Cache;srv*https://symbols.microsoft.com", result);
        }

        /// <summary>
        /// Tests that CleanSymbolPath throws ArgumentException for null input.
        /// </summary>
        [Fact]
        public void CleanSymbolPath_WithNullInput_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SymbolPathValidator.CleanSymbolPath(null));
        }

        /// <summary>
        /// Tests that CleanSymbolPath throws ArgumentException for empty input.
        /// </summary>
        [Fact]
        public void CleanSymbolPath_WithEmptyInput_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SymbolPathValidator.CleanSymbolPath(""));
        }

        /// <summary>
        /// Tests that CleanSymbolPath throws ArgumentException for whitespace-only input.
        /// </summary>
        [Fact]
        public void CleanSymbolPath_WithWhitespaceOnlyInput_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SymbolPathValidator.CleanSymbolPath("   "));
        }

        /// <summary>
        /// Tests that CleanSymbolPath throws ArgumentException when no valid elements remain after cleaning.
        /// </summary>
        [Fact]
        public void CleanSymbolPath_WithOnlyEmptyElements_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SymbolPathValidator.CleanSymbolPath(";;;"));
        }

        /// <summary>
        /// Tests that IsValidSymbolPath returns true for valid symbol paths.
        /// </summary>
        [Fact]
        public void IsValidSymbolPath_WithValidPath_ReturnsTrue()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache;srv*https://symbols.microsoft.com";

            // Act
            var result = SymbolPathValidator.IsValidSymbolPath(symbolPath);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that IsValidSymbolPath returns false for null input.
        /// </summary>
        [Fact]
        public void IsValidSymbolPath_WithNullInput_ReturnsFalse()
        {
            // Act
            var result = SymbolPathValidator.IsValidSymbolPath(null);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that IsValidSymbolPath returns false for empty input.
        /// </summary>
        [Fact]
        public void IsValidSymbolPath_WithEmptyInput_ReturnsFalse()
        {
            // Act
            var result = SymbolPathValidator.IsValidSymbolPath("");

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that IsValidSymbolPath returns false for invalid input.
        /// </summary>
        [Fact]
        public void IsValidSymbolPath_WithInvalidInput_ReturnsFalse()
        {
            // Act
            var result = SymbolPathValidator.IsValidSymbolPath(";;;");

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that GetSymbolPathWarnings detects leading whitespace warnings.
        /// </summary>
        [Fact]
        public void GetSymbolPathWarnings_WithLeadingWhitespace_DetectsWarning()
        {
            // Arrange
            var symbolPath = "  cache*C:\\Symbols\\Cache;srv*https://symbols.microsoft.com";

            // Act
            var warnings = SymbolPathValidator.GetSymbolPathWarnings(symbolPath);

            // Assert
            Assert.Single(warnings);
            Assert.Contains("Whitespace at start of path element", warnings[0]);
        }

        /// <summary>
        /// Tests that GetSymbolPathWarnings detects trailing whitespace warnings.
        /// </summary>
        [Fact]
        public void GetSymbolPathWarnings_WithTrailingWhitespace_DetectsWarning()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache  ;srv*https://symbols.microsoft.com";

            // Act
            var warnings = SymbolPathValidator.GetSymbolPathWarnings(symbolPath);

            // Assert
            Assert.Single(warnings);
            Assert.Contains("Whitespace at end of path element", warnings[0]);
        }

        /// <summary>
        /// Tests that GetSymbolPathWarnings detects empty path elements.
        /// </summary>
        [Fact]
        public void GetSymbolPathWarnings_WithEmptyElements_DetectsWarning()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache;;srv*https://symbols.microsoft.com";

            // Act
            var warnings = SymbolPathValidator.GetSymbolPathWarnings(symbolPath);

            // Assert
            Assert.Single(warnings);
            Assert.Contains("Empty path element found", warnings[0]);
        }

        /// <summary>
        /// Tests that GetSymbolPathWarnings detects invalid characters.
        /// </summary>
        [Fact]
        public void GetSymbolPathWarnings_WithInvalidCharacters_DetectsWarning()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache;srv*https://symbols.microsoft.com|invalid";

            // Act
            var warnings = SymbolPathValidator.GetSymbolPathWarnings(symbolPath);

            // Assert
            Assert.Single(warnings);
            Assert.Contains("Invalid characters in path element", warnings[0]);
        }

        /// <summary>
        /// Tests that GetSymbolPathWarnings returns empty list for null input.
        /// </summary>
        [Fact]
        public void GetSymbolPathWarnings_WithNullInput_ReturnsEmptyList()
        {
            // Act
            var warnings = SymbolPathValidator.GetSymbolPathWarnings(null);

            // Assert
            Assert.Single(warnings);
            Assert.Contains("Symbol path is null or empty", warnings[0]);
        }

        /// <summary>
        /// Tests that GetSymbolPathWarnings returns empty list for valid input.
        /// </summary>
        [Fact]
        public void GetSymbolPathWarnings_WithValidInput_ReturnsEmptyList()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache;srv*https://symbols.microsoft.com";

            // Act
            var warnings = SymbolPathValidator.GetSymbolPathWarnings(symbolPath);

            // Assert
            Assert.Empty(warnings);
        }

        /// <summary>
        /// Tests that NormalizeSymbolPath returns cleaned path for valid input.
        /// </summary>
        [Fact]
        public void NormalizeSymbolPath_WithValidInput_ReturnsCleanedPath()
        {
            // Arrange
            var symbolPath = "  cache*C:\\Symbols\\Cache  ;  srv*https://symbols.microsoft.com  ";

            // Act
            var result = SymbolPathValidator.NormalizeSymbolPath(symbolPath);

            // Assert
            Assert.Equal("cache*C:\\Symbols\\Cache;srv*https://symbols.microsoft.com", result);
        }

        /// <summary>
        /// Tests that NormalizeSymbolPath returns original input for invalid input.
        /// </summary>
        [Fact]
        public void NormalizeSymbolPath_WithInvalidInput_ReturnsOriginalInput()
        {
            // Arrange
            var symbolPath = ";;;";

            // Act
            var result = SymbolPathValidator.NormalizeSymbolPath(symbolPath);

            // Assert
            Assert.Equal(symbolPath, result);
        }

        /// <summary>
        /// Tests that NormalizeSymbolPath returns empty string for null input.
        /// </summary>
        [Fact]
        public void NormalizeSymbolPath_WithNullInput_ReturnsEmptyString()
        {
            // Act
            var result = SymbolPathValidator.NormalizeSymbolPath(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Tests that CleanSymbolPath handles complex symbol paths with multiple elements.
        /// </summary>
        [Fact]
        public void CleanSymbolPath_WithComplexPath_CleansAllElements()
        {
            // Arrange
            var symbolPath = "  cache*C:\\Symbols\\Cache  ;  srv*C:\\Symbols\\Servers\\Microsoft  ;  srv*https://msdl.microsoft.com/download/symbols  ;  cache*C:\\Symbols\\Local  ";

            // Act
            var result = SymbolPathValidator.CleanSymbolPath(symbolPath);

            // Assert
            Assert.Equal("cache*C:\\Symbols\\Cache;srv*C:\\Symbols\\Servers\\Microsoft;srv*https://msdl.microsoft.com/download/symbols;cache*C:\\Symbols\\Local", result);
        }

        /// <summary>
        /// Tests that GetSymbolPathWarnings detects multiple types of warnings.
        /// </summary>
        [Fact]
        public void GetSymbolPathWarnings_WithMultipleIssues_DetectsAllWarnings()
        {
            // Arrange
            var symbolPath = "  cache*C:\\Symbols\\Cache  ;;;srv*https://symbols.microsoft.com|invalid";

            // Act
            var warnings = SymbolPathValidator.GetSymbolPathWarnings(symbolPath);

            // Assert
            Assert.Equal(5, warnings.Count);
            Assert.Contains(warnings, w => w.Contains("Whitespace at start of path element"));
            Assert.Contains(warnings, w => w.Contains("Whitespace at end of path element"));
            Assert.Contains(warnings, w => w.Contains("Empty path element found"));
            Assert.Contains(warnings, w => w.Contains("Invalid characters in path element"));
        }
    }
}
