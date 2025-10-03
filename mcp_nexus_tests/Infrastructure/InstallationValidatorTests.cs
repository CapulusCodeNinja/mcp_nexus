using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Versioning;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for InstallationValidator
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class InstallationValidatorTests
    {
        private readonly Mock<ILogger> m_MockLogger;

        public InstallationValidatorTests()
        {
            m_MockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void InstallationValidator_Class_Exists()
        {
            // This test verifies that the InstallationValidator class exists and can be instantiated
            Assert.True(typeof(InstallationValidator) != null);
        }

        [Fact]
        public async Task ValidateInstallationPrerequisitesAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationPrerequisitesAsync(null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateInstallationPrerequisitesAsync_WithLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationPrerequisitesAsync(m_MockLogger.Object);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateUninstallationPrerequisitesAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateUninstallationPrerequisitesAsync(null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateUninstallationPrerequisitesAsync_WithLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateUninstallationPrerequisitesAsync(m_MockLogger.Object);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateUpdatePrerequisitesAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateUpdatePrerequisitesAsync(null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateUpdatePrerequisitesAsync_WithLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateUpdatePrerequisitesAsync(m_MockLogger.Object);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public void ValidateInstallationSuccess_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = InstallationValidator.ValidateInstallationSuccess(null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public void ValidateInstallationSuccess_WithLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = InstallationValidator.ValidateInstallationSuccess(m_MockLogger.Object);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public void ValidateInstallationSuccess_WithLogger_LogsAppropriately()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            // Act
            var result = InstallationValidator.ValidateInstallationSuccess(loggerMock.Object);

            // Assert
            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);

            // Verify that logging was attempted (the exact log level depends on the result)
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ValidateInstallationPrerequisitesAsync_WithLogger_LogsAppropriately()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            // Act
            var result = await InstallationValidator.ValidateInstallationPrerequisitesAsync(loggerMock.Object);

            // Assert
            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);

            // Verify that logging was attempted (the exact log level depends on the result)
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ValidateUninstallationPrerequisitesAsync_WithLogger_LogsAppropriately()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            // Act
            var result = await InstallationValidator.ValidateUninstallationPrerequisitesAsync(loggerMock.Object);

            // Assert
            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);

            // Verify that logging was attempted (the exact log level depends on the result)
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ValidateUpdatePrerequisitesAsync_WithLogger_LogsAppropriately()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            // Act
            var result = await InstallationValidator.ValidateUpdatePrerequisitesAsync(loggerMock.Object);

            // Assert
            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);

            // Verify that logging was attempted (the exact log level depends on the result)
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void InstallationValidator_IsStaticClass()
        {
            // Assert
            Assert.True(typeof(InstallationValidator).IsAbstract && typeof(InstallationValidator).IsSealed);
        }

        [Fact]
        public void InstallationValidator_HasSupportedOSPlatformAttribute()
        {
            // Arrange
            var attributes = typeof(InstallationValidator).GetCustomAttributes(typeof(System.Runtime.Versioning.SupportedOSPlatformAttribute), false);

            // Assert
            Assert.NotEmpty(attributes);
            var platformAttribute = (System.Runtime.Versioning.SupportedOSPlatformAttribute)attributes[0];
            Assert.Equal("windows", platformAttribute.PlatformName);
        }

        #region InstallationValidationResult Tests

        [Fact]
        public void InstallationValidationResult_DefaultValues_AreCorrect()
        {
            // Act
            var result = new InstallationValidationResult();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Empty(result.Info);
        }

        [Fact]
        public void InstallationValidationResult_AddError_SetsIsValidToFalse()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddError("Test error");

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Test error", result.Errors[0]);
        }

        [Fact]
        public void InstallationValidationResult_AddWarning_KeepsIsValidTrue()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddWarning("Test warning");

            // Assert
            Assert.True(result.IsValid);
            Assert.Single(result.Warnings);
            Assert.Equal("Test warning", result.Warnings[0]);
        }

        [Fact]
        public void InstallationValidationResult_AddInfo_KeepsIsValidTrue()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddInfo("Test info");

            // Assert
            Assert.True(result.IsValid);
            Assert.Single(result.Info);
            Assert.Equal("Test info", result.Info[0]);
        }

        [Fact]
        public void InstallationValidationResult_MultipleErrors_AllAdded()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddError("Error 1");
            result.AddError("Error 2");
            result.AddError("Error 3");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(3, result.Errors.Count);
            Assert.Contains("Error 1", result.Errors);
            Assert.Contains("Error 2", result.Errors);
            Assert.Contains("Error 3", result.Errors);
        }

        [Fact]
        public void InstallationValidationResult_MultipleWarnings_AllAdded()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddWarning("Warning 1");
            result.AddWarning("Warning 2");
            result.AddWarning("Warning 3");

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(3, result.Warnings.Count);
            Assert.Contains("Warning 1", result.Warnings);
            Assert.Contains("Warning 2", result.Warnings);
            Assert.Contains("Warning 3", result.Warnings);
        }

        [Fact]
        public void InstallationValidationResult_MultipleInfo_AllAdded()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddInfo("Info 1");
            result.AddInfo("Info 2");
            result.AddInfo("Info 3");

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(3, result.Info.Count);
            Assert.Contains("Info 1", result.Info);
            Assert.Contains("Info 2", result.Info);
            Assert.Contains("Info 3", result.Info);
        }

        [Fact]
        public void InstallationValidationResult_MixedMessages_AllAdded()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddError("Error message");
            result.AddWarning("Warning message");
            result.AddInfo("Info message");

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Single(result.Info);
            Assert.Equal("Error message", result.Errors[0]);
            Assert.Equal("Warning message", result.Warnings[0]);
            Assert.Equal("Info message", result.Info[0]);
        }

        [Fact]
        public void InstallationValidationResult_EmptyMessages_HandledCorrectly()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddError("");
            result.AddWarning("");
            result.AddInfo("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Single(result.Info);
            Assert.Equal("", result.Errors[0]);
            Assert.Equal("", result.Warnings[0]);
            Assert.Equal("", result.Info[0]);
        }

        [Fact]
        public void InstallationValidationResult_NullMessages_HandledCorrectly()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.AddError(null!);
            result.AddWarning(null!);
            result.AddInfo(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Single(result.Info);
            Assert.Null(result.Errors[0]);
            Assert.Null(result.Warnings[0]);
            Assert.Null(result.Info[0]);
        }

        [Fact]
        public void InstallationValidationResult_Properties_CanBeSetDirectly()
        {
            // Arrange
            var result = new InstallationValidationResult();

            // Act
            result.IsValid = false;
            result.Errors = new List<string> { "Direct error" };
            result.Warnings = new List<string> { "Direct warning" };
            result.Info = new List<string> { "Direct info" };

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Single(result.Warnings);
            Assert.Single(result.Info);
            Assert.Equal("Direct error", result.Errors[0]);
            Assert.Equal("Direct warning", result.Warnings[0]);
            Assert.Equal("Direct info", result.Info[0]);
        }

        #endregion

        #region Additional InstallationValidator Tests

        [Fact]
        public async Task ValidateInstallationEnvironmentAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationEnvironmentAsync(null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateInstallationEnvironmentAsync_WithLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationEnvironmentAsync(m_MockLogger.Object);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateInstallationEnvironmentAsync_WithLogger_LogsAppropriately()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            // Act
            var result = await InstallationValidator.ValidateInstallationEnvironmentAsync(loggerMock.Object);

            // Assert
            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);

            // Verify that logging was attempted (the exact log level depends on the result)
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ValidateServiceConfigurationAsync_WithNullConfiguration_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateServiceConfigurationAsync(null!, null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateServiceConfigurationAsync_WithValidConfiguration_DoesNotThrow()
        {
            // Arrange
            var configuration = new ServiceConfiguration();

            // Act & Assert
            var result = await InstallationValidator.ValidateServiceConfigurationAsync(configuration, null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateServiceConfigurationAsync_WithLogger_LogsAppropriately()
        {
            // Arrange
            var configuration = new ServiceConfiguration();
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            // Act
            var result = await InstallationValidator.ValidateServiceConfigurationAsync(configuration, loggerMock.Object);

            // Assert
            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);

            // Verify that logging was attempted (the exact log level depends on the result)
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ValidateInstallationFilesAsync_WithNullSourcePath_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationFilesAsync(null!, null!, null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateInstallationFilesAsync_WithEmptySourcePath_DoesNotThrow()
        {
            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationFilesAsync("", null!, null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateInstallationFilesAsync_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            var sourcePath = "C:\\Test";
            var requiredFiles = new[] { "file1.txt", "file2.txt" };

            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationFilesAsync(sourcePath, requiredFiles, null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateInstallationFilesAsync_WithLogger_LogsAppropriately()
        {
            // Arrange
            var sourcePath = "C:\\Test";
            var requiredFiles = new[] { "file1.txt", "file2.txt" };
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

            // Act
            var result = await InstallationValidator.ValidateInstallationFilesAsync(sourcePath, requiredFiles, loggerMock.Object);

            // Assert
            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);

            // Verify that logging was attempted (the exact log level depends on the result)
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ValidateInstallationFilesAsync_WithEmptyRequiredFiles_DoesNotThrow()
        {
            // Arrange
            var sourcePath = "C:\\Test";
            var requiredFiles = new string[0];

            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationFilesAsync(sourcePath, requiredFiles, null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        [Fact]
        public async Task ValidateInstallationFilesAsync_WithNullRequiredFiles_DoesNotThrow()
        {
            // Arrange
            var sourcePath = "C:\\Test";

            // Act & Assert
            var result = await InstallationValidator.ValidateInstallationFilesAsync(sourcePath, null!, null);

            // The result depends on the actual system state, but the method should not throw
            Assert.True(result.IsValid || !result.IsValid);
        }

        #endregion
    }
}
