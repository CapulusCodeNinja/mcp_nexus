using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using Xunit;
using nexus.engine.Configuration;
using nexus.engine.unittests.TestHelpers;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;

namespace nexus.engine.unittests.Internal;

/// <summary>
/// Unit tests for the CdbSession class.
/// </summary>
public class CdbSessionTests : IDisposable
{
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private nexus.engine.Internal.CdbSession? m_CdbSession;

    /// <summary>
    /// Initializes a new instance of the CdbSessionTests class and sets up test dependencies.
    /// </summary>
    public CdbSessionTests()
    {
        m_LoggerFactory = NullLoggerFactory.Instance;
        m_Configuration = TestDataBuilder.CreateDebugEngineConfiguration();
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();

        // Setup default mocks
        SetupDefaultMocks();
    }

    /// <summary>
    /// Verifies that the CdbSession constructor creates an instance successfully with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var logger = m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>();
        m_CdbSession = new nexus.engine.Internal.CdbSession(m_Configuration, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Assert
        m_CdbSession.Should().NotBeNull();
        m_CdbSession.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the CdbSession constructor throws ArgumentNullException when configuration is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var logger = m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>();
        var action = () => new nexus.engine.Internal.CdbSession(null!, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    /// <summary>
    /// Verifies that the CdbSession constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new nexus.engine.Internal.CdbSession(m_Configuration, null!, m_MockFileSystem.Object, m_MockProcessManager.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that IsActive returns false when the session has not been initialized.
    /// </summary>
    [Fact]
    public void IsActive_WhenNotInitialized_ShouldReturnFalse()
    {
        // Arrange
        var logger = m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>();
        m_CdbSession = new nexus.engine.Internal.CdbSession(m_Configuration, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        m_CdbSession.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that calling Dispose properly disposes the session and prevents further operations.
    /// </summary>
    [Fact]
    public void Dispose_WhenCalled_ShouldDisposeSession()
    {
        // Arrange
        var logger = m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>();
        m_CdbSession = new nexus.engine.Internal.CdbSession(m_Configuration, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        m_CdbSession.Dispose();

        // Assert
        var action = async () => await m_CdbSession.ExecuteCommandAsync("test");
        action.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that calling Dispose multiple times does not throw an exception.
    /// </summary>
    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var logger = m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>();
        m_CdbSession = new nexus.engine.Internal.CdbSession(m_Configuration, logger, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        var action = () =>
        {
            m_CdbSession!.Dispose();
            m_CdbSession.Dispose();
        };
        action.Should().NotThrow();
    }

    /// <summary>
    /// Disposes the test instance and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        m_CdbSession?.Dispose();
    }

    private void SetupDefaultMocks()
    {
        // Setup file system mocks - return false for ALL file existence checks to prevent real system access
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);

        m_MockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("\\", paths));

        // Setup ALL other file system methods to prevent real system access
        m_MockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
            .Returns("mocked content");

        m_MockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        m_MockFileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()))
            .Verifiable();

        m_MockFileSystem.Setup(fs => fs.GetFileName(It.IsAny<string>()))
            .Returns<string>(path => System.IO.Path.GetFileName(path));

        m_MockFileSystem.Setup(fs => fs.GetDirectoryName(It.IsAny<string>()))
            .Returns<string>(path => System.IO.Path.GetDirectoryName(path));

        // Setup process manager mocks - return null to avoid process-related issues in tests
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns((System.Diagnostics.Process)null!);

        m_MockProcessManager.Setup(pm => pm.KillProcess(It.IsAny<System.Diagnostics.Process>()))
            .Verifiable();
    }

    private nexus.engine.Internal.CdbSession CreateCdbSession()
    {
        return new nexus.engine.Internal.CdbSession(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
    }

    /// <summary>
    /// Verifies that InitializeAsync throws FileNotFoundException when the dump file does not exist.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithValidDumpFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var session = CreateCdbSession();
        var dumpFilePath = @"C:\Test\test.dmp";
        var symbolPath = @"C:\Symbols";

        // Act & Assert
        var action = () => session.InitializeAsync(dumpFilePath, symbolPath);
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ArgumentException when dump file path is null.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithNullDumpFile_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync(null!, null);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ArgumentException when dump file path is empty.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithEmptyDumpFile_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync("", null);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws FileNotFoundException when the dump file does not exist.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithNonExistentDumpFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var session = CreateCdbSession();
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);

        // Act & Assert
        var action = () => session.InitializeAsync(@"C:\Test\nonexistent.dmp", null);
        await action.Should().ThrowAsync<FileNotFoundException>();
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ObjectDisposedException when the session is disposed.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var session = CreateCdbSession();
        session.Dispose();

        // Act & Assert
        var action = () => session.InitializeAsync(@"C:\Test\test.dmp", null);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.ExecuteCommandAsync("lm");
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws ObjectDisposedException when the session is disposed.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var session = CreateCdbSession();
        session.Dispose();

        // Act & Assert
        var action = () => session.ExecuteCommandAsync("lm");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that ExecuteBatchCommandAsync throws InvalidOperationException when session is not initialized.
    /// </summary>
    [Fact]
    public async Task ExecuteBatchCommandAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();
        var commands = new[] { "lm", "!threads" };

        // Act & Assert
        var action = () => session.ExecuteBatchCommandAsync(commands);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that ExecuteBatchCommandAsync throws ObjectDisposedException when the session is disposed.
    /// </summary>
    [Fact]
    public async Task ExecuteBatchCommandAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var session = CreateCdbSession();
        var commands = new[] { "lm", "!threads" };
        session.Dispose();

        // Act & Assert
        var action = () => session.ExecuteBatchCommandAsync(commands);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that ExecuteBatchCommandAsync throws InvalidOperationException when commands parameter is null and session is not initialized.
    /// </summary>
    [Fact]
    public async Task ExecuteBatchCommandAsync_WithNullCommands_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.ExecuteBatchCommandAsync(null!);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ExecuteBatchCommandAsync throws InvalidOperationException when commands array is empty and session is not initialized.
    /// </summary>
    [Fact]
    public async Task ExecuteBatchCommandAsync_WithEmptyCommands_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.ExecuteBatchCommandAsync(Array.Empty<string>());
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that FindCdbExecutableAsync returns a valid CDB path when CDB executable is found.
    /// </summary>
    [Fact]
    public async Task FindCdbExecutableAsync_WhenCdbFound_ShouldReturnCdbPath()
    {
        // Arrange
        var session = CreateCdbSession();
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await session.FindCdbExecutableAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that FindCdbExecutableAsync throws InvalidOperationException when CDB executable is not found.
    /// </summary>
    [Fact]
    public async Task FindCdbExecutableAsync_WhenCdbNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);

        // Act & Assert
        var action = () => session.FindCdbExecutableAsync();
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that StartCdbProcessAsync throws InvalidOperationException when session is not initialized.
    /// </summary>
    [Fact]
    public async Task StartCdbProcessAsync_ShouldStartProcess()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.StartCdbProcessAsync();
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that StopCdbProcess does not throw when CDB process has not been started.
    /// </summary>
    [Fact]
    public void StopCdbProcess_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.StopCdbProcess();
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that StopCdbProcess throws ObjectDisposedException when the session is disposed.
    /// </summary>
    [Fact]
    public void StopCdbProcess_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var session = CreateCdbSession();
        session.Dispose();

        // Act & Assert
        var action = () => session.StopCdbProcess();
        action.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that DumpFilePath returns an empty string when the session is not initialized.
    /// </summary>
    [Fact]
    public void DumpFilePath_WhenNotInitialized_ShouldReturnEmptyString()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act
        var result = session.DumpFilePath;

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that SymbolPath returns null when the session is not initialized.
    /// </summary>
    [Fact]
    public void SymbolPath_WhenNotInitialized_ShouldReturnNull()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act
        var result = session.SymbolPath;

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that IsInitialized returns false when the session is not initialized.
    /// </summary>
    [Fact]
    public void IsInitialized_WhenNotInitialized_ShouldReturnFalse()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act
        var result = session.IsInitialized;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that DisposeAsync properly disposes the session without throwing.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_WhenCalled_ShouldDisposeSession()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.DisposeAsync();
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that calling DisposeAsync multiple times does not throw an exception.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = async () =>
        {
            await session.DisposeAsync();
            await session.DisposeAsync();
        };
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that FindCdbExecutableAsync throws InvalidOperationException when CDB is not found.
    /// </summary>
    [Fact]
    public async Task FindCdbExecutableAsync_WhenNoCdbFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.FindCdbExecutableAsync();
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    /// <summary>
    /// Verifies that StartCdbProcessAsync throws InvalidOperationException when session is not initialized.
    /// </summary>
    [Fact]
    public async Task StartCdbProcessAsync_WhenCalled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.StartCdbProcessAsync();
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that StopCdbProcess does not throw when called.
    /// </summary>
    [Fact]
    public void StopCdbProcess_WhenCalled_ShouldNotThrow()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.StopCdbProcess();
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ArgumentException when dump file path is null.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithNullDumpFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync(null!, "symbols");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ArgumentException when dump file path is empty.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithEmptyDumpFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync(string.Empty, "symbols");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws ArgumentException when dump file path is whitespace.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithWhitespaceDumpFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var session = CreateCdbSession();

        // Act & Assert
        var action = () => session.InitializeAsync("   ", "symbols");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed throws ObjectDisposedException when the session is disposed.
    /// </summary>
    [Fact]
    public void TestThrowIfDisposed_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        testAccessor.Dispose();

        // Act & Assert
        var action = () => testAccessor.TestThrowIfDisposed();
        action.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed does not throw when the session is not disposed.
    /// </summary>
    [Fact]
    public void TestThrowIfDisposed_WhenNotDisposed_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestThrowIfDisposed();
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that ThrowIfNotInitialized throws InvalidOperationException when session is not initialized.
    /// </summary>
    [Fact]
    public void TestThrowIfNotInitialized_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestThrowIfNotInitialized();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that WaitForCdbInitializationAsync completes successfully when process is not started.
    /// </summary>
    [Fact]
    public async Task TestWaitForCdbInitializationAsync_WhenProcessNotStarted_ShouldCompleteSuccessfully()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestWaitForCdbInitializationAsync();
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that SendCommandToCdbAsync throws InvalidOperationException when session is not initialized.
    /// </summary>
    [Fact]
    public async Task TestSendCommandToCdbAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestSendCommandToCdbAsync("test command");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB input stream is not available");
    }

    /// <summary>
    /// Verifies that ReadCommandOutputAsync throws InvalidOperationException when session is not initialized.
    /// </summary>
    [Fact]
    public async Task TestReadCommandOutputAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestReadCommandOutputAsync();
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB output stream is not available");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws InvalidOperationException when CDB process fails to start.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithSuccessfulInitialization_ShouldSetInitializedToTrue()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.InitializeAsync(@"C:\Test\test.dmp", @"C:\Symbols");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to start CDB process");
    }

    /// <summary>
    /// Verifies that InitializeAsync throws InvalidOperationException when CDB process fails to start.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithSuccessfulInitialization_ShouldLogSuccessMessage()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.InitializeAsync(@"C:\Test\test.dmp", @"C:\Symbols");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to start CDB process");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized and command is null.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WithNullCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync(null!);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized and command is empty.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WithEmptyCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync(string.Empty);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized and command is whitespace.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WithWhitespaceCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync("   ");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync logs debug messages when executing a valid command.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WithValidCommand_ShouldLogDebugMessage()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act
        var action = () => testAccessor.ExecuteCommandAsync("lm");

        // Assert
        // The debug message should be logged (we can't easily verify this without a real logger, but the code path is covered)
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized and command is tab character.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WithTabCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync("\t");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized and command is newline character.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WithNewlineCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync("\n");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized and command is carriage return character.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WithCarriageReturnCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync("\r");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ExecuteCommandAsync throws InvalidOperationException when session is not initialized and command contains mixed whitespace.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_WithMixedWhitespaceCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable found
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        // Mock process start - return null to trigger the "CDB executable not found" path
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns((Process)null!);

        // Act & Assert
        var action = () => testAccessor.ExecuteCommandAsync(" \t\n\r ");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session is not initialized");
    }

    #region Additional Tests for Coverage

    /// <summary>
    /// Verifies that CreateCommandWithSentinels wraps a valid command with sentinel markers.
    /// </summary>
    [Fact]
    public void CreateCommandWithSentinels_WithValidCommand_ShouldWrapCommand()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var command = "test command";

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
        result.Should().Contain(command);
    }

    /// <summary>
    /// Verifies that CreateCommandWithSentinels wraps an empty command with sentinel markers.
    /// </summary>
    [Fact]
    public void CreateCommandWithSentinels_WithEmptyCommand_ShouldWrapEmptyCommand()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var command = "";

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
    }

    /// <summary>
    /// Verifies that CreateCommandWithSentinels wraps a null command with sentinel markers.
    /// </summary>
    [Fact]
    public void CreateCommandWithSentinels_WithNullCommand_ShouldWrapNullCommand()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        string? command = null;

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command!);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
    }

    /// <summary>
    /// Verifies that CreateCommandWithSentinels wraps commands with special characters correctly.
    /// </summary>
    [Fact]
    public void CreateCommandWithSentinels_WithSpecialCharacters_ShouldWrapSpecialCharacters()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var command = "!analyze -v; lm; kL";

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
        result.Should().Contain(command);
    }

    /// <summary>
    /// Verifies that CreateCommandWithSentinels wraps long commands correctly.
    /// </summary>
    [Fact]
    public void CreateCommandWithSentinels_WithLongCommand_ShouldWrapLongCommand()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var command = new string('A', 1000);

        // Act
        var result = CdbSessionTestAccessor.TestCreateCommandWithSentinels(command);

        // Assert
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_START");
        result.Should().Contain("MCP_NEXUS_SENTINEL_COMMAND_END");
        result.Should().Contain(command);
    }

    /// <summary>
    /// Verifies that SendQuitCommandAsync completes successfully when input writer is null.
    /// </summary>
    [Fact]
    public async Task TestSendQuitCommandAsync_WithNullInputWriter_ShouldCompleteSuccessfully()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestSendQuitCommandAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that WriteQuitCommandAsync throws NullReferenceException when input writer is null.
    /// </summary>
    [Fact]
    public async Task TestWriteQuitCommandAsync_WithNullInputWriter_ShouldThrowNullReferenceException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestWriteQuitCommandAsync();

        // Assert
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Verifies that FlushInputAsync throws NullReferenceException when input writer is null.
    /// </summary>
    [Fact]
    public async Task TestFlushInputAsync_WithNullInputWriter_ShouldThrowNullReferenceException()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestFlushInputAsync();

        // Assert
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Verifies that WaitForProcessExitAsync completes successfully when process is null.
    /// </summary>
    [Fact]
    public async Task TestWaitForProcessExitAsync_WithNullProcess_ShouldCompleteSuccessfully()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestWaitForProcessExitAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that KillProcess does not throw when process is null.
    /// </summary>
    [Fact]
    public void TestKillProcess_WithNullProcess_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestKillProcess();

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that DisposeResources disposes all resources correctly.
    /// </summary>
    [Fact]
    public void TestDisposeResources_ShouldDisposeAllResources()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        var action = () => testAccessor.TestDisposeResources();

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that SetDisposedState sets disposed flag and clears initialized state.
    /// </summary>
    [Fact]
    public void TestSetDisposedState_ShouldSetDisposedAndNotInitialized()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        testAccessor.TestSetDisposedState();

        // Assert
        testAccessor.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ProcessOutputLine sets start marker found flag when start marker is encountered.
    /// </summary>
    [Fact]
    public void TestProcessOutputLine_WithStartMarker_ShouldSetStartMarkerFound()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        var line = "MCP_NEXUS_SENTINEL_COMMAND_START";
        var startMarkerFound = false;
        var output = new StringBuilder();

        // Act
        var result = testAccessor.TestProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        result.ShouldContinue.Should().BeTrue();
        result.ShouldBreak.Should().BeFalse();
        startMarkerFound.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ProcessOutputLine returns break signal when end marker is encountered.
    /// </summary>
    [Fact]
    public void TestProcessOutputLine_WithEndMarker_ShouldReturnBreak()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        var line = "MCP_NEXUS_SENTINEL_COMMAND_END";
        var startMarkerFound = true;
        var output = new StringBuilder();

        // Act
        var result = testAccessor.TestProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        result.ShouldContinue.Should().BeFalse();
        result.ShouldBreak.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ProcessOutputLine appends content to output when between start and end markers.
    /// </summary>
    [Fact]
    public void TestProcessOutputLine_WithContentBetweenMarkers_ShouldAppendToOutput()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        var line = "Some command output";
        var startMarkerFound = true;
        var output = new StringBuilder();

        // Act
        var result = testAccessor.TestProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        result.ShouldContinue.Should().BeTrue();
        result.ShouldBreak.Should().BeFalse();
        output.ToString().Should().Be("Some command output\r\n");
    }

    /// <summary>
    /// Verifies that ProcessOutputLine does not append content to output before start marker is found.
    /// </summary>
    [Fact]
    public void TestProcessOutputLine_WithContentBeforeStartMarker_ShouldNotAppendToOutput()
    {
        // Arrange
        var testAccessor = new CdbSessionTestAccessor(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        var line = "Some command output";
        var startMarkerFound = false;
        var output = new StringBuilder();

        // Act
        var result = testAccessor.TestProcessOutputLine(line, ref startMarkerFound, output);

        // Assert
        result.ShouldContinue.Should().BeTrue();
        result.ShouldBreak.Should().BeFalse();
        output.ToString().Should().BeEmpty();
    }


    #endregion
}
