using FluentAssertions;

using nexus.extensions.Models;

using Xunit;

namespace nexus.extensions_unittests.Models;

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
        processInfo.CommandId.Should().Be(string.Empty);
        processInfo.ExtensionName.Should().Be(string.Empty);
        processInfo.SessionId.Should().Be(string.Empty);
        processInfo.ProcessId.Should().BeNull();
        processInfo.StartedAt.Should().Be(default);
        processInfo.IsRunning.Should().BeFalse();
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
        processInfo.CommandId.Should().Be(commandId);
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
        processInfo.ExtensionName.Should().Be(extensionName);
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
        processInfo.SessionId.Should().Be(sessionId);
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
        processInfo.ProcessId.Should().Be(processId);
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
        processInfo.ProcessId.Should().BeNull();
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
        processInfo.StartedAt.Should().Be(startedAt);
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
        processInfo.IsRunning.Should().BeTrue();
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
        processInfo.CommandId.Should().Be("cmd-789");
        processInfo.ExtensionName.Should().Be("MyExtension");
        processInfo.SessionId.Should().Be("session-012");
        processInfo.ProcessId.Should().Be(5678);
        processInfo.StartedAt.Should().Be(startedAt);
        processInfo.IsRunning.Should().BeTrue();
    }
}

