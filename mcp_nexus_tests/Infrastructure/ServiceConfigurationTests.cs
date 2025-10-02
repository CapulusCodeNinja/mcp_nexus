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
        private readonly ServiceConfiguration _config = new ServiceConfiguration();
        [Fact]
        public void ServiceName_ReturnsExpectedValue()
        {
            // Arrange
            var config = new ServiceConfiguration();
            
            // Act & Assert
            Assert.Equal("MCP-Nexus", _config.ServiceName);
        }

        [Fact]
        public void ServiceDisplayName_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("MCP Nexus Server", _config.ServiceDisplayName);
        }

        [Fact]
        public void ServiceDescription_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("Model Context Protocol server providing AI tool integration", _config.ServiceDescription);
        }

        [Fact]
        public void InstallFolder_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal(@"C:\Program Files\MCP-Nexus", _config.InstallFolder);
        }

        [Fact]
        public void ServiceArguments_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("--service", _config.ServiceArguments);
        }

        [Fact]
        public void TimingConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal(2000, _config.ServiceStopDelayMs);
            Assert.Equal(3000, _config.ServiceStartDelayMs);
            Assert.Equal(3000, _config.ServiceDeleteDelayMs);
            Assert.Equal(5000, _config.ServiceCleanupDelayMs);
        }

        [Fact]
        public void RetryConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal(3, _config.MaxRetryAttempts);
            Assert.Equal(2000, _config.RetryDelayMs);
        }

        [Fact]
        public void FileOperationConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal("mcp_nexus.exe", _config.ExecutableName);
            Assert.Equal("backups", _config.BackupsFolderName);
            Assert.Equal("mcp_nexus.csproj", _config.ProjectFileName);
            Assert.Equal("Release", _config.BuildConfiguration);
            Assert.Equal(5, _config.MaxBackupsToKeep);
        }

        [Fact]
        public void BackupsBaseFolder_ReturnsExpectedPath()
        {
            // Act
            var backupsPath = _config.BackupsBaseFolder;

            // Assert
            Assert.NotNull(backupsPath);
            Assert.Contains("MCP-Nexus-Backups", backupsPath);
            Assert.StartsWith(Path.GetTempPath(), backupsPath);
        }

        [Fact]
        public void ExecutablePath_ReturnsExpectedPath()
        {
            // Act
            var executablePath = _config.ExecutablePath;

            // Assert
            Assert.NotNull(executablePath);
            Assert.Equal(Path.Combine(_config.InstallFolder, _config.ExecutableName), executablePath);
        }

        [Fact]
        public void BackupsFolder_ReturnsExpectedPath()
        {
            // Act
            var backupsFolder = _config.BackupsFolder;

            // Assert
            Assert.NotNull(backupsFolder);
            Assert.Equal(Path.Combine(_config.InstallFolder, _config.BackupsFolderName), backupsFolder);
        }

        [Fact]
        public void GetCreateServiceCommand_WithValidPath_ReturnsExpectedCommand()
        {
            // Arrange
            var executablePath = @"C:\Test\mcp_nexus.exe";

            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(_config.ServiceName, _config.DisplayName, _config.Description, executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains("create", command);
            Assert.Contains(_config.ServiceName, command);
            Assert.Contains(executablePath, command);
            Assert.Contains(_config.ServiceArguments, command);
            Assert.Contains("start= auto", command);
            Assert.Contains(_config.ServiceDisplayName, command);
        }

        [Fact]
        public void GetCreateServiceCommand_WithNullPath_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ServiceConfiguration.GetCreateServiceCommand(_config.ServiceName, _config.DisplayName, _config.Description, null!));
        }

        [Fact]
        public void GetCreateServiceCommand_WithEmptyPath_ReturnsCommandWithEmptyPath()
        {
            // Arrange
            var executablePath = "";

            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(_config.ServiceName, _config.DisplayName, _config.Description, executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains("create", command);
            Assert.Contains(_config.ServiceName, command);
        }

        [Fact]
        public void GetDeleteServiceCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetDeleteServiceCommand(_config.ServiceName);

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"delete \"{_config.ServiceName}\"", command);
        }

        [Fact]
        public void GetServiceStartCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceStartCommand(_config.ServiceName);

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"start \"{_config.ServiceName}\"", command);
        }

        [Fact]
        public void GetServiceStopCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceStopCommand(_config.ServiceName);

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"stop \"{_config.ServiceName}\"", command);
        }

        [Fact]
        public void GetTimestampedBackupFolder_ReturnsExpectedPath()
        {
            // Act
            var backupFolder = ServiceConfiguration.GetTimestampedBackupFolder(_config.BackupsBaseFolder);

            // Assert
            Assert.NotNull(backupFolder);
            Assert.StartsWith(_config.BackupsFolder, backupFolder);
            Assert.Contains(DateTime.Now.ToString("yyyyMMdd"), backupFolder);
        }

        [Fact]
        public void GetServiceDescriptionCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceDescriptionCommand(_config.ServiceName, _config.Description);

            // Assert
            Assert.NotNull(command);
            Assert.Contains("description", command);
            Assert.Contains(_config.ServiceName, command);
            Assert.Contains(_config.ServiceDescription, command);
        }

        [Theory]
        [InlineData(@"C:\Program Files\MCP-Nexus\mcp_nexus.exe")]
        [InlineData(@"D:\Custom\Path\mcp_nexus.exe")]
        [InlineData(@"mcp_nexus.exe")]
        public void GetCreateServiceCommand_WithVariousPaths_ReturnsValidCommand(string executablePath)
        {
            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(_config.ServiceName, _config.DisplayName, _config.Description, executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains(executablePath, command);
            Assert.Contains(_config.ServiceName, command);
        }

        [Fact]
        public void GetTimestampedBackupFolder_ContainsCurrentTimestamp()
        {
            // Arrange
            var before = DateTime.Now;

            // Act
            var backupFolder = ServiceConfiguration.GetTimestampedBackupFolder(_config.BackupsBaseFolder);

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
            Assert.False(string.IsNullOrEmpty(_config.ServiceName));
            Assert.False(string.IsNullOrEmpty(_config.ServiceDisplayName));
            Assert.False(string.IsNullOrEmpty(_config.ServiceDescription));
            Assert.False(string.IsNullOrEmpty(_config.InstallFolder));
            Assert.False(string.IsNullOrEmpty(_config.ServiceArguments));
            Assert.False(string.IsNullOrEmpty(_config.ExecutableName));
            Assert.False(string.IsNullOrEmpty(_config.BackupsFolderName));
            Assert.False(string.IsNullOrEmpty(_config.ProjectFileName));
            Assert.False(string.IsNullOrEmpty(_config.BuildConfiguration));
        }

        [Fact]
        public void AllNumericConstants_ArePositive()
        {
            // Act & Assert
            Assert.True(_config.ServiceStopDelayMs > 0);
            Assert.True(_config.ServiceStartDelayMs > 0);
            Assert.True(_config.ServiceDeleteDelayMs > 0);
            Assert.True(_config.ServiceCleanupDelayMs > 0);
            Assert.True(_config.MaxRetryAttempts > 0);
            Assert.True(_config.RetryDelayMs > 0);
            Assert.True(_config.MaxBackupsToKeep > 0);
        }

        [Fact]
        public void PathProperties_ReturnValidPaths()
        {
            // Act
            var backupsBaseFolder = _config.BackupsBaseFolder;
            var executablePath = _config.ExecutablePath;
            var backupsFolder = _config.BackupsFolder;

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