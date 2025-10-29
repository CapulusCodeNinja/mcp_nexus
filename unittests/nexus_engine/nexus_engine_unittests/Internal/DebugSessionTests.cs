using FluentAssertions;

using Moq;

using Nexus.Engine.Internal;
using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using Xunit;

namespace Nexus.Engine.Unittests.Internal;

/// <summary>
/// Unit tests for DebugSession class.
/// Tests session lifecycle, command management, and event handling with mocked dependencies.
/// </summary>
public class DebugSessionTests : IDisposable
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private const string TestSessionId = "test-session-id";
    private const string TestDumpFilePath = @"C:\test\dump.dmp";
    private const string TestSymbolPath = @"C:\symbols";

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugSessionTests"/> class.
    /// </summary>
    public DebugSessionTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();
    }

    /// <summary>
    /// Cleans up test resources.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a command preprocessor for testing.
    /// </summary>
    /// <returns></returns>
    private Nexus.Engine.Preprocessing.CommandPreprocessor CreatePreprocessor()
    {
        return new Nexus.Engine.Preprocessing.CommandPreprocessor(m_MockFileSystem.Object);
    }


    /// <summary>
    /// Verifies that constructor throws when sessionId is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullSessionId_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new DebugSession(null!, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor()));
    }

    /// <summary>
    /// Verifies that constructor throws when dumpFilePath is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullDumpFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new DebugSession(TestSessionId, null!, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor()));
    }

    /// <summary>
    /// Verifies that constructor succeeds with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Act
        using var session = new DebugSession(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Assert
        _ = session.Should().NotBeNull();
        _ = session.SessionId.Should().Be(TestSessionId);
        _ = session.State.Should().Be(SessionState.Initializing);
        _ = session.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that constructor succeeds with null symbolPath.
    /// </summary>
    [Fact]
    public void Constructor_WithNullSymbolPath_Succeeds()
    {
        // Act
        using var session = new DebugSession(TestSessionId, TestDumpFilePath, null, m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Assert
        _ = session.Should().NotBeNull();
        _ = session.SessionId.Should().Be(TestSessionId);
    }



    /// <summary>
    /// Verifies that SessionId property returns correct value.
    /// </summary>
    [Fact]
    public void SessionId_ReturnsCorrectValue()
    {
        // Arrange
        using var session = new DebugSession(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        var sessionId = session.SessionId;

        // Assert
        _ = sessionId.Should().Be(TestSessionId);
    }

    /// <summary>
    /// Verifies that State property is thread-safe.
    /// </summary>
    [Fact]
    public void State_IsThreadSafe()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act - Access state from multiple threads
        var states = new System.Collections.Concurrent.ConcurrentBag<SessionState>();
        _ = Parallel.For(0, 100, _ =>
        {
            accessor.SetState(SessionState.Active);
            states.Add(accessor.State);
        });

        // Assert
        _ = states.Should().AllSatisfy(state => state.Should().Be(SessionState.Active));
    }

    /// <summary>
    /// Verifies that IsActive returns true when state is Active.
    /// </summary>
    [Fact]
    public void IsActive_WhenStateIsActive_ReturnsTrue()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.SetState(SessionState.Active);

        // Act
        var isActive = accessor.IsActive;

        // Assert
        _ = isActive.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsActive returns false when state is not Active.
    /// </summary>
    [Fact]
    public void IsActive_WhenStateIsNotActive_ReturnsFalse()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.SetState(SessionState.Closing);

        // Act
        var isActive = accessor.IsActive;

        // Assert
        _ = isActive.Should().BeFalse();
    }



    /// <summary>
    /// Verifies that CommandStateChanged event can be subscribed to.
    /// </summary>
    [Fact]
    public void CommandStateChanged_CanSubscribe()
    {
        // Arrange
        using var session = new DebugSession(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        var eventRaised = false;
        session.CommandStateChanged += (sender, args) => eventRaised = true;

        // Assert
        _ = eventRaised.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that SessionStateChanged event can be subscribed to.
    /// </summary>
    [Fact]
    public void SessionStateChanged_CanSubscribe()
    {
        // Arrange
        using var session = new DebugSession(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        var eventRaised = false;
        session.SessionStateChanged += (sender, args) => eventRaised = true;

        // Assert
        _ = eventRaised.Should().BeFalse();
    }



    /// <summary>
    /// Verifies that Dispose can be called multiple times safely.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var session = new DebugSession(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        session.Dispose();
        session.Dispose();

        // Assert - Should not throw
    }



    /// <summary>
    /// Verifies that SetState changes the state correctly.
    /// </summary>
    [Fact]
    public void SetState_ChangesState()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        accessor.SetState(SessionState.Active);

        // Assert
        _ = accessor.State.Should().Be(SessionState.Active);
    }

    /// <summary>
    /// Verifies that SetState raises SessionStateChanged event.
    /// </summary>
    [Fact]
    public void SetState_RaisesSessionStateChangedEvent()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        var eventRaised = false;
        SessionStateChangedEventArgs? capturedArgs = null;
        accessor.SessionStateChanged += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        // Act
        accessor.SetState(SessionState.Active);

        // Assert
        _ = eventRaised.Should().BeTrue();
        _ = capturedArgs.Should().NotBeNull();
        _ = capturedArgs!.SessionId.Should().Be(TestSessionId);
        _ = capturedArgs.OldState.Should().Be(SessionState.Initializing);
        _ = capturedArgs.NewState.Should().Be(SessionState.Active);
    }

    /// <summary>
    /// Verifies that SetState does not raise event when state doesn't change.
    /// </summary>
    [Fact]
    public void SetState_WithSameState_DoesNotRaiseEvent()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.SetState(SessionState.Active);

        var eventRaised = false;
        accessor.SessionStateChanged += (sender, args) => eventRaised = true;

        // Act
        accessor.SetState(SessionState.Active);

        // Assert
        _ = eventRaised.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged forwards event with session context.
    /// </summary>
    [Fact]
    public void OnCommandStateChanged_ForwardsEventWithSessionContext()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        var eventRaised = false;
        CommandStateChangedEventArgs? capturedArgs = null;
        accessor.CommandStateChanged += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        var sourceArgs = new CommandStateChangedEventArgs
        {
            SessionId = "different-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Executing,
            Command = "!analyze",
            Timestamp = DateTime.Now,
        };

        // Act
        accessor.OnCommandStateChanged(this, sourceArgs);

        // Assert
        _ = eventRaised.Should().BeTrue();
        _ = capturedArgs.Should().NotBeNull();
        _ = capturedArgs!.SessionId.Should().Be(TestSessionId); // Should use accessor's session ID
        _ = capturedArgs.CommandId.Should().Be("cmd-123");
        _ = capturedArgs.OldState.Should().Be(CommandState.Queued);
        _ = capturedArgs.NewState.Should().Be(CommandState.Executing);
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed throws when disposed.
    /// </summary>
    [Fact]
    public void ThrowIfDisposed_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => accessor.ThrowIfDisposed());
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed does not throw when not disposed.
    /// </summary>
    [Fact]
    public void ThrowIfDisposed_WhenNotDisposed_DoesNotThrow()
    {
        // Arrange
        using var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert - Should not throw
        accessor.ThrowIfDisposed();
    }

    /// <summary>
    /// Verifies that ThrowIfNotActive throws when state is not Active.
    /// </summary>
    [Fact]
    public void ThrowIfNotActive_WhenNotActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.SetState(SessionState.Closing);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => accessor.ThrowIfNotActive());
        _ = exception.Message.Should().Contain(TestSessionId);
        _ = exception.Message.Should().Contain("not active");
    }

    /// <summary>
    /// Verifies that ThrowIfNotActive does not throw when state is Active.
    /// </summary>
    [Fact]
    public void ThrowIfNotActive_WhenActive_DoesNotThrow()
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.SetState(SessionState.Active);

        // Act & Assert - Should not throw
        accessor.ThrowIfNotActive();
    }



    /// <summary>
    /// Verifies all valid state transitions.
    /// </summary>
    [Theory]
    [InlineData(SessionState.Initializing, SessionState.Active)]
    [InlineData(SessionState.Active, SessionState.Closing)]
    [InlineData(SessionState.Closing, SessionState.Closed)]
    [InlineData(SessionState.Initializing, SessionState.Faulted)]
    [InlineData(SessionState.Active, SessionState.Faulted)]
    public void SetState_ValidTransitions_Succeed(SessionState fromState, SessionState toState)
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.SetState(fromState);

        // Act
        accessor.SetState(toState);

        // Assert
        _ = accessor.State.Should().Be(toState);
    }

    /// <summary>
    /// Verifies that state changes are reflected in IsActive property.
    /// </summary>
    [Theory]
    [InlineData(SessionState.Initializing, false)]
    [InlineData(SessionState.Active, true)]
    [InlineData(SessionState.Closing, false)]
    [InlineData(SessionState.Closed, false)]
    [InlineData(SessionState.Faulted, false)]
    public void IsActive_ReflectsStateChanges(SessionState state, bool expectedIsActive)
    {
        // Arrange
        var accessor = new DebugSessionTestAccessor(TestSessionId, TestDumpFilePath, TestSymbolPath, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        accessor.SetState(state);

        // Assert
        _ = accessor.IsActive.Should().Be(expectedIsActive);
    }
}
