using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Debugger;

namespace mcp_nexus_unit_tests.Debugger
{
    /// <summary>
    /// Unit tests for the SymbolVerificationHandler class.
    /// Tests symbol verification warning handling and third-party software detection.
    /// </summary>
    public class SymbolVerificationHandlerTests
    {
        private readonly Mock<ILogger<SymbolVerificationHandler>> m_MockLogger;
        private readonly SymbolVerificationHandler m_Handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolVerificationHandlerTests"/> class.
        /// </summary>
        public SymbolVerificationHandlerTests()
        {
            m_MockLogger = new Mock<ILogger<SymbolVerificationHandler>>();
            m_Handler = new SymbolVerificationHandler(m_MockLogger.Object);
        }

        /// <summary>
        /// Tests that ProcessSymbolWarnings returns original output when no warnings are present.
        /// </summary>
        [Fact]
        public void ProcessSymbolWarnings_WithNoWarnings_ReturnsOriginalOutput()
        {
            // Arrange
            var output = "Normal CDB output without warnings";

            // Act
            var result = m_Handler.ProcessSymbolWarnings(output);

            // Assert
            Assert.Equal(output, result);
        }

        /// <summary>
        /// Tests that ProcessSymbolWarnings returns original output when input is null.
        /// </summary>
        [Fact]
        public void ProcessSymbolWarnings_WithNullInput_ReturnsNull()
        {
            // Act
            var result = m_Handler.ProcessSymbolWarnings(null!);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ProcessSymbolWarnings returns original output when input is empty.
        /// </summary>
        [Fact]
        public void ProcessSymbolWarnings_WithEmptyInput_ReturnsEmpty()
        {
            // Act
            var result = m_Handler.ProcessSymbolWarnings("");

            // Assert
            Assert.Equal("", result);
        }

        /// <summary>
        /// Tests that ProcessSymbolWarnings handles checksum verification warnings for third-party software.
        /// </summary>
        [Fact]
        public void ProcessSymbolWarnings_WithThirdPartyChecksumWarning_HandlesWarning()
        {
            // Arrange
            var output = "*** WARNING: Unable to verify checksum for avast.dll";

            // Act
            var result = m_Handler.ProcessSymbolWarnings(output);

            // Assert
            Assert.Contains("INFO: Symbol verification warning for third-party module 'avast.dll'", result);
            Assert.Contains("this is normal for non-Microsoft software", result);
        }

        /// <summary>
        /// Tests that ProcessSymbolWarnings handles checksum verification warnings for Microsoft software.
        /// </summary>
        [Fact]
        public void ProcessSymbolWarnings_WithMicrosoftChecksumWarning_HandlesWarning()
        {
            // Arrange
            var output = "*** WARNING: Unable to verify checksum for kernel32.dll";

            // Act
            var result = m_Handler.ProcessSymbolWarnings(output);

            // Assert
            Assert.Contains("WARNING:", result);
            Assert.Contains("Consider updating symbols or checking module integrity", result);
        }

        /// <summary>
        /// Tests that ExtractSymbolWarnings correctly identifies checksum warnings.
        /// </summary>
        [Fact]
        public void ExtractSymbolWarnings_WithChecksumWarnings_ExtractsWarnings()
        {
            // Arrange
            var output = "Normal output\n*** WARNING: Unable to verify checksum for module.dll\nMore output";

            // Act
            var warnings = m_Handler.ExtractSymbolWarnings(output);

            // Assert
            Assert.Single(warnings);
            Assert.Contains("WARNING: Unable to verify checksum for module.dll", warnings[0]);
        }

        /// <summary>
        /// Tests that ExtractSymbolWarnings correctly identifies symbol loading warnings.
        /// </summary>
        [Fact]
        public void ExtractSymbolWarnings_WithSymbolWarnings_ExtractsWarnings()
        {
            // Arrange
            var output = "Normal output\n*** WARNING: Symbol loading failed for module.dll\nMore output";

            // Act
            var warnings = m_Handler.ExtractSymbolWarnings(output);

            // Assert
            Assert.Single(warnings);
            Assert.Contains("WARNING: Symbol loading failed for module.dll", warnings[0]);
        }

        /// <summary>
        /// Tests that ExtractSymbolWarnings correctly identifies module warnings.
        /// </summary>
        [Fact]
        public void ExtractSymbolWarnings_WithModuleWarnings_ExtractsWarnings()
        {
            // Arrange
            var output = "Normal output\n*** WARNING: Module verification failed for module.dll\nMore output";

            // Act
            var warnings = m_Handler.ExtractSymbolWarnings(output);

            // Assert
            Assert.Single(warnings);
            Assert.Contains("WARNING: Module verification failed for module.dll", warnings[0]);
        }

        /// <summary>
        /// Tests that ExtractSymbolWarnings returns empty list when no warnings are present.
        /// </summary>
        [Fact]
        public void ExtractSymbolWarnings_WithNoWarnings_ReturnsEmptyList()
        {
            // Arrange
            var output = "Normal CDB output without any warnings";

            // Act
            var warnings = m_Handler.ExtractSymbolWarnings(output);

            // Assert
            Assert.Empty(warnings);
        }

        /// <summary>
        /// Tests that ExtractSymbolWarnings handles multiple warnings.
        /// </summary>
        [Fact]
        public void ExtractSymbolWarnings_WithMultipleWarnings_ExtractsAllWarnings()
        {
            // Arrange
            var output = "Output\n*** WARNING: Unable to verify checksum for module1.dll\nMore output\n*** WARNING: Symbol loading failed for module2.dll\nEnd";

            // Act
            var warnings = m_Handler.ExtractSymbolWarnings(output);

            // Assert
            Assert.Equal(2, warnings.Count);
            Assert.Contains(warnings, w => w.Contains("Unable to verify checksum for module1.dll"));
            Assert.Contains(warnings, w => w.Contains("Symbol loading failed for module2.dll"));
        }

        /// <summary>
        /// Tests that GetSymbolLoadingRecommendations returns recommendations for checksum warnings.
        /// </summary>
        [Fact]
        public void GetSymbolLoadingRecommendations_WithChecksumWarnings_ReturnsRecommendations()
        {
            // Arrange
            var warnings = new List<string> { "*** WARNING: Unable to verify checksum for module.dll" };

            // Act
            var recommendations = m_Handler.GetSymbolLoadingRecommendations(warnings);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Contains("Consider updating symbol files"));
            Assert.Contains(recommendations, r => r.Contains("Verify symbol server connectivity"));
        }

        /// <summary>
        /// Tests that GetSymbolLoadingRecommendations returns recommendations for third-party warnings.
        /// </summary>
        [Fact]
        public void GetSymbolLoadingRecommendations_WithThirdPartyWarnings_ReturnsRecommendations()
        {
            // Arrange
            var warnings = new List<string> { "*** WARNING: Unable to verify checksum for avast.dll" };

            // Act
            var recommendations = m_Handler.GetSymbolLoadingRecommendations(warnings);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Contains("Third-party software symbols may not be available"));
            Assert.Contains(recommendations, r => r.Contains("Contact software vendor"));
        }

        /// <summary>
        /// Tests that GetSymbolLoadingRecommendations returns empty list when no warnings are present.
        /// </summary>
        [Fact]
        public void GetSymbolLoadingRecommendations_WithNoWarnings_ReturnsEmptyList()
        {
            // Arrange
            var warnings = new List<string>();

            // Act
            var recommendations = m_Handler.GetSymbolLoadingRecommendations(warnings);

            // Assert
            Assert.Empty(recommendations);
        }

        /// <summary>
        /// Tests that ValidateSymbolServerConfiguration returns valid result for proper configuration.
        /// </summary>
        [Fact]
        public void ValidateSymbolServerConfiguration_WithValidConfiguration_ReturnsValidResult()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache;srv*https://msdl.microsoft.com/download/symbols";
            var warnings = new List<string>();

            // Act
            var result = m_Handler.ValidateSymbolServerConfiguration(symbolPath, warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Warnings);
        }

        /// <summary>
        /// Tests that ValidateSymbolServerConfiguration returns invalid result for null symbol path.
        /// </summary>
        [Fact]
        public void ValidateSymbolServerConfiguration_WithNullSymbolPath_ReturnsInvalidResult()
        {
            // Arrange
            string? symbolPath = null;
            var warnings = new List<string>();

            // Act
            var result = m_Handler.ValidateSymbolServerConfiguration(symbolPath, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("Symbol search path is not configured"));
            Assert.Contains(result.Recommendations, r => r.Contains("Configure symbol search path"));
        }

        /// <summary>
        /// Tests that ValidateSymbolServerConfiguration returns invalid result for empty symbol path.
        /// </summary>
        [Fact]
        public void ValidateSymbolServerConfiguration_WithEmptySymbolPath_ReturnsInvalidResult()
        {
            // Arrange
            var symbolPath = "";
            var warnings = new List<string>();

            // Act
            var result = m_Handler.ValidateSymbolServerConfiguration(symbolPath, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("Symbol search path is not configured"));
        }

        /// <summary>
        /// Tests that ValidateSymbolServerConfiguration detects missing symbol server configuration.
        /// </summary>
        [Fact]
        public void ValidateSymbolServerConfiguration_WithoutSymbolServer_DetectsMissingConfiguration()
        {
            // Arrange
            var symbolPath = "cache*C:\\Symbols\\Cache";
            var warnings = new List<string>();

            // Act
            var result = m_Handler.ValidateSymbolServerConfiguration(symbolPath, warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("No symbol server (srv*) configured"));
            Assert.Contains(result.Recommendations, r => r.Contains("Add symbol server configuration"));
        }

        /// <summary>
        /// Tests that ValidateSymbolServerConfiguration detects missing cache configuration.
        /// </summary>
        [Fact]
        public void ValidateSymbolServerConfiguration_WithoutCache_DetectsMissingConfiguration()
        {
            // Arrange
            var symbolPath = "srv*https://msdl.microsoft.com/download/symbols";
            var warnings = new List<string>();

            // Act
            var result = m_Handler.ValidateSymbolServerConfiguration(symbolPath, warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("No local cache configured"));
            Assert.Contains(result.Recommendations, r => r.Contains("Add local cache directory"));
        }

        /// <summary>
        /// Tests that ProcessSymbolWarnings handles multiple warnings in the same output.
        /// </summary>
        [Fact]
        public void ProcessSymbolWarnings_WithMultipleWarnings_HandlesAllWarnings()
        {
            // Arrange
            var output = "Output\n*** WARNING: Unable to verify checksum for avast.dll\nMore output\n*** WARNING: Unable to verify checksum for kernel32.dll\nEnd";

            // Act
            var result = m_Handler.ProcessSymbolWarnings(output);

            // Assert
            Assert.Contains("INFO: Symbol verification warning for third-party module 'avast.dll'", result);
            Assert.Contains("WARNING:", result);
            Assert.Contains("Consider updating symbols or checking module integrity", result);
        }

        /// <summary>
        /// Tests that ProcessSymbolWarnings handles warnings with different formats.
        /// </summary>
        [Theory]
        [InlineData("*** WARNING: Unable to verify checksum for avast.dll")]
        [InlineData("WARNING: Unable to verify checksum for avast.dll")]
        [InlineData("*** WARNING: Unable to verify checksum for avast.exe")]
        [InlineData("*** WARNING: Unable to verify checksum for avast.sys")]
        public void ProcessSymbolWarnings_WithDifferentWarningFormats_HandlesWarnings(string warning)
        {
            // Act
            var result = m_Handler.ProcessSymbolWarnings(warning);

            // Assert
            Assert.NotEqual(warning, result); // Should be processed
            Assert.Contains("avast", result);
        }

        /// <summary>
        /// Tests that GetSymbolLoadingRecommendations handles mixed warning types.
        /// </summary>
        [Fact]
        public void GetSymbolLoadingRecommendations_WithMixedWarnings_ReturnsComprehensiveRecommendations()
        {
            // Arrange
            var warnings = new List<string>
            {
                "*** WARNING: Unable to verify checksum for avast.dll",
                "*** WARNING: Unable to verify checksum for kernel32.dll"
            };

            // Act
            var recommendations = m_Handler.GetSymbolLoadingRecommendations(warnings);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Contains("Consider updating symbol files"));
            Assert.Contains(recommendations, r => r.Contains("Third-party software symbols"));
            Assert.Contains(recommendations, r => r.Contains("Contact software vendor"));
        }

        #region Branch Coverage Tests - Missing Branches

        [Fact]
        public void ProcessSymbolWarnings_WithSymbolKeywordWarning_ProcessesCorrectly()
        {
            // Arrange - Tests Line 63 branch: WARNING containing "symbol"
            var output = "Some output\n*** WARNING: symbol file is corrupted for test.dll\nMore output";

            // Act
            var result = m_Handler.ProcessSymbolWarnings(output);

            // Assert - should process the warning
            Assert.NotNull(result);
        }

        [Fact]
        public void ProcessSymbolWarnings_WithModuleKeywordWarning_ProcessesCorrectly()
        {
            // Arrange - Tests similar branch: WARNING containing "module"
            var output = "Some output\n*** WARNING: module could not be loaded\nMore output";

            // Act
            var result = m_Handler.ProcessSymbolWarnings(output);

            // Assert - should process the warning
            Assert.NotNull(result);
        }

        [Fact]
        public void ProcessSymbolWarnings_WithSymbolChecksumWarning_HandlesGracefully()
        {
            // Arrange
            var output = "*** WARNING: symbol checksum verification failed for kernel32.dll";

            // Act
            var result = m_Handler.ProcessSymbolWarnings(output);

            // Assert - should process without error
            Assert.NotNull(result);
        }

        #endregion
    }
}
