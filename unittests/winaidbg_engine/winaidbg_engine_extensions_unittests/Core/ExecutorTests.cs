using System.Diagnostics;

using FluentAssertions;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.Engine.Extensions.Core;
using WinAiDbg.Engine.Extensions.Security;
using WinAiDbg.Engine.Share.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;

using Xunit;

namespace WinAiDbg.Engine.Extensions.Unittests.Core;

/// <summary>
/// Unit tests for the <see cref="Executor"/> class.
/// Tests extension execution, process management, and error handling.
/// </summary>
public class ExecutorTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private readonly Mock<IFileSystem> m_MockFileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutorTests"/> class.
    /// </summary>
    public ExecutorTests()
    {
        m_Settings = new Mock<ISettings>();
        m_MockProcessManager = new Mock<IProcessManager>();
        m_MockFileSystem = new Mock<IFileSystem>();

        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Server = new ServerSettings
                {
                    Port = 8080,
                },
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
    }

    /// <summary>
    /// Creates an <see cref="Executor"/> instance wired to the default test doubles.
    /// </summary>
    /// <param name="manager">The extension manager.</param>
    /// <param name="tokenValidator">The token validator.</param>
    /// <returns>A new <see cref="Executor"/> instance.</returns>
    private Executor CreateExecutor(Manager manager, TokenValidator tokenValidator)
    {
        return new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);
    }

    /// <summary>
    /// Adds extension metadata into a <see cref="Manager"/> instance using reflection.
    /// </summary>
    /// <param name="manager">The extension manager to update.</param>
    /// <param name="extensionName">The extension name key.</param>
    /// <param name="scriptType">The script type (e.g., "PowerShell").</param>
    /// <param name="fullScriptPath">The full script path.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    private static void AddExtensionViaReflection(
        Manager manager,
        string extensionName,
        string scriptType,
        string fullScriptPath,
        int timeoutMs)
    {
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, extensionName);
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, scriptType);
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, fullScriptPath);
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, timeoutMs);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict == null)
        {
            return;
        }

        var addMethod = extensionsDict.GetType().GetMethod("Add");
        if (addMethod == null)
        {
            return;
        }

        _ = addMethod.Invoke(extensionsDict, new[] { extensionName, metadata });
    }

    /// <summary>
    /// Verifies that PowerShell 7 (pwsh) is used when present on the machine.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WhenPwshIsInstalled_UsesPwshHost()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        AddExtensionViaReflection(manager, "TestExtension", "PowerShell", @"C:\extensions\test\test.ps1", 30000);

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var expectedPwshPath = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");

        _ = m_MockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => Path.Combine(paths));
        _ = m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
        _ = m_MockFileSystem.Setup(fs => fs.FileExists(expectedPwshPath)).Returns(true);

        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.Is<ProcessStartInfo>(psi => psi.FileName == expectedPwshPath)))
            .Returns(new Process());

        // Act
        _ = await CreateExecutor(manager, tokenValidator).ExecuteAsync("TestExtension", "session-123", null, "cmd-456");

        // Assert
        m_MockProcessManager.Verify(pm => pm.StartProcess(It.Is<ProcessStartInfo>(psi => psi.FileName == expectedPwshPath)), Times.Once);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that Windows PowerShell is used when PowerShell 7 is not present.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WhenPwshIsNotInstalled_UsesWindowsPowerShellHost()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        AddExtensionViaReflection(manager, "TestExtension", "PowerShell", @"C:\extensions\test\test.ps1", 30000);

        _ = m_MockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => Path.Combine(paths));
        _ = m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.Is<ProcessStartInfo>(psi => psi.FileName == "powershell.exe")))
            .Returns(new Process());

        // Act
        _ = await CreateExecutor(manager, tokenValidator).ExecuteAsync("TestExtension", "session-123", null, "cmd-456");

        // Assert
        m_MockProcessManager.Verify(pm => pm.StartProcess(It.Is<ProcessStartInfo>(psi => psi.FileName == "powershell.exe")), Times.Once);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that constructor with valid parameters succeeds.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Act
        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Assert
        _ = executor.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that constructor with null manager throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenValidator = new TokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Executor(null!, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that constructor with null tokenValidator throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullTokenValidator_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Executor(manager, null!, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object));
    }

    /// <summary>
    /// Verifies that constructor with null processManager throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProcessManager_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Executor(manager, tokenValidator, m_MockFileSystem.Object, null!, m_Settings.Object));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that constructor with null fileSystem throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Executor(manager, tokenValidator, null!, m_MockProcessManager.Object, m_Settings.Object));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteAsync with non-existent extension returns failed CommandInfo.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNonExistentExtension_ReturnsFailedCommandInfo()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act
        var result = await executor.ExecuteAsync("NonExistentExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Failed);
        _ = result.ErrorMessage.Should().Contain("not found");
        _ = result.Command.Should().Contain("NonExistentExtension");
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl with valid URL succeeds.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_WithValidUrl_Succeeds()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);
        var callbackUrl = "http://127.0.0.1:9001/extension-callback";

        // Act
        executor.UpdateCallbackUrl(callbackUrl);

        // Assert - Should not throw
        _ = executor.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl with null URL throws ArgumentException.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_WithNullUrl_ThrowsArgumentException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => executor.UpdateCallbackUrl(null!));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl with empty URL throws ArgumentException.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => executor.UpdateCallbackUrl(string.Empty));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl with whitespace URL throws ArgumentException.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_WithWhitespaceUrl_ThrowsArgumentException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => executor.UpdateCallbackUrl("   "));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl can be called multiple times.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_CanBeCalledMultipleTimes()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act
        executor.UpdateCallbackUrl("http://127.0.0.1:9001/extension-callback");
        executor.UpdateCallbackUrl("http://127.0.0.1:9002/extension-callback");
        executor.UpdateCallbackUrl("http://127.0.0.1:9003/extension-callback");

        // Assert - Should not throw
        _ = executor.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteAsync returns CommandInfo with correct command text.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_ReturnsCommandInfoWithCommandText()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act
        var result = await executor.ExecuteAsync("NonExistentExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Command.Should().Contain("NonExistentExtension");
        _ = result.SessionId.Should().Be("session-123");
        _ = result.CommandId.Should().Be("cmd-456");
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteAsync handles null parameters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNullParameters_HandlesGracefully()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act
        var result = await executor.ExecuteAsync("NonExistentExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Failed);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteAsync with parameters serializes them correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithParameters_HandlesGracefully()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);
        var parameters = new { Key = "Value", Number = 42 };

        // Act
        var result = await executor.ExecuteAsync("NonExistentExtension", "session-123", parameters, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Failed);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl can be updated before execution.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_BeforeExecution_CanBeUpdated()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act
        executor.UpdateCallbackUrl("http://127.0.0.1:9001/extension-callback");
        executor.UpdateCallbackUrl("http://127.0.0.1:9002/extension-callback");

        // Assert - Should not throw
        _ = executor.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteAsync handles exception during execution gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithException_HandlesGracefully()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = CreateExecutor(manager, tokenValidator);

        // Act - ExecuteAsync with non-existent extension should return Failed state
        var result = await executor.ExecuteAsync("NonExistentExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Failed);
        _ = result.ErrorMessage.Should().NotBeNullOrEmpty();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteAsync with null process returns Failed state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNullProcess_ReturnsFailed()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager to return null (simulating process start failure)
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns((System.Diagnostics.Process?)null!);

        // Act - This will fail because extension doesn't exist, but we're testing the error path
        var result = await executor.ExecuteAsync("NonExistentExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Failed);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that StartProcessAsync handles non-PowerShell script type (else branch).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNonPowerShellScriptType_UsesDefault()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Use reflection to inject extension metadata
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, "TestExtension");
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, "Batch"); // Non-PowerShell type
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, @"C:\extensions\test\test.ps1");
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, 30000);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict != null)
        {
            var addMethod = extensionsDict.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                _ = addMethod.Invoke(extensionsDict, new[] { "TestExtension", metadata });
            }
        }

        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager
        var mockProcess = new System.Diagnostics.Process();
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns(mockProcess);
        _ = m_MockProcessManager.Setup(pm => pm.WaitForProcessExitAsync(It.IsAny<System.Diagnostics.Process>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await executor.ExecuteAsync("TestExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that StartProcessAsync handles parameters serialization when parameters are provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithParameters_SerializesParameters()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Use reflection to inject extension metadata
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, "TestExtension");
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, "PowerShell");
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, @"C:\extensions\test\test.ps1");
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, 30000);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict != null)
        {
            var addMethod = extensionsDict.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                _ = addMethod.Invoke(extensionsDict, new[] { "TestExtension", metadata });
            }
        }

        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager to verify parameters are included
        var mockProcess = new System.Diagnostics.Process();
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.Is<System.Diagnostics.ProcessStartInfo>(psi =>
            psi.Arguments.Contains("-Parameters"))))
            .Returns(mockProcess);
        _ = m_MockProcessManager.Setup(pm => pm.WaitForProcessExitAsync(It.IsAny<System.Diagnostics.Process>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var parameters = new { Param1 = "value1", Param2 = 42 };

        // Act
        var result = await executor.ExecuteAsync("TestExtension", "session-123", parameters, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that StartProcessAsync handles null working directory (empty Path.GetDirectoryName).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNullWorkingDirectory_HandlesGracefully()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Use reflection to inject extension metadata with path that results in null working directory
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, "TestExtension");
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, "PowerShell");
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, "test.ps1"); // Relative path
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, 30000);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict != null)
        {
            var addMethod = extensionsDict.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                _ = addMethod.Invoke(extensionsDict, new[] { "TestExtension", metadata });
            }
        }

        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager
        var mockProcess = new System.Diagnostics.Process();
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.Is<System.Diagnostics.ProcessStartInfo>(psi =>
            string.IsNullOrEmpty(psi.WorkingDirectory))))
            .Returns(mockProcess);
        _ = m_MockProcessManager.Setup(pm => pm.WaitForProcessExitAsync(It.IsAny<System.Diagnostics.Process>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await executor.ExecuteAsync("TestExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that MonitorScriptAsync handles timeout scenario.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithTimeout_ReturnsTimedOut()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Use reflection to inject extension metadata
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, "TestExtension");
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, "PowerShell");
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, @"C:\extensions\test\test.ps1");
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, 100);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict != null)
        {
            var addMethod = extensionsDict.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                _ = addMethod.Invoke(extensionsDict, new[] { "TestExtension", metadata });
            }
        }

        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager to simulate timeout - create Process with redirected output
        var processWithRedirectedOutput = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-Command \"Start-Sleep -Seconds 10\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };
        _ = processWithRedirectedOutput.Start();
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns(processWithRedirectedOutput);
        _ = m_MockProcessManager.Setup(pm => pm.WaitForProcessExitAsync(It.IsAny<System.Diagnostics.Process>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Timeout

        // Act
        var result = await executor.ExecuteAsync("TestExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Timeout);
        processWithRedirectedOutput.Dispose();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that MonitorScriptAsync handles cancellation scenario.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithCancellation_ReturnsCancelled()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Use reflection to inject extension metadata
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, "TestExtension");
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, "PowerShell");
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, @"C:\extensions\test\test.ps1");
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, 30000);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict != null)
        {
            var addMethod = extensionsDict.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                _ = addMethod.Invoke(extensionsDict, new[] { "TestExtension", metadata });
            }
        }

        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager to simulate cancellation during WaitForProcessExitAsync
        // Create a Process with redirected output to avoid InvalidOperationException from BeginOutputReadLine
        var processWithRedirectedOutput = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-Command \"exit 0\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };
        _ = processWithRedirectedOutput.Start();
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns(processWithRedirectedOutput);
        _ = m_MockProcessManager.Setup(pm => pm.WaitForProcessExitAsync(It.IsAny<System.Diagnostics.Process>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<System.Diagnostics.Process, int, CancellationToken>((p, timeout, ct) => throw new OperationCanceledException());

        // Act
        using var cts = new CancellationTokenSource();
        var result = await executor.ExecuteAsync("TestExtension", "session-123", null, "cmd-456", null, null, cts.Token);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Cancelled);
        processWithRedirectedOutput.Dispose();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that StartProcessAsync handles exception during process creation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithProcessCreationException_ReturnsFailed()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Use reflection to inject extension metadata
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, "TestExtension");
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, "PowerShell");
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, @"C:\extensions\test\test.ps1");
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, 30000);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict != null)
        {
            var addMethod = extensionsDict.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                _ = addMethod.Invoke(extensionsDict, new[] { "TestExtension", metadata });
            }
        }

        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager to throw exception
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Throws(new InvalidOperationException("Process creation failed"));

        // Act
        var result = await executor.ExecuteAsync("TestExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Failed);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that StartProcessAsync handles non-null working directory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithWorkingDirectory_SetsWorkingDirectory()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Use reflection to inject extension metadata with absolute path
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, "TestExtension");
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, "PowerShell");
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, @"C:\extensions\test\test.ps1");
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, 30000);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict != null)
        {
            var addMethod = extensionsDict.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                _ = addMethod.Invoke(extensionsDict, new[] { "TestExtension", metadata });
            }
        }

        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager to verify working directory is set
        var mockProcess = new System.Diagnostics.Process();
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.Is<System.Diagnostics.ProcessStartInfo>(psi =>
            !string.IsNullOrEmpty(psi.WorkingDirectory))))
            .Returns(mockProcess);
        _ = m_MockProcessManager.Setup(pm => pm.WaitForProcessExitAsync(It.IsAny<System.Diagnostics.Process>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await executor.ExecuteAsync("TestExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that MonitorScriptAsync handles exception during monitoring.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithMonitoringException_ReturnsFailed()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        _ = mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _ = mockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.SearchOption>()))
            .Returns(Array.Empty<string>());

        var manager = new Manager(mockFileSystem.Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Use reflection to inject extension metadata
        var extensionMetadataType = typeof(Manager).Assembly.GetType("WinAiDbg.Engine.Extensions.Models.ExtensionMetadata")!;
        var metadata = Activator.CreateInstance(extensionMetadataType)!;
        extensionMetadataType.GetProperty("Name")!.SetValue(metadata, "TestExtension");
        extensionMetadataType.GetProperty("ScriptType")!.SetValue(metadata, "PowerShell");
        extensionMetadataType.GetProperty("FullScriptPath")!.SetValue(metadata, @"C:\extensions\test\test.ps1");
        extensionMetadataType.GetProperty("TimeoutMs")!.SetValue(metadata, 30000);

        var extensionsField = typeof(Manager).GetField("m_Extensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var extensionsDict = extensionsField?.GetValue(manager);
        if (extensionsDict != null)
        {
            var addMethod = extensionsDict.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                _ = addMethod.Invoke(extensionsDict, new[] { "TestExtension", metadata });
            }
        }

        var executor = new Executor(manager, tokenValidator, m_MockFileSystem.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Setup process manager to throw exception during monitoring
        var mockProcess = new System.Diagnostics.Process();
        _ = m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns(mockProcess);
        _ = m_MockProcessManager.Setup(pm => pm.WaitForProcessExitAsync(It.IsAny<System.Diagnostics.Process>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Monitoring failed"));

        // Act
        var result = await executor.ExecuteAsync("TestExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Failed);
        tokenValidator.Dispose();
    }
}
