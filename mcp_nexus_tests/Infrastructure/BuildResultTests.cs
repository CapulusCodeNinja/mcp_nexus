using System;
using Xunit;
using mcp_nexus.Infrastructure;

namespace mcp_nexus_tests.Infrastructure
{
    public class BuildResultTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultValues_SetsCorrectDefaults()
        {
            // Arrange & Act
            var result = new BuildResult();

            // Assert
            Assert.False(result.Success);
            Assert.Equal(string.Empty, result.Output);
            Assert.Equal(string.Empty, result.Error);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Success_CanBeSetAndRetrieved()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Success = true;

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void Success_CanBeSetToFalse()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Success = false;

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void Output_CanBeSetAndRetrieved()
        {
            // Arrange
            var result = new BuildResult();
            var output = "Build completed successfully";

            // Act
            result.Output = output;

            // Assert
            Assert.Equal(output, result.Output);
        }

        [Fact]
        public void Output_CanBeSetToEmptyString()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Output = string.Empty;

            // Assert
            Assert.Equal(string.Empty, result.Output);
        }

        [Fact]
        public void Output_CanBeSetToNull()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Output = null!;

            // Assert
            Assert.Null(result.Output);
        }

        [Fact]
        public void Error_CanBeSetAndRetrieved()
        {
            // Arrange
            var result = new BuildResult();
            var error = "Build failed with errors";

            // Act
            result.Error = error;

            // Assert
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void Error_CanBeSetToEmptyString()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Error = string.Empty;

            // Assert
            Assert.Equal(string.Empty, result.Error);
        }

        [Fact]
        public void Error_CanBeSetToNull()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Error = null!;

            // Assert
            Assert.Null(result.Error);
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Fact]
        public void Properties_CanBeSetMultipleTimes()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Success = true;
            result.Output = "First output";
            result.Error = "First error";

            result.Success = false;
            result.Output = "Second output";
            result.Error = "Second error";

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Second output", result.Output);
            Assert.Equal("Second error", result.Error);
        }

        [Fact]
        public void Output_WithVeryLongString_HandlesCorrectly()
        {
            // Arrange
            var result = new BuildResult();
            var longOutput = new string('A', 10000); // 10KB string

            // Act
            result.Output = longOutput;

            // Assert
            Assert.Equal(longOutput, result.Output);
            Assert.Equal(10000, result.Output.Length);
        }

        [Fact]
        public void Error_WithVeryLongString_HandlesCorrectly()
        {
            // Arrange
            var result = new BuildResult();
            var longError = new string('E', 10000); // 10KB string

            // Act
            result.Error = longError;

            // Assert
            Assert.Equal(longError, result.Error);
            Assert.Equal(10000, result.Error.Length);
        }

        [Fact]
        public void Output_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var result = new BuildResult();
            var specialOutput = "Build output with special chars: \n\r\t\"'\\";

            // Act
            result.Output = specialOutput;

            // Assert
            Assert.Equal(specialOutput, result.Output);
        }

        [Fact]
        public void Error_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var result = new BuildResult();
            var specialError = "Error with special chars: \n\r\t\"'\\";

            // Act
            result.Error = specialError;

            // Assert
            Assert.Equal(specialError, result.Error);
        }

        [Fact]
        public void Output_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var result = new BuildResult();
            var unicodeOutput = "Build output with unicode: 你好世界 🌍";

            // Act
            result.Output = unicodeOutput;

            // Assert
            Assert.Equal(unicodeOutput, result.Output);
        }

        [Fact]
        public void Error_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var result = new BuildResult();
            var unicodeError = "Error with unicode: 你好世界 🌍";

            // Act
            result.Error = unicodeError;

            // Assert
            Assert.Equal(unicodeError, result.Error);
        }

        [Fact]
        public void Properties_WithMixedValues_WorkCorrectly()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Success = true;
            result.Output = "Build successful";
            result.Error = "No errors";

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Build successful", result.Output);
            Assert.Equal("No errors", result.Error);
        }

        [Fact]
        public void Properties_WithEmptyValues_WorkCorrectly()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Success = false;
            result.Output = string.Empty;
            result.Error = string.Empty;

            // Assert
            Assert.False(result.Success);
            Assert.Equal(string.Empty, result.Output);
            Assert.Equal(string.Empty, result.Error);
        }

        #endregion

        #region Scenario Tests

        [Fact]
        public void SuccessfulBuild_Scenario()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Success = true;
            result.Output = "Build completed successfully in 2.5 seconds";
            result.Error = string.Empty;

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Build completed successfully", result.Output);
            Assert.Equal(string.Empty, result.Error);
        }

        [Fact]
        public void FailedBuild_Scenario()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Success = false;
            result.Output = "Build failed";
            result.Error = "Compilation error: Missing reference";

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Build failed", result.Output);
            Assert.Contains("Compilation error", result.Error);
        }

        [Fact]
        public void PartialBuild_Scenario()
        {
            // Arrange
            var result = new BuildResult();

            // Act
            result.Success = false;
            result.Output = "Build partially completed";
            result.Error = "Warning: Some files could not be compiled";

            // Assert
            Assert.False(result.Success);
            Assert.Contains("partially completed", result.Output);
            Assert.Contains("Warning", result.Error);
        }

        #endregion

        #region Object State Tests

        [Fact]
        public void Object_CanBeCreatedMultipleTimes()
        {
            // Arrange & Act
            var result1 = new BuildResult();
            var result2 = new BuildResult();

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void Object_StateIsIndependent()
        {
            // Arrange
            var result1 = new BuildResult();
            var result2 = new BuildResult();

            // Act
            result1.Success = true;
            result1.Output = "Result 1";
            result1.Error = "Error 1";

            result2.Success = false;
            result2.Output = "Result 2";
            result2.Error = "Error 2";

            // Assert
            Assert.True(result1.Success);
            Assert.Equal("Result 1", result1.Output);
            Assert.Equal("Error 1", result1.Error);

            Assert.False(result2.Success);
            Assert.Equal("Result 2", result2.Output);
            Assert.Equal("Error 2", result2.Error);
        }

        #endregion
    }
}
