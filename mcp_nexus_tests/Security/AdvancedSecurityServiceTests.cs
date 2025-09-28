using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Security;
using Xunit;

namespace mcp_nexus_tests.Security
{
    public class AdvancedSecurityServiceTests
    {
        private readonly Mock<ILogger<AdvancedSecurityService>> m_mockLogger;
        private readonly AdvancedSecurityService m_securityService;

        public AdvancedSecurityServiceTests()
        {
            m_mockLogger = new Mock<ILogger<AdvancedSecurityService>>();
            m_securityService = new AdvancedSecurityService(m_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AdvancedSecurityService(null!));
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AdvancedSecurityService>>();

            // Act
            var service = new AdvancedSecurityService(mockLogger.Object);

            // Assert
            Assert.NotNull(service);
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AdvancedSecurityService initialized")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #region ValidateCommand Tests

        [Fact]
        public void ValidateCommand_WithNullCommand_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateCommand(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Command cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void ValidateCommand_WithEmptyCommand_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateCommand("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Command cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void ValidateCommand_WithWhitespaceCommand_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateCommand("   ");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Command cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void ValidateCommand_WithValidCommand_ReturnsValid()
        {
            // Arrange
            const string command = "version";

            // Act
            var result = m_securityService.ValidateCommand(command);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Command passed security validation")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("format c:")]
        [InlineData("FDISK /MBR")]
        [InlineData("del important.txt")]
        [InlineData("rmdir temp")]
        [InlineData("rd temp")]
        [InlineData("rm -rf /")]
        [InlineData("shutdown -s -t 0")]
        [InlineData("restart now")]
        [InlineData("net user admin password")]
        [InlineData("net localgroup administrators")]
        [InlineData("reg add HKLM\\Software\\Test")]
        [InlineData("reg delete HKLM\\Software\\Test")]
        [InlineData("wmic process list")]
        [InlineData("powershell Get-Process")]
        [InlineData("cmd /c dir")]
        [InlineData("bash -c ls")]
        [InlineData("sh -c pwd")]
        [InlineData("exec notepad.exe")]
        [InlineData("system info")]
        public void ValidateCommand_WithDangerousCommands_ReturnsInvalid(string command)
        {
            // Act
            var result = m_securityService.ValidateCommand(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Potentially dangerous command detected", result.ErrorMessage);
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Security validation failed for command")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("../etc/passwd")]
        [InlineData("..\\windows\\system32")]
        [InlineData("..%2fetc%2fpasswd")]
        [InlineData("..%5cwindows%5csystem32")]
        public void ValidateCommand_WithPathTraversal_ReturnsInvalid(string command)
        {
            // Act
            var result = m_securityService.ValidateCommand(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Path traversal attempt detected", result.ErrorMessage);
        }

        [Theory]
        [InlineData("SELECT * FROM users")]
        [InlineData("UNION SELECT password FROM users")]
        [InlineData("INSERT INTO users VALUES")]
        [InlineData("UPDATE users SET password")]
        [InlineData("DELETE FROM users WHERE")]
        [InlineData("DROP TABLE users")]
        [InlineData("CREATE TABLE test")]
        [InlineData("ALTER TABLE users ADD")]
        [InlineData("EXEC sp_helpdb")]
        [InlineData("EXECUTE sp_helpdb")]
        public void ValidateCommand_WithSqlInjection_ReturnsInvalid(string command)
        {
            // Act
            var result = m_securityService.ValidateCommand(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("SQL injection pattern detected", result.ErrorMessage);
        }

        [Fact]
        public void ValidateCommand_WithLongCommand_ReturnsInvalid()
        {
            // Arrange
            var longCommand = new string('a', 1001); // 1001 characters

            // Act
            var result = m_securityService.ValidateCommand(longCommand);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Command too long (max 1000 characters)", result.ErrorMessage);
        }

        [Fact]
        public void ValidateCommand_WithMultipleIssues_ReturnsAllIssues()
        {
            // Arrange
            const string command = "format c: ../etc/passwd SELECT * FROM users";

            // Act
            var result = m_securityService.ValidateCommand(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Potentially dangerous command detected: format", result.ErrorMessage);
            Assert.Contains("Path traversal attempt detected", result.ErrorMessage);
            Assert.Contains("SQL injection pattern detected", result.ErrorMessage);
        }

        [Fact]
        public void ValidateCommand_TrimsWhitespace()
        {
            // Arrange
            const string command = "  version  ";

            // Act
            var result = m_securityService.ValidateCommand(command);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        #endregion

        #region ValidateFilePath Tests

        [Fact]
        public void ValidateFilePath_WithNullPath_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateFilePath(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("File path cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void ValidateFilePath_WithEmptyPath_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateFilePath("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("File path cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void ValidateFilePath_WithWhitespacePath_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateFilePath("   ");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("File path cannot be empty", result.ErrorMessage);
        }

        [Theory]
        [InlineData("test.dmp")]
        [InlineData("C:\\temp\\test.exe")]
        [InlineData("D:\\debug\\test.dll")]
        [InlineData("E:\\symbols\\test.pdb")]
        [InlineData("C:\\temp\\test.sym")]
        public void ValidateFilePath_WithValidPaths_ReturnsValid(string filePath)
        {
            // Act
            var result = m_securityService.ValidateFilePath(filePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData("../etc/passwd")]
        [InlineData("..\\windows\\system32")]
        [InlineData("..%2fetc%2fpasswd")]
        [InlineData("..%5cwindows%5csystem32")]
        public void ValidateFilePath_WithPathTraversal_ReturnsInvalid(string filePath)
        {
            // Act
            var result = m_securityService.ValidateFilePath(filePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Path traversal attempt detected", result.ErrorMessage);
        }

        [Theory]
        [InlineData("F:\\test.dmp")]
        [InlineData("G:\\test.exe")]
        [InlineData("Z:\\test.dll")]
        public void ValidateFilePath_WithDisallowedRoots_ReturnsInvalid(string filePath)
        {
            // Act
            var result = m_securityService.ValidateFilePath(filePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("File path outside allowed directories", result.ErrorMessage);
        }

        [Theory]
        [InlineData("test.txt")]
        [InlineData("test.bat")]
        [InlineData("test.com")]
        [InlineData("test.scr")]
        public void ValidateFilePath_WithDisallowedExtensions_ReturnsInvalid(string filePath)
        {
            // Act
            var result = m_securityService.ValidateFilePath(filePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("File extension not allowed", result.ErrorMessage);
        }

        [Fact]
        public void ValidateFilePath_WithNoExtension_ReturnsValid()
        {
            // Arrange
            const string filePath = "testfile";

            // Act
            var result = m_securityService.ValidateFilePath(filePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ValidateFilePath_WithMultipleIssues_ReturnsAllIssues()
        {
            // Arrange
            const string filePath = "F:\\test.txt ../etc/passwd.txt";

            // Act
            var result = m_securityService.ValidateFilePath(filePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Path traversal attempt detected", result.ErrorMessage);
            Assert.Contains("File path outside allowed directories", result.ErrorMessage);
            Assert.Contains("File extension not allowed", result.ErrorMessage);
        }

        [Fact]
        public void ValidateFilePath_LogsWarningOnFailure()
        {
            // Arrange
            const string filePath = "F:\\test.txt";

            // Act
            var result = m_securityService.ValidateFilePath(filePath);

            // Assert
            Assert.False(result.IsValid);
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("File path validation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region ValidateSessionId Tests

        [Fact]
        public void ValidateSessionId_WithNullId_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateSessionId(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Session ID cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void ValidateSessionId_WithEmptyId_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateSessionId("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Session ID cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public void ValidateSessionId_WithWhitespaceId_ReturnsInvalid()
        {
            // Act
            var result = m_securityService.ValidateSessionId("   ");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Session ID cannot be empty", result.ErrorMessage);
        }

        [Theory]
        [InlineData("sess-123456-abcdef12-34567890-2024")]
        [InlineData("sess-000000-00000000-00000000-0000")]
        [InlineData("sess-999999-ffffffff-ffffffff-9999")]
        public void ValidateSessionId_WithValidFormat_ReturnsValid(string sessionId)
        {
            // Act
            var result = m_securityService.ValidateSessionId(sessionId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData("sess-12345-abcdef12-34567890-2024")] // Too few digits in first part
        [InlineData("sess-1234567-abcdef12-34567890-2024")] // Too many digits in first part
        [InlineData("sess-123456-abcdef1-34567890-2024")] // Too few hex chars in second part
        [InlineData("sess-123456-abcdef123-34567890-2024")] // Too many hex chars in second part
        [InlineData("sess-123456-abcdef12-3456789-2024")] // Too few hex chars in third part
        [InlineData("sess-123456-abcdef12-345678901-2024")] // Too many hex chars in third part
        [InlineData("sess-123456-abcdef12-34567890-202")] // Too few digits in last part
        [InlineData("sess-123456-abcdef12-34567890-20240")] // Too many digits in last part
        [InlineData("session-123456-abcdef12-34567890-2024")] // Wrong prefix
        [InlineData("sess-123456-abcdef12-34567890-2024-extra")] // Extra characters
        [InlineData("sess-123456-abcdef12-34567890")] // Missing last part
        [InlineData("sess-123456-abcdef12-34567890-2024-")] // Trailing dash
        [InlineData("-sess-123456-abcdef12-34567890-2024")] // Leading dash
        public void ValidateSessionId_WithInvalidFormat_ReturnsInvalid(string sessionId)
        {
            // Act
            var result = m_securityService.ValidateSessionId(sessionId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Invalid session ID format", result.ErrorMessage);
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invalid session ID format")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region SecurityValidationResult Tests

        [Fact]
        public void SecurityValidationResult_Valid_ReturnsCorrectValues()
        {
            // Act
            var result = SecurityValidationResult.Valid();

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_Invalid_ReturnsCorrectValues()
        {
            // Arrange
            const string errorMessage = "Test error message";

            // Act
            var result = SecurityValidationResult.Invalid(errorMessage);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(errorMessage, result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_InvalidWithNullMessage_ReturnsCorrectValues()
        {
            // Act
            var result = SecurityValidationResult.Invalid(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void SecurityValidationResult_InvalidWithEmptyMessage_ReturnsCorrectValues()
        {
            // Act
            var result = SecurityValidationResult.Invalid("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("", result.ErrorMessage);
        }

        #endregion
    }
}
