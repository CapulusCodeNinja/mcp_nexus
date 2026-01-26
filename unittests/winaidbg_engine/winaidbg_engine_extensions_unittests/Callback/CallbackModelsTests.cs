using FluentAssertions;

using WinAiDbg.Engine.Extensions.Callback;

using Xunit;

namespace WinAiDbg.Engine.Extensions.Unittests.Callback;

/// <summary>
/// Unit tests for callback model classes.
/// </summary>
public class CallbackModelsTests
{
    /// <summary>
    /// Verifies that ExecuteCommandRequest initializes with default values.
    /// </summary>
    [Fact]
    public void ExecuteCommandRequest_Constructor_InitializesWithDefaultValues()
    {
        // Act
        var request = new ExecuteCommandRequest();

        // Assert
        _ = request.Command.Should().BeEmpty();
        _ = request.TimeoutSeconds.Should().Be(300);
    }

    /// <summary>
    /// Verifies that ExecuteCommandRequest properties can be set.
    /// </summary>
    [Fact]
    public void ExecuteCommandRequest_Properties_CanBeSet()
    {
        // Arrange
        var request = new ExecuteCommandRequest
        {
            Command = "!analyze -v",
            TimeoutSeconds = 600,
        };

        // Assert
        _ = request.Command.Should().Be("!analyze -v");
        _ = request.TimeoutSeconds.Should().Be(600);
    }

    /// <summary>
    /// Verifies that QueueCommandRequest initializes with default values.
    /// </summary>
    [Fact]
    public void QueueCommandRequest_Constructor_InitializesWithDefaultValues()
    {
        // Act
        var request = new QueueCommandRequest();

        // Assert
        _ = request.Command.Should().BeEmpty();
        _ = request.TimeoutSeconds.Should().Be(300);
    }

    /// <summary>
    /// Verifies that QueueCommandRequest properties can be set.
    /// </summary>
    [Fact]
    public void QueueCommandRequest_Properties_CanBeSet()
    {
        // Arrange
        var request = new QueueCommandRequest
        {
            Command = "kL",
            TimeoutSeconds = 120,
        };

        // Assert
        _ = request.Command.Should().Be("kL");
        _ = request.TimeoutSeconds.Should().Be(120);
    }

    /// <summary>
    /// Verifies that ReadCommandRequest initializes with default values.
    /// </summary>
    [Fact]
    public void ReadCommandRequest_Constructor_InitializesWithDefaultValues()
    {
        // Act
        var request = new ReadCommandRequest();

        // Assert
        _ = request.CommandId.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ReadCommandRequest properties can be set.
    /// </summary>
    [Fact]
    public void ReadCommandRequest_Properties_CanBeSet()
    {
        // Arrange
        var request = new ReadCommandRequest
        {
            CommandId = "cmd-123",
        };

        // Assert
        _ = request.CommandId.Should().Be("cmd-123");
    }

    /// <summary>
    /// Verifies that StatusCommandRequest initializes with default values.
    /// </summary>
    [Fact]
    public void StatusCommandRequest_Constructor_InitializesWithDefaultValues()
    {
        // Act
        var request = new StatusCommandRequest();

        // Assert
        _ = request.CommandId.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that StatusCommandRequest properties can be set.
    /// </summary>
    [Fact]
    public void StatusCommandRequest_Properties_CanBeSet()
    {
        // Arrange
        var request = new StatusCommandRequest
        {
            CommandId = "cmd-456",
        };

        // Assert
        _ = request.CommandId.Should().Be("cmd-456");
    }

    /// <summary>
    /// Verifies that BulkStatusRequest initializes with empty list.
    /// </summary>
    [Fact]
    public void BulkStatusRequest_Constructor_InitializesWithEmptyList()
    {
        // Act
        var request = new BulkStatusRequest();

        // Assert
        _ = request.CommandIds.Should().NotBeNull();
        _ = request.CommandIds.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that BulkStatusRequest can store command IDs.
    /// </summary>
    [Fact]
    public void BulkStatusRequest_CommandIds_CanBeSet()
    {
        // Arrange
        var request = new BulkStatusRequest
        {
            CommandIds = new List<string> { "cmd-1", "cmd-2", "cmd-3" },
        };

        // Assert
        _ = request.CommandIds.Should().HaveCount(3);
        _ = request.CommandIds.Should().Contain("cmd-1");
        _ = request.CommandIds.Should().Contain("cmd-2");
        _ = request.CommandIds.Should().Contain("cmd-3");
    }

    /// <summary>
    /// Verifies that LogRequest initializes with default values.
    /// </summary>
    [Fact]
    public void LogRequest_Constructor_InitializesWithDefaultValues()
    {
        // Act
        var request = new LogRequest();

        // Assert
        _ = request.Level.Should().Be("Info");
        _ = request.Message.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that LogRequest properties can be set.
    /// </summary>
    [Fact]
    public void LogRequest_Properties_CanBeSet()
    {
        // Arrange
        var request = new LogRequest
        {
            Level = "Error",
            Message = "Test error message",
        };

        // Assert
        _ = request.Level.Should().Be("Error");
        _ = request.Message.Should().Be("Test error message");
    }

    /// <summary>
    /// Verifies that CommandResponse initializes with default values.
    /// </summary>
    [Fact]
    public void CommandResponse_Constructor_InitializesWithDefaultValues()
    {
        // Act
        var response = new CommandResponse();

        // Assert
        _ = response.Success.Should().BeFalse();
        _ = response.CommandId.Should().BeEmpty();
        _ = response.Output.Should().BeEmpty();
        _ = response.Error.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CommandResponse properties can be set for success.
    /// </summary>
    [Fact]
    public void CommandResponse_Properties_CanBeSetForSuccess()
    {
        // Arrange
        var response = new CommandResponse
        {
            Success = true,
            CommandId = "cmd-789",
            Output = "Command executed successfully",
        };

        // Assert
        _ = response.Success.Should().BeTrue();
        _ = response.CommandId.Should().Be("cmd-789");
        _ = response.Output.Should().Be("Command executed successfully");
        _ = response.Error.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CommandResponse properties can be set for failure.
    /// </summary>
    [Fact]
    public void CommandResponse_Properties_CanBeSetForFailure()
    {
        // Arrange
        var response = new CommandResponse
        {
            Success = false,
            CommandId = "cmd-999",
            Error = "Command failed",
        };

        // Assert
        _ = response.Success.Should().BeFalse();
        _ = response.CommandId.Should().Be("cmd-999");
        _ = response.Error.Should().Be("Command failed");
        _ = response.Output.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that CommandStatus initializes with default values.
    /// </summary>
    [Fact]
    public void CommandStatus_Constructor_InitializesWithDefaultValues()
    {
        // Act
        var status = new CommandStatus();

        // Assert
        _ = status.CommandId.Should().BeEmpty();
        _ = status.State.Should().BeEmpty();
        _ = status.Command.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that CommandStatus properties can be set.
    /// </summary>
    [Fact]
    public void CommandStatus_Properties_CanBeSet()
    {
        // Arrange
        var status = new CommandStatus
        {
            CommandId = "cmd-111",
            State = "Completed",
            Command = "!analyze",
        };

        // Assert
        _ = status.CommandId.Should().Be("cmd-111");
        _ = status.State.Should().Be("Completed");
        _ = status.Command.Should().Be("!analyze");
    }

    /// <summary>
    /// Verifies that BulkStatusResponse initializes with empty list.
    /// </summary>
    [Fact]
    public void BulkStatusResponse_Constructor_InitializesWithEmptyList()
    {
        // Act
        var response = new BulkStatusResponse();

        // Assert
        _ = response.Commands.Should().NotBeNull();
        _ = response.Commands.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that BulkStatusResponse can store command statuses.
    /// </summary>
    [Fact]
    public void BulkStatusResponse_Commands_CanBeSet()
    {
        // Arrange
        var response = new BulkStatusResponse
        {
            Commands = new List<CommandStatus>
            {
                new() { CommandId = "cmd-1", State = "Completed" },
                new() { CommandId = "cmd-2", State = "Running" },
            },
        };

        // Assert
        _ = response.Commands.Should().HaveCount(2);
        _ = response.Commands[0].CommandId.Should().Be("cmd-1");
        _ = response.Commands[1].CommandId.Should().Be("cmd-2");
    }
}
