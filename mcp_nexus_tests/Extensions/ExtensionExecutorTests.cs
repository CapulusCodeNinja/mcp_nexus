using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Extensions;
using Xunit;

namespace mcp_nexus_tests.Extensions
{
    /// <summary>
    /// Tests for the ExtensionExecutor class.
    /// </summary>
    public class ExtensionExecutorTests : IDisposable
    {
        private readonly Mock<ILogger<ExtensionExecutor>> m_MockLogger;
        private readonly Mock<IExtensionManager> m_MockExtensionManager;
        private readonly string m_CallbackUrl;
        private readonly string m_TestExtensionsPath;
        private readonly string m_TestExtensionPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionExecutorTests"/> class.
        /// </summary>
        public ExtensionExecutorTests()
        {
            m_MockLogger = new Mock<ILogger<ExtensionExecutor>>();
            m_MockExtensionManager = new Mock<IExtensionManager>();
            m_CallbackUrl = "http://localhost:5000/api/extensions/callback";

            // Create temporary test directory
            m_TestExtensionsPath = Path.Combine(Path.GetTempPath(), $"mcp_nexus_test_executor_{Guid.NewGuid()}");
            Directory.CreateDirectory(m_TestExtensionsPath);

            m_TestExtensionPath = Path.Combine(m_TestExtensionsPath, "test_extension");
            Directory.CreateDirectory(m_TestExtensionPath);
        }

        /// <summary>
        /// Cleans up test resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(m_TestExtensionsPath))
                {
                    Directory.Delete(m_TestExtensionsPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Creates a test metadata object with mocked file paths.
        /// </summary>
        private ExtensionMetadata CreateTestMetadata(string name, string scriptFile)
        {
            // Create a realistic metadata object
            // Note: ExtensionMetadata computes FullScriptPath from the metadata file location
            var metadataFilePath = Path.Combine(m_TestExtensionPath, "metadata.json");
            var scriptPath = Path.Combine(m_TestExtensionPath, scriptFile);
            
            // Mock the metadata loading by creating the files
            Directory.CreateDirectory(Path.GetDirectoryName(metadataFilePath)!);
            
            var metadata = new ExtensionMetadata
            {
                Name = name,
                Description = "Test extension",
                Version = "1.0.0",
                Author = "Test Author",
                ScriptType = "powershell",
                ScriptFile = scriptFile,
                Timeout = 60000
            };
            
            // Set the internal paths using reflection or by loading from a real file
            // For simplicity in tests, we'll mock the manager to return this
            return metadata;
        }

        /// <summary>
        /// Creates a simple PowerShell test script.
        /// </summary>
        private void CreateTestScript(string scriptName, string content)
        {
            File.WriteAllText(Path.Combine(m_TestExtensionPath, scriptName), content);
        }

        [Fact]
        public void Constructor_WithValidDependencies_Succeeds()
        {
            // Act
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            // Assert
            Assert.NotNull(executor);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExtensionExecutor(
                    null!,
                    m_MockExtensionManager.Object,
                    m_CallbackUrl));
        }

        [Fact]
        public void Constructor_WithNullExtensionManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExtensionExecutor(
                    m_MockLogger.Object,
                    null!,
                    m_CallbackUrl));
        }

        [Fact]
        public void Constructor_WithNullCallbackUrl_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ExtensionExecutor(
                    m_MockLogger.Object,
                    m_MockExtensionManager.Object,
                    null!));
        }

        [Fact]
        public void Constructor_WithEmptyCallbackUrl_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ExtensionExecutor(
                    m_MockLogger.Object,
                    m_MockExtensionManager.Object,
                    string.Empty));
        }

        [Fact]
        public async Task ExecuteAsync_WithNullExtensionName_ThrowsArgumentException()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                executor.ExecuteAsync(null!, "session-1", null, "cmd-1"));
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyExtensionName_ThrowsArgumentException()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                executor.ExecuteAsync(string.Empty, "session-1", null, "cmd-1"));
        }

        [Fact]
        public async Task ExecuteAsync_WithNullSessionId_ThrowsArgumentException()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                executor.ExecuteAsync("test_extension", null!, null, "cmd-1"));
        }

        [Fact]
        public async Task ExecuteAsync_WithNullCommandId_ThrowsArgumentException()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                executor.ExecuteAsync("test_extension", "session-1", null, null!));
        }

        [Fact]
        public async Task ExecuteAsync_WithNonexistentExtension_ThrowsInvalidOperationException()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            m_MockExtensionManager
                .Setup(em => em.GetExtension("nonexistent"))
                .Returns((ExtensionMetadata?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                executor.ExecuteAsync("nonexistent", "session-1", null, "cmd-1"));
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidExtension_ThrowsInvalidOperationException()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            var metadata = CreateTestMetadata("test_extension", "test.ps1");
            // Don't create the script file

            m_MockExtensionManager
                .Setup(em => em.GetExtension("test_extension"))
                .Returns(metadata);

            m_MockExtensionManager
                .Setup(em => em.ValidateExtension("test_extension"))
                .Returns((false, "Script file does not exist"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                executor.ExecuteAsync("test_extension", "session-1", null, "cmd-1"));
        }

        [Fact]
        public async Task ExecuteAsync_WithSimpleScript_ReturnsResult()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            var scriptContent = @"
Write-Output 'Extension executed successfully'
exit 0
";
            CreateTestScript("test.ps1", scriptContent);

            var metadata = CreateTestMetadata("test_extension", "test.ps1");

            m_MockExtensionManager
                .Setup(em => em.GetExtension("test_extension"))
                .Returns(metadata);

            m_MockExtensionManager
                .Setup(em => em.ValidateExtension("test_extension"))
                .Returns((true, null));

            // Act
            var result = await executor.ExecuteAsync("test_extension", "session-1", null, "cmd-1");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Extension executed successfully", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_WithFailingScript_ReturnsFailure()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            var scriptContent = @"
Write-Error 'Extension failed'
exit 1
";
            CreateTestScript("failing.ps1", scriptContent);

            var metadata = CreateTestMetadata("failing_extension", "failing.ps1");

            m_MockExtensionManager
                .Setup(em => em.GetExtension("failing_extension"))
                .Returns(metadata);

            m_MockExtensionManager
                .Setup(em => em.ValidateExtension("failing_extension"))
                .Returns((true, null));

            // Act
            var result = await executor.ExecuteAsync("failing_extension", "session-1", null, "cmd-1");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(1, result.ExitCode);
        }

        [Fact]
        public async Task ExecuteAsync_WithProgress_CallsProgressCallback()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            var scriptContent = @"
Write-Output '[PROGRESS] Starting extension'
Start-Sleep -Milliseconds 100
Write-Output '[PROGRESS] Extension in progress'
Start-Sleep -Milliseconds 100
Write-Output '[PROGRESS] Extension complete'
exit 0
";
            CreateTestScript("progress.ps1", scriptContent);

            var metadata = CreateTestMetadata("progress_extension", "progress.ps1");

            m_MockExtensionManager
                .Setup(em => em.GetExtension("progress_extension"))
                .Returns(metadata);

            m_MockExtensionManager
                .Setup(em => em.ValidateExtension("progress_extension"))
                .Returns((true, null));

            var progressMessages = new List<string>();

            // Act
            var result = await executor.ExecuteAsync(
                "progress_extension",
                "session-1",
                null,
                "cmd-1",
                message => progressMessages.Add(message));

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(progressMessages);
        }

        [Fact]
        public async Task ExecuteAsync_WithEnvironmentVariables_SetsCorrectVariables()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            var scriptContent = @"
Write-Output ""SessionId: $env:MCP_NEXUS_SESSION_ID""
Write-Output ""CommandId: $env:MCP_NEXUS_COMMAND_ID""
Write-Output ""CallbackUrl: $env:MCP_NEXUS_CALLBACK_URL""
Write-Output ""Token: $($env:MCP_NEXUS_CALLBACK_TOKEN.Length)""
exit 0
";
            CreateTestScript("env.ps1", scriptContent);

            var metadata = CreateTestMetadata("env_extension", "env.ps1");

            m_MockExtensionManager
                .Setup(em => em.GetExtension("env_extension"))
                .Returns(metadata);

            m_MockExtensionManager
                .Setup(em => em.ValidateExtension("env_extension"))
                .Returns((true, null));

            // Act
            var result = await executor.ExecuteAsync("env_extension", "session-1", null, "cmd-1");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Contains("SessionId: session-1", result.Output);
            Assert.Contains("CommandId: cmd-1", result.Output);
            Assert.Contains($"CallbackUrl: {m_CallbackUrl}", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_WithParameters_PassesParametersAsJson()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            var scriptContent = @"
$params = $env:MCP_NEXUS_PARAMETERS | ConvertFrom-Json
Write-Output ""Param1: $($params.param1)""
Write-Output ""Param2: $($params.param2)""
exit 0
";
            CreateTestScript("params.ps1", scriptContent);

            var metadata = CreateTestMetadata("params_extension", "params.ps1");

            m_MockExtensionManager
                .Setup(em => em.GetExtension("params_extension"))
                .Returns(metadata);

            m_MockExtensionManager
                .Setup(em => em.ValidateExtension("params_extension"))
                .Returns((true, null));

            var parameters = new { param1 = "value1", param2 = "value2" };

            // Act
            var result = await executor.ExecuteAsync("params_extension", "session-1", parameters, "cmd-1");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Contains("Param1: value1", result.Output);
            Assert.Contains("Param2: value2", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellation_CancelsExecution()
        {
            // Arrange
            var executor = new ExtensionExecutor(
                m_MockLogger.Object,
                m_MockExtensionManager.Object,
                m_CallbackUrl);

            var scriptContent = @"
Write-Output 'Starting long operation'
Start-Sleep -Seconds 30
Write-Output 'This should not be reached'
exit 0
";
            CreateTestScript("long.ps1", scriptContent);

            var metadata = CreateTestMetadata("long_extension", "long.ps1");

            m_MockExtensionManager
                .Setup(em => em.GetExtension("long_extension"))
                .Returns(metadata);

            m_MockExtensionManager
                .Setup(em => em.ValidateExtension("long_extension"))
                .Returns((true, null));

            var cts = new CancellationTokenSource();
            cts.CancelAfter(500); // Cancel after 500ms

            // Act
            var result = await executor.ExecuteAsync("long_extension", "session-1", null, "cmd-1", null, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("cancelled", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

    }
}

