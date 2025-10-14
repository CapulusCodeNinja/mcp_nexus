using System;
using System.Collections.Generic;
using Xunit;
using mcp_nexus.CommandQueue;

namespace mcp_nexus_tests.CommandQueue
{
    public class CommandResultTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output";
            var errorMessage = "Test error";
            var duration = TimeSpan.FromSeconds(5);
            var data = new Dictionary<string, object> { { "key", "value" } };

            // Act
            var result = new CommandResult(isSuccess, output, errorMessage, duration, data);

            // Assert
            Assert.Equal(isSuccess, result.IsSuccess);
            Assert.Equal(output, result.Output);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(duration, result.Duration);
            Assert.Equal(data, result.Data);
        }

        [Fact]
        public void Constructor_WithNullOutput_SetsEmptyString()
        {
            // Arrange
            var isSuccess = true;
            string? output = null;

            // Act
            var result = new CommandResult(isSuccess, output!);

            // Assert
            Assert.Equal(string.Empty, result.Output);
        }

        [Fact]
        public void Constructor_WithNullData_CreatesEmptyDictionary()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output";
            Dictionary<string, object>? data = null;

            // Act
            var result = new CommandResult(isSuccess, output, data: data);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public void Constructor_WithDefaultDuration_SetsZeroDuration()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output";

            // Act
            var result = new CommandResult(isSuccess, output);

            // Assert
            Assert.Equal(TimeSpan.Zero, result.Duration);
        }

        [Fact]
        public void Constructor_WithNullErrorMessage_SetsNullErrorMessage()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output";
            string? errorMessage = null;

            // Act
            var result = new CommandResult(isSuccess, output, errorMessage);

            // Assert
            Assert.Null(result.ErrorMessage);
        }

        #endregion

        #region Success Factory Method Tests

        [Fact]
        public void Success_WithOutput_ReturnsSuccessfulResult()
        {
            // Arrange
            var output = "Success output";
            var duration = TimeSpan.FromSeconds(3);
            var data = new Dictionary<string, object> { { "status", "completed" } };

            // Act
            var result = CommandResult.Success(output, duration, data);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(output, result.Output);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(duration, result.Duration);
            Assert.Equal(data, result.Data);
        }

        [Fact]
        public void Success_WithDefaultParameters_ReturnsSuccessfulResultWithDefaults()
        {
            // Arrange
            var output = "Success output";

            // Act
            var result = CommandResult.Success(output);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(output, result.Output);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(TimeSpan.Zero, result.Duration);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public void Success_WithNullData_CreatesEmptyDictionary()
        {
            // Arrange
            var output = "Success output";
            Dictionary<string, object>? data = null;

            // Act
            var result = CommandResult.Success(output, data: data);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        #endregion

        #region Failure Factory Method Tests

        [Fact]
        public void Failure_WithErrorMessage_ReturnsFailedResult()
        {
            // Arrange
            var errorMessage = "Command failed";
            var duration = TimeSpan.FromSeconds(2);
            var data = new Dictionary<string, object> { { "error", "timeout" } };

            // Act
            var result = CommandResult.Failure(errorMessage, duration, data);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(string.Empty, result.Output);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(duration, result.Duration);
            Assert.Equal(data, result.Data);
        }

        [Fact]
        public void Failure_WithDefaultParameters_ReturnsFailedResultWithDefaults()
        {
            // Arrange
            var errorMessage = "Command failed";

            // Act
            var result = CommandResult.Failure(errorMessage);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(string.Empty, result.Output);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(TimeSpan.Zero, result.Duration);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public void Failure_WithNullData_CreatesEmptyDictionary()
        {
            // Arrange
            var errorMessage = "Command failed";
            Dictionary<string, object>? data = null;

            // Act
            var result = CommandResult.Failure(errorMessage, data: data);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Fact]
        public void Constructor_WithEmptyStringOutput_SetsEmptyString()
        {
            // Arrange
            var isSuccess = true;
            var output = string.Empty;

            // Act
            var result = new CommandResult(isSuccess, output);

            // Assert
            Assert.Equal(string.Empty, result.Output);
        }

        [Fact]
        public void Constructor_WithEmptyStringErrorMessage_SetsEmptyString()
        {
            // Arrange
            var isSuccess = false;
            var output = "Test output";
            var errorMessage = string.Empty;

            // Act
            var result = new CommandResult(isSuccess, output, errorMessage);

            // Assert
            Assert.Equal(string.Empty, result.ErrorMessage);
        }

        [Fact]
        public void Constructor_WithVeryLongOutput_HandlesCorrectly()
        {
            // Arrange
            var isSuccess = true;
            var output = new string('A', 10000); // 10KB string

            // Act
            var result = new CommandResult(isSuccess, output);

            // Assert
            Assert.Equal(output, result.Output);
            Assert.Equal(10000, result.Output?.Length ?? 0);
        }

        [Fact]
        public void Constructor_WithVeryLongErrorMessage_HandlesCorrectly()
        {
            // Arrange
            var isSuccess = false;
            var output = "Test output";
            var errorMessage = new string('E', 10000); // 10KB string

            // Act
            var result = new CommandResult(isSuccess, output, errorMessage);

            // Assert
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(10000, result.ErrorMessage?.Length ?? 0);
        }

        [Fact]
        public void Constructor_WithLargeDuration_HandlesCorrectly()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output";
            var duration = TimeSpan.FromDays(365); // 1 year

            // Act
            var result = new CommandResult(isSuccess, output, duration: duration);

            // Assert
            Assert.Equal(duration, result.Duration);
        }

        [Fact]
        public void Constructor_WithLargeDataDictionary_HandlesCorrectly()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output";
            var data = new Dictionary<string, object>();
            for (int i = 0; i < 1000; i++)
            {
                data[$"key{i}"] = $"value{i}";
            }

            // Act
            var result = new CommandResult(isSuccess, output, data: data);

            // Assert
            Assert.Equal(1000, result.Data.Count);
            Assert.Equal(data, result.Data);
        }

        [Fact]
        public void Constructor_WithSpecialCharactersInOutput_HandlesCorrectly()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output with special chars: \n\r\t\"'\\";

            // Act
            var result = new CommandResult(isSuccess, output);

            // Assert
            Assert.Equal(output, result.Output);
        }

        [Fact]
        public void Constructor_WithSpecialCharactersInErrorMessage_HandlesCorrectly()
        {
            // Arrange
            var isSuccess = false;
            var output = "Test output";
            var errorMessage = "Error with special chars: \n\r\t\"'\\";

            // Act
            var result = new CommandResult(isSuccess, output, errorMessage);

            // Assert
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public void Constructor_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output with unicode: 你好世界 🌍";

            // Act
            var result = new CommandResult(isSuccess, output);

            // Assert
            Assert.Equal(output, result.Output);
        }
        private static readonly int[] value = new[] { 1, 2, 3 };

        [Fact]
        public void Constructor_WithComplexDataTypes_HandlesCorrectly()
        {
            // Arrange
            var isSuccess = true;
            var output = "Test output";
            var data = new Dictionary<string, object>
            {
                { "string", "value" },
                { "int", 42 },
                { "double", 3.14 },
                { "bool", true },
                { "array", value },
                { "nested", new Dictionary<string, object> { { "nestedKey", "nestedValue" } } }
            };

            // Act
            var result = new CommandResult(isSuccess, output, data: data);

            // Assert
            Assert.Equal(data, result.Data);
            Assert.Equal("value", result.Data["string"]);
            Assert.Equal(42, result.Data["int"]);
            Assert.Equal(3.14, result.Data["double"]);
            Assert.True((bool)result.Data["bool"]);
            Assert.Equal(new[] { 1, 2, 3 }, result.Data["array"]);
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void CommandResult_ImplementsICommandResult()
        {
            // Arrange
            var result = new CommandResult(true, "test");

            // Act & Assert
            Assert.IsAssignableFrom<ICommandResult>(result);
        }

        [Fact]
        public void Data_IsReadOnly()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "key", "value" } };
            var result = new CommandResult(true, "test", data: data);

            // Act & Assert
            Assert.True(result.Data is not null);
        }

        #endregion
    }
}
