using Xunit;
using mcp_nexus.Extensions;
using System.Text.Json;

namespace mcp_nexus_unit_tests.Extensions
{
    /// <summary>
    /// Tests for Extension Callback DTO models.
    /// </summary>
    public class ExtensionCallbackDtoTests
    {
        #region ExtensionCallbackBulkStatusRequest Tests

        [Fact]
        public void BulkStatusRequest_DefaultConstructor_InitializesEmptyList()
        {
            // Act
            var request = new ExtensionCallbackBulkStatusRequest();

            // Assert
            Assert.NotNull(request.CommandIds);
            Assert.Empty(request.CommandIds);
        }

        [Fact]
        public void BulkStatusRequest_CommandIds_CanBeSet()
        {
            // Arrange
            var request = new ExtensionCallbackBulkStatusRequest();
            var commandIds = new List<string> { "cmd-1", "cmd-2", "cmd-3" };

            // Act
            request.CommandIds = commandIds;

            // Assert
            Assert.Equal(commandIds, request.CommandIds);
            Assert.Equal(3, request.CommandIds.Count);
        }

        [Fact]
        public void BulkStatusRequest_CommandIds_CanAddItems()
        {
            // Arrange
            var request = new ExtensionCallbackBulkStatusRequest();

            // Act
            request.CommandIds.Add("cmd-1");
            request.CommandIds.Add("cmd-2");

            // Assert
            Assert.Equal(2, request.CommandIds.Count);
            Assert.Contains("cmd-1", request.CommandIds);
            Assert.Contains("cmd-2", request.CommandIds);
        }

        [Fact]
        public void BulkStatusRequest_Serialization_PreservesCommandIds()
        {
            // Arrange
            var request = new ExtensionCallbackBulkStatusRequest
            {
                CommandIds = new List<string> { "cmd-1", "cmd-2" }
            };

            // Act
            var json = JsonSerializer.Serialize(request);
            var deserialized = JsonSerializer.Deserialize<ExtensionCallbackBulkStatusRequest>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.CommandIds.Count);
            Assert.Contains("cmd-1", deserialized.CommandIds);
            Assert.Contains("cmd-2", deserialized.CommandIds);
        }

        [Fact]
        public void BulkStatusRequest_JsonPropertyName_IsCorrect()
        {
            // Arrange
            var request = new ExtensionCallbackBulkStatusRequest
            {
                CommandIds = new List<string> { "cmd-1" }
            };

            // Act
            var json = JsonSerializer.Serialize(request);

            // Assert
            Assert.Contains("\"commandIds\"", json);
        }

        #endregion

        #region ExtensionCallbackBulkStatusResponse Tests

        [Fact]
        public void BulkStatusResponse_DefaultConstructor_InitializesEmptyDictionary()
        {
            // Act
            var response = new ExtensionCallbackBulkStatusResponse();

            // Assert
            Assert.NotNull(response.Results);
            Assert.Empty(response.Results);
            Assert.Null(response.Error);
        }

        [Fact]
        public void BulkStatusResponse_Results_CanBeSet()
        {
            // Arrange
            var response = new ExtensionCallbackBulkStatusResponse();
            var results = new Dictionary<string, ExtensionCallbackReadResponse>
            {
                ["cmd-1"] = new ExtensionCallbackReadResponse { CommandId = "cmd-1", Output = "output1" },
                ["cmd-2"] = new ExtensionCallbackReadResponse { CommandId = "cmd-2", Output = "output2" }
            };

            // Act
            response.Results = results;

            // Assert
            Assert.Equal(results, response.Results);
            Assert.Equal(2, response.Results.Count);
        }

        [Fact]
        public void BulkStatusResponse_Error_CanBeSet()
        {
            // Arrange
            var response = new ExtensionCallbackBulkStatusResponse();

            // Act
            response.Error = "Something went wrong";

            // Assert
            Assert.Equal("Something went wrong", response.Error);
        }

        [Fact]
        public void BulkStatusResponse_Serialization_PreservesAllProperties()
        {
            // Arrange
            var response = new ExtensionCallbackBulkStatusResponse
            {
                Results = new Dictionary<string, ExtensionCallbackReadResponse>
                {
                    ["cmd-1"] = new ExtensionCallbackReadResponse { CommandId = "cmd-1", Output = "output1" }
                },
                Error = "Test error"
            };

            // Act
            var json = JsonSerializer.Serialize(response);
            var deserialized = JsonSerializer.Deserialize<ExtensionCallbackBulkStatusResponse>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Single(deserialized.Results);
            Assert.Equal("Test error", deserialized.Error);
        }

        [Fact]
        public void BulkStatusResponse_WithNullError_SerializesCorrectly()
        {
            // Arrange
            var response = new ExtensionCallbackBulkStatusResponse
            {
                Results = new Dictionary<string, ExtensionCallbackReadResponse>()
            };

            // Act
            var json = JsonSerializer.Serialize(response);
            var deserialized = JsonSerializer.Deserialize<ExtensionCallbackBulkStatusResponse>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Null(deserialized.Error);
        }

        #endregion

        #region ExtensionCallbackQueueResponse Tests

        [Fact]
        public void QueueResponse_DefaultConstructor_InitializesEmptyStrings()
        {
            // Act
            var response = new ExtensionCallbackQueueResponse();

            // Assert
            Assert.Equal(string.Empty, response.CommandId);
            Assert.Equal(string.Empty, response.Status);
            Assert.Null(response.Message);
        }

        [Fact]
        public void QueueResponse_CommandId_CanBeSet()
        {
            // Arrange
            var response = new ExtensionCallbackQueueResponse();

            // Act
            response.CommandId = "cmd-123";

            // Assert
            Assert.Equal("cmd-123", response.CommandId);
        }

        [Fact]
        public void QueueResponse_Status_CanBeSet()
        {
            // Arrange
            var response = new ExtensionCallbackQueueResponse();

            // Act
            response.Status = "Queued";

            // Assert
            Assert.Equal("Queued", response.Status);
        }

        [Fact]
        public void QueueResponse_Message_CanBeSet()
        {
            // Arrange
            var response = new ExtensionCallbackQueueResponse();

            // Act
            response.Message = "Command queued successfully";

            // Assert
            Assert.Equal("Command queued successfully", response.Message);
        }

        [Fact]
        public void QueueResponse_AllProperties_CanBeSet()
        {
            // Act
            var response = new ExtensionCallbackQueueResponse
            {
                CommandId = "cmd-456",
                Status = "Executing",
                Message = "Command is executing"
            };

            // Assert
            Assert.Equal("cmd-456", response.CommandId);
            Assert.Equal("Executing", response.Status);
            Assert.Equal("Command is executing", response.Message);
        }

        [Fact]
        public void QueueResponse_Serialization_PreservesAllProperties()
        {
            // Arrange
            var response = new ExtensionCallbackQueueResponse
            {
                CommandId = "cmd-789",
                Status = "Completed",
                Message = "Command completed successfully"
            };

            // Act
            var json = JsonSerializer.Serialize(response);
            var deserialized = JsonSerializer.Deserialize<ExtensionCallbackQueueResponse>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("cmd-789", deserialized.CommandId);
            Assert.Equal("Completed", deserialized.Status);
            Assert.Equal("Command completed successfully", deserialized.Message);
        }

        [Fact]
        public void QueueResponse_WithNullMessage_SerializesCorrectly()
        {
            // Arrange
            var response = new ExtensionCallbackQueueResponse
            {
                CommandId = "cmd-000",
                Status = "Queued"
            };

            // Act
            var json = JsonSerializer.Serialize(response);
            var deserialized = JsonSerializer.Deserialize<ExtensionCallbackQueueResponse>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("cmd-000", deserialized.CommandId);
            Assert.Equal("Queued", deserialized.Status);
            Assert.Null(deserialized.Message);
        }

        #endregion
    }
}

