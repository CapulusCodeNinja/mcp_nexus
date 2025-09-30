using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for ServiceConfiguration
    /// </summary>
    public class ServiceConfigurationTests
    {
        [Fact]
        public void ServiceName_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("MCP-Nexus", ServiceConfiguration.ServiceName);
        }

        [Fact]
        public void ServiceDisplayName_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("MCP Nexus Server", ServiceConfiguration.ServiceDisplayName);
        }

        [Fact]
        public void ServiceDescription_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("Model Context Protocol server providing AI tool integration", ServiceConfiguration.ServiceDescription);
        }

        [Fact]
        public void InstallFolder_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal(@"C:\Program Files\MCP-Nexus", ServiceConfiguration.InstallFolder);
        }

        [Fact]
        public void ServiceArguments_ReturnsExpectedValue()
        {
            // Act & Assert
            Assert.Equal("--service", ServiceConfiguration.ServiceArguments);
        }

        [Fact]
        public void TimingConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal(2000, ServiceConfiguration.ServiceStopDelayMs);
            Assert.Equal(3000, ServiceConfiguration.ServiceStartDelayMs);
            Assert.Equal(3000, ServiceConfiguration.ServiceDeleteDelayMs);
            Assert.Equal(5000, ServiceConfiguration.ServiceCleanupDelayMs);
        }

        [Fact]
        public void RetryConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal(3, ServiceConfiguration.MaxRetryAttempts);
            Assert.Equal(2000, ServiceConfiguration.RetryDelayMs);
        }

        [Fact]
        public void FileOperationConstants_ReturnExpectedValues()
        {
            // Act & Assert
            Assert.Equal("mcp_nexus.exe", ServiceConfiguration.ExecutableName);
            Assert.Equal("backups", ServiceConfiguration.BackupsFolderName);
            Assert.Equal("mcp_nexus.csproj", ServiceConfiguration.ProjectFileName);
            Assert.Equal("Release", ServiceConfiguration.BuildConfiguration);
            Assert.Equal(5, ServiceConfiguration.MaxBackupsToKeep);
        }

        [Fact]
        public void BackupsBaseFolder_ReturnsExpectedPath()
        {
            // Act
            var backupsPath = ServiceConfiguration.BackupsBaseFolder;

            // Assert
            Assert.NotNull(backupsPath);
            Assert.Contains("MCP-Nexus-Backups", backupsPath);
            Assert.True(backupsPath.StartsWith(Path.GetTempPath()));
        }

        [Fact]
        public void ExecutablePath_ReturnsExpectedPath()
        {
            // Act
            var executablePath = ServiceConfiguration.ExecutablePath;

            // Assert
            Assert.NotNull(executablePath);
            Assert.Equal(Path.Combine(ServiceConfiguration.InstallFolder, ServiceConfiguration.ExecutableName), executablePath);
        }

        [Fact]
        public void BackupsFolder_ReturnsExpectedPath()
        {
            // Act
            var backupsFolder = ServiceConfiguration.BackupsFolder;

            // Assert
            Assert.NotNull(backupsFolder);
            Assert.Equal(Path.Combine(ServiceConfiguration.InstallFolder, ServiceConfiguration.BackupsFolderName), backupsFolder);
        }

        [Fact]
        public void GetCreateServiceCommand_WithValidPath_ReturnsExpectedCommand()
        {
            // Arrange
            var executablePath = @"C:\Test\mcp_nexus.exe";

            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains("create", command);
            Assert.Contains(ServiceConfiguration.ServiceName, command);
            Assert.Contains(executablePath, command);
            Assert.Contains(ServiceConfiguration.ServiceArguments, command);
            Assert.Contains("start= auto", command);
            Assert.Contains(ServiceConfiguration.ServiceDisplayName, command);
        }

        [Fact]
        public void GetCreateServiceCommand_WithNullPath_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ServiceConfiguration.GetCreateServiceCommand(null!));
        }

        [Fact]
        public void GetCreateServiceCommand_WithEmptyPath_ReturnsCommandWithEmptyPath()
        {
            // Arrange
            var executablePath = "";

            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains("create", command);
            Assert.Contains(ServiceConfiguration.ServiceName, command);
        }

        [Fact]
        public void GetDeleteServiceCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetDeleteServiceCommand();

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"delete \"{ServiceConfiguration.ServiceName}\"", command);
        }

        [Fact]
        public void GetServiceStartCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceStartCommand();

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"start \"{ServiceConfiguration.ServiceName}\"", command);
        }

        [Fact]
        public void GetServiceStopCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceStopCommand();

            // Assert
            Assert.NotNull(command);
            Assert.Equal($"stop \"{ServiceConfiguration.ServiceName}\"", command);
        }

        [Fact]
        public void GetTimestampedBackupFolder_ReturnsExpectedPath()
        {
            // Act
            var backupFolder = ServiceConfiguration.GetTimestampedBackupFolder();

            // Assert
            Assert.NotNull(backupFolder);
            Assert.True(backupFolder.StartsWith(ServiceConfiguration.BackupsFolder));
            Assert.Contains(DateTime.Now.ToString("yyyyMMdd"), backupFolder);
        }

        [Fact]
        public void GetServiceDescriptionCommand_ReturnsExpectedCommand()
        {
            // Act
            var command = ServiceConfiguration.GetServiceDescriptionCommand();

            // Assert
            Assert.NotNull(command);
            Assert.Contains("description", command);
            Assert.Contains(ServiceConfiguration.ServiceName, command);
            Assert.Contains(ServiceConfiguration.ServiceDescription, command);
        }

        [Theory]
        [InlineData(@"C:\Program Files\MCP-Nexus\mcp_nexus.exe")]
        [InlineData(@"D:\Custom\Path\mcp_nexus.exe")]
        [InlineData(@"mcp_nexus.exe")]
        public void GetCreateServiceCommand_WithVariousPaths_ReturnsValidCommand(string executablePath)
        {
            // Act
            var command = ServiceConfiguration.GetCreateServiceCommand(executablePath);

            // Assert
            Assert.NotNull(command);
            Assert.Contains(executablePath, command);
            Assert.Contains(ServiceConfiguration.ServiceName, command);
        }

        [Fact]
        public void GetTimestampedBackupFolder_ContainsCurrentTimestamp()
        {
            // Arrange
            var before = DateTime.Now;

            // Act
            var backupFolder = ServiceConfiguration.GetTimestampedBackupFolder();

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
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.ServiceName));
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.ServiceDisplayName));
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.ServiceDescription));
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.InstallFolder));
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.ServiceArguments));
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.ExecutableName));
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.BackupsFolderName));
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.ProjectFileName));
            Assert.False(string.IsNullOrEmpty(ServiceConfiguration.BuildConfiguration));
        }

        [Fact]
        public void AllNumericConstants_ArePositive()
        {
            // Act & Assert
            Assert.True(ServiceConfiguration.ServiceStopDelayMs > 0);
            Assert.True(ServiceConfiguration.ServiceStartDelayMs > 0);
            Assert.True(ServiceConfiguration.ServiceDeleteDelayMs > 0);
            Assert.True(ServiceConfiguration.ServiceCleanupDelayMs > 0);
            Assert.True(ServiceConfiguration.MaxRetryAttempts > 0);
            Assert.True(ServiceConfiguration.RetryDelayMs > 0);
            Assert.True(ServiceConfiguration.MaxBackupsToKeep > 0);
        }

        [Fact]
        public void PathProperties_ReturnValidPaths()
        {
            // Act
            var backupsBaseFolder = ServiceConfiguration.BackupsBaseFolder;
            var executablePath = ServiceConfiguration.ExecutablePath;
            var backupsFolder = ServiceConfiguration.BackupsFolder;

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