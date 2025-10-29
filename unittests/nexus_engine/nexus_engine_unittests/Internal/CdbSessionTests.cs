using System.Diagnostics;
using System.Text;

using FluentAssertions;

using Moq;

using Nexus.Engine.Internal;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using Xunit;

namespace Nexus.Engine.Tests.Internal;

/// <summary>
/// Unit tests for the <see cref="CdbSession"/> class.
/// </summary>
public class CdbSessionTests
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private readonly Mock<Process> m_MockProcess;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdbSessionTests"/> class.
    /// </summary>
    public CdbSessionTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();
        m_MockProcess = new Mock<Process>();
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
    /// Creates an initialized CdbSessionTestAccessor for testing.
    /// </summary>
    /// <returns></returns>
    private CdbSessionTestAccessor CreateInitializedAccessor()
    {
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.SetInitializedForTesting(true);
        return accessor;
    }


    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_NullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CdbSession(null!, m_MockProcessManager.Object, CreatePreprocessor());
        _ = act.Should().Throw<ArgumentNullException>().WithParameterName("fileSystem");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when processManager is null.
    /// </summary>
    [Fact]
    public void Constructor_NullProcessManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CdbSession(m_MockFileSystem.Object, null!, CreatePreprocessor());
        _ = act.Should().Throw<ArgumentNullException>().WithParameterName("processManager");
    }

    /// <summary>
    /// Verifies that the constructor initializes properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_InitializesProperties()
    {
        // Act
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Assert
        _ = session.Should().NotBeNull();
        _ = session.IsActive.Should().BeFalse();
        _ = session.IsInitialized.Should().BeFalse();
        _ = session.DumpFilePath.Should().BeEmpty();
        _ = session.SymbolPath.Should().BeNull();
    }



    /// <summary>
    /// Verifies that InitializeAsync throws ArgumentException when dumpFilePath is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InitializeAsync_NullDumpFilePath_ThrowsArgumentException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        var act = async () => await session.InitializeAsync("test-session-id", null!, null);

        // Assert
        _ = await act.Should().ThrowAsync<ArgumentException>().WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ArgumentException when dumpFilePath is empty.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InitializeAsync_EmptyDumpFilePath_ThrowsArgumentException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        var act = async () => await session.InitializeAsync("test-session-id", string.Empty, null);

        // Assert
        _ = await act.Should().ThrowAsync<ArgumentException>().WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ArgumentException when dumpFilePath is whitespace.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InitializeAsync_WhitespaceDumpFilePath_ThrowsArgumentException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        var act = async () => await session.InitializeAsync("test-session-id", "   ", null);

        // Assert
        _ = await act.Should().ThrowAsync<ArgumentException>().WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws FileNotFoundException when dump file does not exist.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InitializeAsync_DumpFileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        var dumpPath = @"C:\test\dump.dmp";
        _ = m_MockFileSystem.Setup(f => f.FileExists(dumpPath)).Returns(false);

        // Act
        var act = async () => await session.InitializeAsync("test-session-id", dumpPath, null);

        // Assert
        _ = await act.Should().ThrowAsync<FileNotFoundException>();
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ObjectDisposedException when session is disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InitializeAsync_DisposedSession_ThrowsObjectDisposedException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        await session.DisposeAsync();

        // Act
        var act = async () => await session.InitializeAsync("test-session-id", @"C:\test\dump.dmp", null);

        // Assert
        _ = await act.Should().ThrowAsync<ObjectDisposedException>();
    }



    /// <summary>
    /// Verifies that ExecuteCommandAsync throws ArgumentException when command is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteCommandAsync_NullCommand_ThrowsArgumentException()
    {
        // Arrange
        var accessor = CreateInitializedAccessor();

        // Act
        var act = async () => await accessor.ExecuteCommandAsync(null!);

        // Assert
        _ = await act.Should().ThrowAsync<ArgumentException>().WithParameterName("command");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws ArgumentException when command is empty.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteCommandAsync_EmptyCommand_ThrowsArgumentException()
    {
        // Arrange
        var accessor = CreateInitializedAccessor();

        // Act
        var act = async () => await accessor.ExecuteCommandAsync(string.Empty);

        // Assert
        _ = await act.Should().ThrowAsync<ArgumentException>().WithParameterName("command");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws ArgumentException when command is whitespace.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteCommandAsync_WhitespaceCommand_ThrowsArgumentException()
    {
        // Arrange
        var accessor = CreateInitializedAccessor();

        // Act
        var act = async () => await accessor.ExecuteCommandAsync("   ");

        // Assert
        _ = await act.Should().ThrowAsync<ArgumentException>().WithParameterName("command");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws ObjectDisposedException when session is disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteCommandAsync_DisposedSession_ThrowsObjectDisposedException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        await session.DisposeAsync();

        // Act
        var act = async () => await session.ExecuteCommandAsync("test");

        // Assert
        _ = await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteCommandAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        var act = async () => await session.ExecuteCommandAsync("test");

        // Assert
        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }



    /// <summary>
    /// Verifies that ExecuteBatchCommandAsync throws ArgumentNullException when commands is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteBatchCommandAsync_NullCommands_ThrowsArgumentNullException()
    {
        // Arrange
        var accessor = CreateInitializedAccessor();

        // Act
        var act = async () => await accessor.ExecuteBatchCommandAsync(null!);

        // Assert
        _ = await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("commands");
    }

    /// <summary>
    /// Verifies that ExecuteBatchCommandAsync throws ArgumentException when commands list is empty.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteBatchCommandAsync_EmptyCommands_ThrowsArgumentException()
    {
        // Arrange
        var accessor = CreateInitializedAccessor();

        // Act
        var act = async () => await accessor.ExecuteBatchCommandAsync(new List<string>());

        // Assert
        _ = await act.Should().ThrowAsync<ArgumentException>().WithParameterName("commands");
    }

    /// <summary>
    /// Verifies that ExecuteBatchCommandAsync throws ObjectDisposedException when session is disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteBatchCommandAsync_DisposedSession_ThrowsObjectDisposedException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        await session.DisposeAsync();

        // Act
        var act = async () => await session.ExecuteBatchCommandAsync(new[] { "test" });

        // Assert
        _ = await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that ExecuteBatchCommandAsync throws InvalidOperationException when session is not initialized.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ExecuteBatchCommandAsync_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        var act = async () => await session.ExecuteBatchCommandAsync(new[] { "test" });

        // Assert
        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }



    /// <summary>
    /// Verifies that FindCdbExecutableAsync throws InvalidOperationException when CDB not found.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task FindCdbExecutableAsync_CdbNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        _ = m_MockFileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        var act = async () => await session.FindCdbExecutableAsync();

        // Assert
        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CDB executable not found*");
    }



    /// <summary>
    /// Verifies that StopCdbProcess throws ObjectDisposedException when session is disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task StopCdbProcess_DisposedSession_ThrowsObjectDisposedException()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        await session.DisposeAsync();

        // Act
        var act = () => session.StopCdbProcess();

        // Assert
        _ = act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that StopCdbProcess handles null process gracefully.
    /// </summary>
    [Fact]
    public void StopCdbProcess_NullProcess_DoesNotThrow()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        var act = () => session.StopCdbProcess();

        // Assert
        _ = act.Should().NotThrow();
    }



    /// <summary>
    /// Verifies that DisposeAsync can be called multiple times without errors.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        await session.DisposeAsync();
        var act = async () => await session.DisposeAsync();

        // Assert
        _ = await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that DisposeAsync marks session as disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task DisposeAsync_MarksSessionAsDisposed()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        await session.DisposeAsync();

        // Assert
        _ = session.IsActive.Should().BeFalse();
        _ = session.IsInitialized.Should().BeFalse();
    }



    /// <summary>
    /// Verifies that Dispose can be called multiple times without errors.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        session.Dispose();
        var act = () => session.Dispose();

        // Assert
        _ = act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Dispose marks session as disposed.
    /// </summary>
    [Fact]
    public void Dispose_MarksSessionAsDisposed()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act
        session.Dispose();

        // Assert
        _ = session.IsActive.Should().BeFalse();
        _ = session.IsInitialized.Should().BeFalse();
    }



    /// <summary>
    /// Verifies that CreateCommandWithSentinels wraps command correctly.
    /// </summary>
    [Fact]
    public void CreateCommandWithSentinels_ValidCommand_ReturnsWrappedCommand()
    {
        // Arrange
        var command = "!analyze -v";

        // Act
        var result = CdbSessionTestAccessor.CreateCommandWithSentinels(command);

        // Assert
        _ = result.Should().Contain("!analyze -v");
        _ = result.Should().Contain(".echo");
        _ = result.Should().Contain(";");
    }

    /// <summary>
    /// Verifies that ProcessOutputLine sets startMarkerFound when start marker is found.
    /// </summary>
    [Fact]
    public void ProcessOutputLine_StartMarkerFound_SetsFlag()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);
        var output = new StringBuilder();
        var startMarkerFound = false;
        var line = "MCP_NEXUS_SENTINEL_COMMAND_START";

        // Act
        var (shouldContinue, shouldBreak) = accessor.ProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        _ = startMarkerFound.Should().BeTrue();
        _ = shouldContinue.Should().BeTrue();
        _ = shouldBreak.Should().BeFalse();
        _ = output.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ProcessOutputLine appends line when between markers.
    /// </summary>
    [Fact]
    public void ProcessOutputLine_BetweenMarkers_AppendsLine()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);
        var output = new StringBuilder();
        var startMarkerFound = true;
        var line = "Some output";

        // Act
        var (shouldContinue, shouldBreak) = accessor.ProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        _ = shouldContinue.Should().BeTrue();
        _ = shouldBreak.Should().BeFalse();
        _ = output.ToString().Should().Contain("Some output");
    }

    /// <summary>
    /// Verifies that ProcessOutputLine breaks when end marker is found.
    /// </summary>
    [Fact]
    public void ProcessOutputLine_EndMarkerFound_BreaksLoop()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);
        var output = new StringBuilder();
        var startMarkerFound = true;
        var line = "MCP_NEXUS_SENTINEL_COMMAND_END";

        // Act
        var (shouldContinue, shouldBreak) = accessor.ProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        _ = shouldContinue.Should().BeFalse();
        _ = shouldBreak.Should().BeTrue();
        _ = output.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ProcessOutputLine ignores lines before start marker.
    /// </summary>
    [Fact]
    public void ProcessOutputLine_BeforeStartMarker_IgnoresLine()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);
        var output = new StringBuilder();
        var startMarkerFound = false;
        var line = "Random output";

        // Act
        var (shouldContinue, shouldBreak) = accessor.ProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        _ = shouldContinue.Should().BeTrue();
        _ = shouldBreak.Should().BeFalse();
        _ = output.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed throws when session is disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task ThrowIfDisposed_DisposedSession_ThrowsObjectDisposedException()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);
        await accessor.DisposeAsync();

        // Act
        var act = () => accessor.ThrowIfDisposed();

        // Assert
        _ = act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed does not throw when session is not disposed.
    /// </summary>
    [Fact]
    public void ThrowIfDisposed_NotDisposed_DoesNotThrow()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        var act = () => accessor.ThrowIfDisposed();

        // Assert
        _ = act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that ThrowIfNotInitialized throws when session is not initialized.
    /// </summary>
    [Fact]
    public void ThrowIfNotInitialized_NotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        var act = () => accessor.ThrowIfNotInitialized();

        // Assert
        _ = act.Should().Throw<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ThrowIfNotInitialized does not throw when session is initialized.
    /// </summary>
    [Fact]
    public void ThrowIfNotInitialized_Initialized_DoesNotThrow()
    {
        // Arrange
        var accessor = CreateInitializedAccessor();

        // Act
        var act = () => accessor.ThrowIfNotInitialized();

        // Assert
        _ = act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that SetDisposedState sets the disposed flag.
    /// </summary>
    [Fact]
    public void SetDisposedState_SetsDisposedFlag()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        accessor.SetDisposedState();

        // Assert
        _ = accessor.IsActive.Should().BeFalse();
        _ = accessor.IsInitialized.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsProcessExited returns false when process is null.
    /// </summary>
    [Fact]
    public void IsProcessExited_NullProcess_ReturnsFalse()
    {
        // Arrange
        var accessor = new CdbSessionTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        var result = accessor.IsProcessExited();

        // Assert
        _ = result.Should().BeFalse();
    }



    /// <summary>
    /// Verifies that IsActive returns false when not initialized.
    /// </summary>
    [Fact]
    public void IsActive_NotInitialized_ReturnsFalse()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act & Assert
        _ = session.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsActive returns false when disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task IsActive_Disposed_ReturnsFalse()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());
        await session.DisposeAsync();

        // Act & Assert
        _ = session.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsInitialized returns false initially.
    /// </summary>
    [Fact]
    public void IsInitialized_Initially_ReturnsFalse()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act & Assert
        _ = session.IsInitialized.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that DumpFilePath is empty initially.
    /// </summary>
    [Fact]
    public void DumpFilePath_Initially_IsEmpty()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act & Assert
        _ = session.DumpFilePath.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that SymbolPath is null initially.
    /// </summary>
    [Fact]
    public void SymbolPath_Initially_IsNull()
    {
        // Arrange
        var session = new CdbSession(m_MockFileSystem.Object, m_MockProcessManager.Object, CreatePreprocessor());

        // Act & Assert
        _ = session.SymbolPath.Should().BeNull();
    }
}
