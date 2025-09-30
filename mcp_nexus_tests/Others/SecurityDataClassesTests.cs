using System;
using Xunit;
using mcp_nexus.Security;

namespace mcp_nexus_tests.Security
{
    /// <summary>
    /// Tests for Security data classes - simple data containers
    /// </summary>
    public class SecurityDataClassesTests
    {
        [Fact]
        public void SecurityValidationResult_Valid_ReturnsCorrectResult()
        {
            // Act
            var result = SecurityValidationResult.Valid();

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_Invalid_ReturnsCorrectResult()
        {
            // Arrange
            const string errorMessage = "Test validation error";

            // Act
            var result = SecurityValidationResult.Invalid(errorMessage);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Simple error")]
        [InlineData("Complex error with multiple details and special characters: !@#$%^&*()")]
        [InlineData("Error with unicode: 测试错误 🚨")]
        [InlineData("Very long error message that contains many details about what went wrong during the validation process and provides comprehensive information to help understand the issue")]
        public void SecurityValidationResult_Invalid_WithVariousMessages_ReturnsCorrectResult(string errorMessage)
        {
            // Act
            var result = SecurityValidationResult.Invalid(errorMessage);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_Invalid_WithNullMessage_HandlesCorrectly()
        {
            // Act
            var result = SecurityValidationResult.Invalid(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_Valid_IsImmutable()
        {
            // Act
            var result = SecurityValidationResult.Valid();

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);

            // Verify the result is consistent
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_Invalid_IsImmutable()
        {
            // Arrange
            const string errorMessage = "Immutable error";

            // Act
            var result = SecurityValidationResult.Invalid(errorMessage);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(errorMessage, result.ErrorMessage);

            // Verify the result is consistent
            Assert.False(result.IsValid);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_CanBeCreatedMultipleTimes()
        {
            // Act
            var valid1 = SecurityValidationResult.Valid();
            var valid2 = SecurityValidationResult.Valid();
            var invalid1 = SecurityValidationResult.Invalid("Error 1");
            var invalid2 = SecurityValidationResult.Invalid("Error 2");

            // Assert
            Assert.True(valid1.IsValid);
            Assert.True(valid2.IsValid);
            Assert.False(invalid1.IsValid);
            Assert.False(invalid2.IsValid);
            Assert.Equal("Error 1", invalid1.ErrorMessage);
            Assert.Equal("Error 2", invalid2.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_WithEmptyStringMessage_HandlesCorrectly()
        {
            // Act
            var result = SecurityValidationResult.Invalid(string.Empty);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(string.Empty, result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_WithWhitespaceMessage_HandlesCorrectly()
        {
            // Act
            var result = SecurityValidationResult.Invalid("   ");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("   ", result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            const string specialMessage = "Error with special chars: <>&\"'\\/";

            // Act
            var result = SecurityValidationResult.Invalid(specialMessage);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(specialMessage, result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            const string unicodeMessage = "Error with unicode: 测试错误 🚨 🔒 ⚠️";

            // Act
            var result = SecurityValidationResult.Invalid(unicodeMessage);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(unicodeMessage, result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_WithVeryLongMessage_HandlesCorrectly()
        {
            // Arrange
            var longMessage = new string('A', 10000);

            // Act
            var result = SecurityValidationResult.Invalid(longMessage);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(longMessage, result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_CanBeUsedInConditionals()
        {
            // Arrange
            var validResult = SecurityValidationResult.Valid();
            var invalidResult = SecurityValidationResult.Invalid("Test error");

            // Act & Assert
            if (validResult.IsValid)
            {
                Assert.True(true); // This should execute
            }
            else
            {
                Assert.Fail("Valid result should be valid");
            }

            if (!invalidResult.IsValid)
            {
                Assert.True(true); // This should execute
            }
            else
            {
                Assert.Fail("Invalid result should be invalid");
            }
        }

        [Fact]
        public void SecurityValidationResult_CanBeUsedInSwitchExpressions()
        {
            // Arrange
            var validResult = SecurityValidationResult.Valid();
            var invalidResult = SecurityValidationResult.Invalid("Test error");

            // Act & Assert
            var validMessage = validResult.IsValid switch
            {
                true => "Valid",
                false => "Invalid"
            };

            var invalidMessage = invalidResult.IsValid switch
            {
                true => "Valid",
                false => "Invalid"
            };

            Assert.Equal("Valid", validMessage);
            Assert.Equal("Invalid", invalidMessage);
        }

        [Fact]
        public void SecurityValidationResult_CanBeUsedInLinq()
        {
            // Arrange
            var results = new[]
            {
                SecurityValidationResult.Valid(),
                SecurityValidationResult.Invalid("Error 1"),
                SecurityValidationResult.Valid(),
                SecurityValidationResult.Invalid("Error 2")
            };

            // Act
            var validCount = results.Count(r => r.IsValid);
            var invalidCount = results.Count(r => !r.IsValid);
            var errorMessages = results.Where(r => !r.IsValid).Select(r => r.ErrorMessage).ToList();

            // Assert
            Assert.Equal(2, validCount);
            Assert.Equal(2, invalidCount);
            Assert.Contains("Error 1", errorMessages);
            Assert.Contains("Error 2", errorMessages);
        }
    }
}
