using System.ComponentModel.DataAnnotations;
using mcp_nexus.Models;
using Xunit;

namespace mcp_nexus_tests.Models
{
    /// <summary>
    /// Tests for model validation attributes to ensure proper input validation.
    /// </summary>
    public class ValidationTests
    {
        #region McpRequest Validation Tests

        [Fact]
        public void McpRequest_WithValidData_PassesValidation()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = "tools/list",
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void McpRequest_WithInvalidJsonRpc_FailsValidation()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "1.0", // Invalid - should be "2.0"
                Method = "tools/list",
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("jsonrpc must be '2.0'"));
        }

        [Fact]
        public void McpRequest_WithEmptyJsonRpc_FailsValidation()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "", // Invalid - required field
                Method = "tools/list",
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("jsonrpc field is required"));
        }

        [Fact]
        public void McpRequest_WithEmptyMethod_FailsValidation()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = "", // Invalid - Required with AllowEmptyStrings = false
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("method field is required"));
        }

        [Fact]
        public void McpRequest_WithTooLongMethod_FailsValidation()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = new string('A', 101), // Invalid - exceeds 100 character limit
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("method must be between 1 and 100 characters"));
        }

        [Theory]
        [InlineData("tools/list")]
        [InlineData("initialize")]
        [InlineData("tools/call")]
        [InlineData("notifications/initialized")]
        [InlineData("a")] // Minimum length
        [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqr")] // 100 chars
        public void McpRequest_WithValidMethodLengths_PassesValidation(string method)
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = method,
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        #endregion

        #region McpError Validation Tests

        [Fact]
        public void McpError_WithValidData_PassesValidation()
        {
            // Arrange
            var error = new McpError
            {
                Code = -32603,
                Message = "Internal error"
            };

            // Act
            var validationResults = ValidateModel(error);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void McpError_WithEmptyMessage_FailsValidation()
        {
            // Arrange
            var error = new McpError
            {
                Code = -32603,
                Message = "" // Invalid - required field
            };

            // Act
            var validationResults = ValidateModel(error);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("error message is required"));
        }

        [Fact]
        public void McpError_WithTooLongMessage_FailsValidation()
        {
            // Arrange
            var error = new McpError
            {
                Code = -32603,
                Message = new string('A', 1001) // Invalid - exceeds 1000 character limit
            };

            // Act
            var validationResults = ValidateModel(error);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("error message cannot exceed 1000 characters"));
        }

        [Theory]
        [InlineData(-32700, "Parse error")]
        [InlineData(-32600, "Invalid Request")]
        [InlineData(-32601, "Method not found")]
        [InlineData(-32602, "Invalid params")]
        [InlineData(-32603, "Internal error")]
        [InlineData(-32000, "Server error")]
        [InlineData(0, "Success")]
        [InlineData(1, "Custom error")]
        public void McpError_WithValidErrorCodes_PassesValidation(int code, string message)
        {
            // Arrange
            var error = new McpError
            {
                Code = code,
                Message = message
            };

            // Act
            var validationResults = ValidateModel(error);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void McpError_WithMaxLengthMessage_PassesValidation()
        {
            // Arrange
            var error = new McpError
            {
                Code = -32603,
                Message = new string('A', 1000) // Exactly at the limit
            };

            // Act
            var validationResults = ValidateModel(error);

            // Assert
            Assert.Empty(validationResults);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void McpRequest_WithNullJsonRpc_FailsValidation()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = null!, // Invalid - required field
                Method = "tools/list",
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("jsonrpc field is required"));
        }

        [Fact]
        public void McpRequest_WithNullMethod_FailsValidation()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = null!, // Invalid - required field
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("method field is required"));
        }

        [Fact]
        public void McpError_WithNullMessage_FailsValidation()
        {
            // Arrange
            var error = new McpError
            {
                Code = -32603,
                Message = null! // Invalid - required field
            };

            // Act
            var validationResults = ValidateModel(error);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("error message is required"));
        }

        #endregion

        #region Unicode and Special Characters

        [Fact]
        public void McpRequest_WithUnicodeMethod_PassesValidation()
        {
            // Arrange
            var request = new McpRequest
            {
                JsonRpc = "2.0",
                Method = "测试_method_🚀", // Unicode characters
                Id = 1
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void McpError_WithUnicodeMessage_PassesValidation()
        {
            // Arrange
            var error = new McpError
            {
                Code = -32603,
                Message = "Ошибка сервера 🔥 测试错误" // Unicode characters
            };

            // Act
            var validationResults = ValidateModel(error);

            // Assert
            Assert.Empty(validationResults);
        }

        #endregion

        #region Helper Methods

        private static List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        #endregion
    }
}
