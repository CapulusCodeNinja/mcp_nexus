using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;
using System.Runtime.Versioning;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for ServiceConfiguration
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ServiceConfigurationTests
    {
        private readonly ServiceConfiguration m_Config = new ServiceConfiguration();
        [Fact]
        public void ServiceName_ReturnsExpectedValue()
        {
            // Arrange
            var config = new ServiceConfiguration();

            // Act & Assert
            Assert.Equal("MCP-Nexus", m_Config.ServiceName);
        }

        [Fact]
        public void ServiceDisplayName_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("MCP Nexus Server", m_Config.ServiceDisplayName);
        }

        [Fact]
        public void ServiceDescription_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("Model Context Protocol server providing AI tool integration", m_Config.ServiceDescription);
        }

        [Fact]
        public void InstallFolder_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal(@"C:\Program Files\MCP-Nexus", m_Config.InstallFolder);
        }

        [Fact]
        public void ServiceArguments_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("--service", m_Config.ServiceArguments);
        }

        [Fact]
        public void TimingConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal(2000, m_Config.ServiceStopDelayMs);
            Assert.Equal(3000, m_Config.ServiceStartDelayMs);
            Assert.Equal(3000, m_Config.ServiceDeleteDelayMs);
            Assert.Equal(5000, m_Config.ServiceCleanupDelayMs);
        }

        [Fact]
        public void RetryConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal(3, m_Config.MaxRetryAttempts);
            Assert.Equal(2000, m_Config.RetryDelayMs);
        }

        [Fact]
        public void FileOperationConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal("mcp_nexus.exe", m_Config.ExecutableName);
            Assert.Equal("backups", m_Config.BackupsFolderName);
            Assert.Equal("mcp_nexus.csproj", m_Config.ProjectFileName);
            Assert.Equal("Release", m_Config.BuildConfiguration);
            Assert.Equal(5, m_Config.MaxBackupsToKeep);
        }

        [Fact]
        public void BackupsBaseFolder_ReturnsExpectedPath()
        {
            // Act
            var backupsPath = m_Config.BackupsBaseFolder;

            // Assert
            Assert.NotNull(backupsPath);
            Assert.Contains("MCP-Nexus-Backups", backupsPath);
            Assert.StartsWith(Path.GetTempPath(), backupsPath);
        }

        [Fact]
        public void ExecutablePath_ReturnsExpectedPath()
        {
            // Act
            var executablePath = m_Config.ExecutablePath;

            // Assert
            Assert.NotNull(executablePath);
            Assert.Equal(Path.Combine(m_Config.InstallFolder, m_Config.ExecutableName), executablePath);
        }

        [Fact]
        public void BackupsFolder_ReturnsExpectedPath()
        {
            // Act
            var backupsFolder = m_Config.BackupsFolder;

            // Assert
            Assert.NotNull(backupsFolder);
            Assert.Equal(Path.Combine(m_Config.InstallFolder, m_Config.BackupsFolderName), backupsFolder);
        }

        [Fact]
        public void GetCreateServiceCommand_WithValidPath_ReturnsExpectedCommand()
        {
            // Arrange
            var executablePath = @"C:\Test\mcp_nexus.exe";

            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(m_Config.ServiceName, m_Config.DisplayName, m_Config.Description, executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains("create", command);
            Assert.Contains(m_Config.ServiceName, command);
            Assert.Contains(executablePath, command);
            Assert.Contains(m_Config.ServiceArguments, command);
            Assert.Contains("start= auto", command);
            Assert.Contains(m_Config.ServiceDisplayName, command);
        }

        [Fact]
        public void GetCreateServiceCommand_WithNullPath_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ServiceConfiguration.GetCreateServiceCommand(m_Config.ServiceName, m_Config.DisplayName, m_Config.Description, null!));
        }

        [Fact]
        public void GetCreateServiceCommand_WithEmptyPath_ReturnsCommandWithEmptyPath()
        {
            // Arrange
            var executablePath = "";

            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(m_Config.ServiceName, m_Config.DisplayName, m_Config.Description, executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains("create", command);
            Assert.Contains(m_Config.ServiceName, command);
        }

        [Fact]
        public void GetDeleteServiceCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetDeleteServiceCommand(m_Config.ServiceName);

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"delete \"{m_Config.ServiceName}\"", command);
        }

        [Fact]
        public void GetServiceStartCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceStartCommand(m_Config.ServiceName);

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"start \"{m_Config.ServiceName}\"", command);
        }

        [Fact]
        public void GetServiceStopCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceStopCommand(m_Config.ServiceName);

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"stop \"{m_Config.ServiceName}\"", command);
        }

        [Fact]
        public void GetTimestampedBackupFolder_ReturnsExpectedPath()
        {
            // Act
            var backupFolder = ServiceConfiguration.GetTimestampedBackupFolder(m_Config.BackupsBaseFolder);

            // Assert
            Assert.NotNull(backupFolder);
            Assert.StartsWith(m_Config.BackupsFolder, backupFolder);
            Assert.Contains(DateTime.Now.ToString("yyyyMMdd"), backupFolder);
        }

        [Fact]
        public void GetServiceDescriptionCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceDescriptionCommand(m_Config.ServiceName, m_Config.Description);

            // Assert
            Assert.NotNull(command);
            Assert.Contains("description", command);
            Assert.Contains(m_Config.ServiceName, command);
            Assert.Contains(m_Config.ServiceDescription, command);
        }

        [Theory]
        [InlineData(@"C:\Program Files\MCP-Nexus\mcp_nexus.exe")]
        [InlineData(@"D:\Custom\Path\mcp_nexus.exe")]
        [InlineData(@"mcp_nexus.exe")]
        public void GetCreateServiceCommand_WithVariousPaths_ReturnsValidCommand(string executablePath)
        {
            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(m_Config.ServiceName, m_Config.DisplayName, m_Config.Description, executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains(executablePath, command);
            Assert.Contains(m_Config.ServiceName, command);
        }

        [Fact]
        public void GetTimestampedBackupFolder_ContainsCurrentTimestamp()
        {
            // Arrange
            var before = DateTime.Now;

            // Act
            var backupFolder = ServiceConfiguration.GetTimestampedBackupFolder(m_Config.BackupsBaseFolder);

            // Arrange
            var after = DateTime.Now;

            // Assert
            var timestamp = Path.GetFileName(backupFolder);
            var parsedTimestamp = DateTime.ParseExact(timestamp, "yyyyMMdd_HHmmss", null);
            Assert.True(parsedTimestamp >= before.AddSeconds(-1));
            Assert.True(parsedTimestamp <= after.AddSeconds(1));
        }

        [Fact]
        public void AllConstants_AreNotNullOrEmpty()
        {
            // Act & Assert
            Assert.False(string.IsNullOrEmpty(m_Config.ServiceName));
            Assert.False(string.IsNullOrEmpty(m_Config.ServiceDisplayName));
            Assert.False(string.IsNullOrEmpty(m_Config.ServiceDescription));
            Assert.False(string.IsNullOrEmpty(m_Config.InstallFolder));
            Assert.False(string.IsNullOrEmpty(m_Config.ServiceArguments));
            Assert.False(string.IsNullOrEmpty(m_Config.ExecutableName));
            Assert.False(string.IsNullOrEmpty(m_Config.BackupsFolderName));
            Assert.False(string.IsNullOrEmpty(m_Config.ProjectFileName));
            Assert.False(string.IsNullOrEmpty(m_Config.BuildConfiguration));
        }

        [Fact]
        public void AllNumericConstants_ArePositive()
        {
            // Act & Assert
            Assert.True(m_Config.ServiceStopDelayMs > 0);
            Assert.True(m_Config.ServiceStartDelayMs > 0);
            Assert.True(m_Config.ServiceDeleteDelayMs > 0);
            Assert.True(m_Config.ServiceCleanupDelayMs > 0);
            Assert.True(m_Config.MaxRetryAttempts > 0);
            Assert.True(m_Config.RetryDelayMs > 0);
            Assert.True(m_Config.MaxBackupsToKeep > 0);
        }

        [Fact]
        public void PathProperties_ReturnValidPaths()
        {
            // Act
            var backupsBaseFolder = m_Config.BackupsBaseFolder;
            var executablePath = m_Config.ExecutablePath;
            var backupsFolder = m_Config.BackupsFolder;

            // Assert
            Assert.NotNull(backupsBaseFolder);
            Assert.NotNull(executablePath);
            Assert.NotNull(backupsFolder);

            // These should be valid path characters
            Assert.DoesNotContain(Path.GetInvalidPathChars(), backupsBaseFolder);
            Assert.DoesNotContain(Path.GetInvalidPathChars(), executablePath);
            Assert.DoesNotContain(Path.GetInvalidPathChars(), backupsFolder);
        }
    }
}