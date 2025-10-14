using System;
using System.Collections.Generic;
using Xunit;
using mcp_nexus.CommandQueue;

namespace mcp_nexus_tests.CommandQueue
{
    public class CommandValidationResultTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var isValid = true;
            var errors = new List<string> { "Error 1", "Error 2" };
            var warnings = new List<string> { "Warning 1", "Warning 2" };

            // Act
            var result = new CommandValidationResult(isValid, errors, warnings);

            // Assert
            Assert.Equal(isValid, result.IsValid);
            Assert.Equal(errors, result.Errors);
            Assert.Equal(warnings, result.Warnings);
        }

        [Fact]
        public void Constructor_WithNullErrors_CreatesEmptyErrorsList()
        {
            // Arrange
            var isValid = true;
            List<string>? errors = null;
            var warnings = new List<string> { "Warning 1" };

            // Act
            var result = new CommandValidationResult(isValid, errors, warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
            Assert.Equal(warnings, result.Warnings);
        }

        [Fact]
        public void Constructor_WithNullWarnings_CreatesEmptyWarningsList()
        {
            // Arrange
            var isValid = false;
            var errors = new List<string> { "Error 1" };
            List<string>? warnings = null;

            // Act
            var result = new CommandValidationResult(isValid, errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(errors, result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Constructor_WithBothNullLists_CreatesEmptyLists()
        {
            // Arrange
            var isValid = true;
            List<string>? errors = null;
            List<string>? warnings = null;

            // Act
            var result = new CommandValidationResult(isValid, errors, warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Constructor_WithEmptyLists_SetsEmptyLists()
        {
            // Arrange
            var isValid = false;
            var errors = new List<string>();
            var warnings = new List<string>();

            // Act
            var result = new CommandValidationResult(isValid, errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        #endregion

        #region Valid Factory Method Tests

        [Fact]
        public void Valid_WithNoWarnings_ReturnsValidResult()
        {
            // Act
            var result = CommandValidationResult.Valid();

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Valid_WithWarnings_ReturnsValidResultWithWarnings()
        {
            // Arrange
            var warnings = new List<string> { "Warning 1", "Warning 2" };

            // Act
            var result = CommandValidationResult.Valid(warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
            Assert.Equal(warnings, result.Warnings);
        }

        [Fact]
        public void Valid_WithNullWarnings_ReturnsValidResultWithEmptyWarnings()
        {
            // Arrange
            List<string>? warnings = null;

            // Act
            var result = CommandValidationResult.Valid(warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Valid_WithEmptyWarnings_ReturnsValidResultWithEmptyWarnings()
        {
            // Arrange
            var warnings = new List<string>();

            // Act
            var result = CommandValidationResult.Valid(warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        #endregion

        #region Invalid Factory Method Tests

        [Fact]
        public void Invalid_WithErrorsList_ReturnsInvalidResult()
        {
            // Arrange
            var errors = new List<string> { "Error 1", "Error 2" };
            var warnings = new List<string> { "Warning 1" };

            // Act
            var result = CommandValidationResult.Invalid(errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(errors, result.Errors);
            Assert.Equal(warnings, result.Warnings);
        }

        [Fact]
        public void Invalid_WithErrorsOnly_ReturnsInvalidResultWithEmptyWarnings()
        {
            // Arrange
            var errors = new List<string> { "Error 1", "Error 2" };

            // Act
            var result = CommandValidationResult.Invalid(errors);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(errors, result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Invalid_WithNullWarnings_ReturnsInvalidResultWithEmptyWarnings()
        {
            // Arrange
            var errors = new List<string> { "Error 1" };
            List<string>? warnings = null;

            // Act
            var result = CommandValidationResult.Invalid(errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(errors, result.Errors);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Invalid_WithSingleError_ReturnsInvalidResultWithSingleError()
        {
            // Arrange
            var error = "Single error message";

            // Act
            var result = CommandValidationResult.Invalid(error);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal(error, result.Errors[0]);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Invalid_WithEmptyErrorString_ReturnsInvalidResultWithEmptyError()
        {
            // Arrange
            var error = string.Empty;

            // Act
            var result = CommandValidationResult.Invalid(error);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal(string.Empty, result.Errors[0]);
            Assert.NotNull(result.Warnings);
            Assert.Empty(result.Warnings);
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Fact]
        public void Constructor_WithVeryLongErrorMessages_HandlesCorrectly()
        {
            // Arrange
            var longError = new string('E', 10000); // 10KB string
            var errors = new List<string> { longError };
            var warnings = new List<string>();

            // Act
            var result = new CommandValidationResult(false, errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal(longError, result.Errors[0]);
            Assert.Equal(10000, result.Errors[0].Length);
        }

        [Fact]
        public void Constructor_WithVeryLongWarningMessages_HandlesCorrectly()
        {
            // Arrange
            var longWarning = new string('W', 10000); // 10KB string
            var errors = new List<string>();
            var warnings = new List<string> { longWarning };

            // Act
            var result = new CommandValidationResult(true, errors, warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.Single(result.Warnings);
            Assert.Equal(longWarning, result.Warnings[0]);
            Assert.Equal(10000, result.Warnings[0].Length);
        }

        [Fact]
        public void Constructor_WithManyErrors_HandlesCorrectly()
        {
            // Arrange
            var errors = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                errors.Add($"Error {i}");
            }
            var warnings = new List<string>();

            // Act
            var result = new CommandValidationResult(false, errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(1000, result.Errors.Count);
            Assert.Equal("Error 0", result.Errors[0]);
            Assert.Equal("Error 999", result.Errors[999]);
        }

        [Fact]
        public void Constructor_WithManyWarnings_HandlesCorrectly()
        {
            // Arrange
            var errors = new List<string>();
            var warnings = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                warnings.Add($"Warning {i}");
            }

            // Act
            var result = new CommandValidationResult(true, errors, warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(1000, result.Warnings.Count);
            Assert.Equal("Warning 0", result.Warnings[0]);
            Assert.Equal("Warning 999", result.Warnings[999]);
        }

        [Fact]
        public void Constructor_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var specialError = "Error with special chars: \n\r\t\"'\\";
            var specialWarning = "Warning with special chars: \n\r\t\"'\\";
            var errors = new List<string> { specialError };
            var warnings = new List<string> { specialWarning };

            // Act
            var result = new CommandValidationResult(false, errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(specialError, result.Errors[0]);
            Assert.Equal(specialWarning, result.Warnings[0]);
        }

        [Fact]
        public void Constructor_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var unicodeError = "Error with unicode: 你好世界 🌍";
            var unicodeWarning = "Warning with unicode: 你好世界 🌍";
            var errors = new List<string> { unicodeError };
            var warnings = new List<string> { unicodeWarning };

            // Act
            var result = new CommandValidationResult(false, errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(unicodeError, result.Errors[0]);
            Assert.Equal(unicodeWarning, result.Warnings[0]);
        }

        [Fact]
        public void Constructor_WithNullErrorStrings_HandlesCorrectly()
        {
            // Arrange
            var errors = new List<string> { null!, "Valid error", null! };
            var warnings = new List<string>();

            // Act
            var result = new CommandValidationResult(false, errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(3, result.Errors.Count);
            Assert.Null(result.Errors[0]);
            Assert.Equal("Valid error", result.Errors[1]);
            Assert.Null(result.Errors[2]);
        }

        [Fact]
        public void Constructor_WithNullWarningStrings_HandlesCorrectly()
        {
            // Arrange
            var errors = new List<string>();
            var warnings = new List<string> { null!, "Valid warning", null! };

            // Act
            var result = new CommandValidationResult(true, errors, warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(3, result.Warnings.Count);
            Assert.Null(result.Warnings[0]);
            Assert.Equal("Valid warning", result.Warnings[1]);
            Assert.Null(result.Warnings[2]);
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void CommandValidationResult_ImplementsICommandValidationResult()
        {
            // Arrange
            var result = new CommandValidationResult(true);

            // Act & Assert
            Assert.IsAssignableFrom<ICommandValidationResult>(result);
        }

        [Fact]
        public void Errors_IsReadOnly()
        {
            // Arrange
            var errors = new List<string> { "Error 1" };
            var result = new CommandValidationResult(false, errors);

            // Act & Assert
            Assert.True(result.Errors is not null);
        }

        [Fact]
        public void Warnings_IsReadOnly()
        {
            // Arrange
            var warnings = new List<string> { "Warning 1" };
            var result = new CommandValidationResult(true, warnings: warnings);

            // Act & Assert
            Assert.True(result.Warnings is not null);
        }

        #endregion

        #region Scenario Tests

        [Fact]
        public void ValidCommand_Scenario()
        {
            // Act
            var result = CommandValidationResult.Valid();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void ValidCommandWithWarnings_Scenario()
        {
            // Arrange
            var warnings = new List<string> { "Deprecated command", "Consider using newer version" };

            // Act
            var result = CommandValidationResult.Valid(warnings);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Warnings.Count);
            Assert.Contains("Deprecated command", result.Warnings);
            Assert.Contains("Consider using newer version", result.Warnings);
        }

        [Fact]
        public void InvalidCommand_Scenario()
        {
            // Arrange
            var errors = new List<string> { "Missing required parameter", "Invalid syntax" };

            // Act
            var result = CommandValidationResult.Invalid(errors);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
            Assert.Contains("Missing required parameter", result.Errors);
            Assert.Contains("Invalid syntax", result.Errors);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void InvalidCommandWithWarnings_Scenario()
        {
            // Arrange
            var errors = new List<string> { "Command not found" };
            var warnings = new List<string> { "Alternative command available" };

            // Act
            var result = CommandValidationResult.Invalid(errors, warnings);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Command not found", result.Errors[0]);
            Assert.Single(result.Warnings);
            Assert.Equal("Alternative command available", result.Warnings[0]);
        }

        [Fact]
        public void SingleErrorCommand_Scenario()
        {
            // Arrange
            var error = "Permission denied";

            // Act
            var result = CommandValidationResult.Invalid(error);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Permission denied", result.Errors[0]);
            Assert.Empty(result.Warnings);
        }

        #endregion
    }
}
