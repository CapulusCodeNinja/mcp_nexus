using FluentAssertions;

using Nexus.Engine.Extensions.Models;

namespace Nexus.Engine.Extensions.Tests.Models;

/// <summary>
/// Unit tests for the ExtensionProcessInfo class.
/// </summary>
public class ExtensionProcessInfoTests
{
    /// <summary>
    /// Verifies that ExtensionProcessInfo can be instantiated with default values.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var processInfo = new ExtensionProcessInfo();

        // Assert
        _ = processInfo.CommandId.Should().Be(string.Empty);
        _ = processInfo.ExtensionName.Should().Be(string.Empty);
        _ = processInfo.SessionId.Should().Be(string.Empty);
        _ = processInfo.ProcessId.Should().BeNull();
        _ = processInfo.StartedAt.Should().Be(default);
        _ = processInfo.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CommandId property can be set and retrieved.
    /// </summary>
    [Fact]
    public void CommandId_ShouldSetAndGetValue()
    {
        // Arrange
        var processInfo = new ExtensionProcessInfo();
        const string commandId = "cmd-123";

        // Act
        processInfo.CommandId = commandId;

        // Assert
        _ = processInfo.CommandId.Should().Be(commandId);
    }

    /// <summary>
    /// Verifies that ExtensionName property can be set and retrieved.
    /// </summary>
    [Fact]
    public void ExtensionName_ShouldSetAndGetValue()
    {
        // Arrange
        var processInfo = new ExtensionProcessInfo();
        const string extensionName = "TestExtension";

        // Act
        processInfo.ExtensionName = extensionName;

        // Assert
        _ = processInfo.ExtensionName.Should().Be(extensionName);
    }

    /// <summary>
    /// Verifies that SessionId property can be set and retrieved.
    /// </summary>
    [Fact]
    public void SessionId_ShouldSetAndGetValue()
    {
        // Arrange
        var processInfo = new ExtensionProcessInfo();
        const string sessionId = "session-456";

        // Act
        processInfo.SessionId = sessionId;

        // Assert
        _ = processInfo.SessionId.Should().Be(sessionId);
    }

    /// <summary>
    /// Verifies that ProcessId property can be set and retrieved with a value.
    /// </summary>
    [Fact]
    public void ProcessId_ShouldSetAndGetValue()
    {
        // Arrange
        var processInfo = new ExtensionProcessInfo();
        const int processId = 1234;

        // Act
        processInfo.ProcessId = processId;

        // Assert
        _ = processInfo.ProcessId.Should().Be(processId);
    }

    /// <summary>
    /// Verifies that ProcessId property can be null.
    /// </summary>
    [Fact]
    public void ProcessId_ShouldAllowNullValue()
    {
        // Arrange
        var processInfo = new ExtensionProcessInfo
        {
            ProcessId = 1234
        };

        // Act
        processInfo.ProcessId = null;

        // Assert
        _ = processInfo.ProcessId.Should().BeNull();
    }

    /// <summary>
    /// Verifies that StartedAt property can be set and retrieved.
    /// </summary>
    [Fact]
    public void StartedAt_ShouldSetAndGetValue()
    {
        // Arrange
        var processInfo = new ExtensionProcessInfo();
        var startedAt = DateTime.Now;

        // Act
        processInfo.StartedAt = startedAt;

        // Assert
        _ = processInfo.StartedAt.Should().Be(startedAt);
    }

    /// <summary>
    /// Verifies that IsRunning property can be set and retrieved.
    /// </summary>
    [Fact]
    public void IsRunning_ShouldSetAndGetValue()
    {
        // Arrange
        var processInfo = new ExtensionProcessInfo
        {
            // Act
            IsRunning = true
        };

        // Assert
        _ = processInfo.IsRunning.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that all properties can be set via object initializer.
    /// </summary>
    [Fact]
    public void ObjectInitializer_ShouldSetAllProperties()
    {
        // Arrange
        var startedAt = DateTime.Now;

        // Act
        var processInfo = new ExtensionProcessInfo
        {
            CommandId = "cmd-789",
            ExtensionName = "MyExtension",
            SessionId = "session-012",
            ProcessId = 5678,
            StartedAt = startedAt,
            IsRunning = true
        };

        // Assert
        _ = processInfo.CommandId.Should().Be("cmd-789");
        _ = processInfo.ExtensionName.Should().Be("MyExtension");
        _ = processInfo.SessionId.Should().Be("session-012");
        _ = processInfo.ProcessId.Should().Be(5678);
        _ = processInfo.StartedAt.Should().Be(startedAt);
        _ = processInfo.IsRunning.Should().BeTrue();
    }
}

