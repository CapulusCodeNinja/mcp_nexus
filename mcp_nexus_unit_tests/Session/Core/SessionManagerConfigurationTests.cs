using System;
using System.IO;
using Microsoft.Extensions.Options;
using Xunit;
using mcp_nexus.Session.Core;
using mcp_nexus.Session.Core.Models;

namespace mcp_nexus_unit_tests.Session.Core
{
    /// <summary>
    /// Tests for SessionManagerConfiguration
    /// </summary>
    public class SessionManagerConfigurationTests : IDisposable
    {
        private readonly string m_TestDumpFile;
        private readonly string m_TestSymbolsDir;

        public SessionManagerConfigurationTests()
        {
            // Create temporary test files
            m_TestDumpFile = Path.GetTempFileName();
            m_TestSymbolsDir = Path.Combine(Path.GetTempPath(), $"TestSymbols_{Guid.NewGuid():N}");
            Directory.CreateDirectory(m_TestSymbolsDir);
        }

        public void Dispose()
        {
            // Cleanup test files
            try { File.Delete(m_TestDumpFile); } catch { }
            try { Directory.Delete(m_TestSymbolsDir, true); } catch { }
        }

        [Fact]
        public void SessionManagerConfiguration_Class_Exists()
        {
            // This test verifies that the SessionManagerConfiguration class exists and can be instantiated
            Assert.NotNull(typeof(SessionManagerConfiguration));
        }

        [Fact]
        public void Constructor_WithNullOptions_UsesDefaults()
        {
            // Act
            var config = new SessionManagerConfiguration(null, null);

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.Config);
            Assert.NotNull(config.CdbOptions);
        }

        [Fact]
        public void Constructor_WithValidOptions_UsesProvidedValues()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 5,
                SessionTimeout = TimeSpan.FromMinutes(30),
                CleanupInterval = TimeSpan.FromMinutes(10)
            };
            var cdbOptions = new CdbSessionOptions
            {
                CommandTimeoutMs = 900000 // 15 minutes
            };
            var sessionConfigOptions = Options.Create(sessionConfig);
            var cdbConfigOptions = Options.Create(cdbOptions);

            // Act
            var config = new SessionManagerConfiguration(sessionConfigOptions, cdbConfigOptions);

            // Assert
            Assert.NotNull(config);
            Assert.Equal(5, config.Config.MaxConcurrentSessions);
            Assert.Equal(TimeSpan.FromMinutes(30), config.Config.SessionTimeout);
            Assert.Equal(900000, config.CdbOptions.CommandTimeoutMs);
        }

        [Fact]
        public void ValidateSessionCreation_WithNullDumpPath_ReturnsInvalid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation(null!);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Dump path cannot be null", errorMessage);
        }

        [Fact]
        public void ValidateSessionCreation_WithEmptyDumpPath_ReturnsInvalid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation("");

            // Assert
            Assert.False(isValid);
            Assert.Equal("Dump path cannot be empty or whitespace", errorMessage);
        }

        [Fact]
        public void ValidateSessionCreation_WithWhitespaceDumpPath_ReturnsInvalid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation("   ");

            // Assert
            Assert.False(isValid);
            Assert.Equal("Dump path cannot be empty or whitespace", errorMessage);
        }

        [Fact]
        public void ValidateSessionCreation_WithNonExistentDumpFile_ReturnsInvalid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();
            var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.dmp");

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation(nonExistentPath);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Dump file not found", errorMessage);
            Assert.Contains(nonExistentPath, errorMessage);
        }

        [Fact]
        public void ValidateSessionCreation_WithValidDumpFile_ReturnsValid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation(m_TestDumpFile);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidateSessionCreation_WithValidDumpFileAndNonExistentSymbolsPath_ReturnsInvalid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();
            var nonExistentSymbolsPath = Path.Combine(Path.GetTempPath(), $"nonexistent_symbols_{Guid.NewGuid()}");

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation(m_TestDumpFile, nonExistentSymbolsPath);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Symbols directory not found", errorMessage);
            Assert.Contains(nonExistentSymbolsPath, errorMessage);
        }

        [Fact]
        public void ValidateSessionCreation_WithValidDumpFileAndValidSymbolsPath_ReturnsValid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation(m_TestDumpFile, m_TestSymbolsDir);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidateSessionCreation_WithValidDumpFileAndNullSymbolsPath_ReturnsValid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation(m_TestDumpFile, null);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidateSessionCreation_WithValidDumpFileAndEmptySymbolsPath_ReturnsValid()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act
            var (isValid, errorMessage) = config.ValidateSessionCreation(m_TestDumpFile, "");

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void GenerateSessionId_ReturnsUniqueIds()
        {
            // Act
            var id1 = SessionManagerConfiguration.GenerateSessionId(1);
            var id2 = SessionManagerConfiguration.GenerateSessionId(2);
            var id3 = SessionManagerConfiguration.GenerateSessionId(3);

            // Assert
            Assert.NotEqual(id1, id2);
            Assert.NotEqual(id2, id3);
            Assert.NotEqual(id1, id3);
        }

        [Fact]
        public void GenerateSessionId_ContainsSessionCounter()
        {
            // Act
            var id = SessionManagerConfiguration.GenerateSessionId(42);

            // Assert
            Assert.Contains("sess-000042", id);
        }

        [Fact]
        public void GenerateSessionId_ContainsProcessId()
        {
            // Act
            var id = SessionManagerConfiguration.GenerateSessionId(1);

            // Assert
            Assert.StartsWith("sess-", id);
            Assert.Contains("-", id);
        }

        [Fact]
        public void GenerateSessionId_WithLargeCounter_FormatsCorrectly()
        {
            // Act
            var id = SessionManagerConfiguration.GenerateSessionId(999999);

            // Assert
            Assert.Contains("sess-999999", id);
        }

        [Fact]
        public void ConstructCdbTarget_WithDumpPathOnly_ReturnsValidTarget()
        {
            // Arrange
            SetupMinimalNLogConfiguration();
            var config = new SessionManagerConfiguration();
            var sessionId = "test-session-123";
            var dumpPath = m_TestDumpFile;

            // Act
            var target = config.ConstructCdbTarget(sessionId, dumpPath);

            // Assert
            Assert.NotNull(target);
            Assert.Contains($"-z \"{dumpPath}\"", target);
            Assert.Contains("-lines", target);
            Assert.Contains("-logau", target);
        }

        [Fact]
        public void ConstructCdbTarget_WithSymbolsPath_IncludesSymbolsPath()
        {
            // Arrange
            SetupMinimalNLogConfiguration();
            var config = new SessionManagerConfiguration();
            var sessionId = "test-session-456";
            var dumpPath = m_TestDumpFile;
            var symbolsPath = m_TestSymbolsDir;

            // Act
            var target = config.ConstructCdbTarget(sessionId, dumpPath, symbolsPath);

            // Assert
            Assert.NotNull(target);
            Assert.Contains($"-z \"{dumpPath}\"", target);
            Assert.Contains($"-y \"{symbolsPath}\"", target);
            Assert.Contains("-lines", target);
            Assert.Contains("-logau", target);
        }

        [Fact]
        public void ConstructCdbTarget_WithNullSymbolsPath_OmitsSymbolsPath()
        {
            // Arrange
            SetupMinimalNLogConfiguration();
            var config = new SessionManagerConfiguration();
            var sessionId = "test-session-789";
            var dumpPath = m_TestDumpFile;

            // Act
            var target = config.ConstructCdbTarget(sessionId, dumpPath, null);

            // Assert
            Assert.NotNull(target);
            Assert.Contains($"-z \"{dumpPath}\"", target);
            Assert.DoesNotContain("-y", target);
        }

        /// <summary>
        /// Sets up a minimal NLog configuration for testing ConstructCdbTarget
        /// </summary>
        private static void SetupMinimalNLogConfiguration()
        {
            if (NLog.LogManager.Configuration == null)
            {
                var config = new NLog.Config.LoggingConfiguration();
                var fileTarget = new NLog.Targets.FileTarget("mainFile")
                {
                    FileName = Path.Combine(Path.GetTempPath(), "test-log.log")
                };
                config.AddTarget(fileTarget);
                config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget);
                NLog.LogManager.Configuration = config;
            }
        }

        [Fact]
        public void IsSessionExpired_WithRecentActivity_ReturnsFalse()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                SessionTimeout = TimeSpan.FromMinutes(30)
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));
            var lastActivity = DateTime.Now.AddMinutes(-10);

            // Act
            var isExpired = config.IsSessionExpired(lastActivity);

            // Assert
            Assert.False(isExpired);
        }

        [Fact]
        public void IsSessionExpired_WithOldActivity_ReturnsTrue()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                SessionTimeout = TimeSpan.FromMinutes(30)
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));
            var lastActivity = DateTime.Now.AddMinutes(-45);

            // Act
            var isExpired = config.IsSessionExpired(lastActivity);

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public void IsSessionExpired_WithActivityAtExactTimeout_ReturnsTrue()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                SessionTimeout = TimeSpan.FromMinutes(30)
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));
            var lastActivity = DateTime.Now.AddMinutes(-30).AddSeconds(-1);

            // Act
            var isExpired = config.IsSessionExpired(lastActivity);

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public void WouldExceedSessionLimit_WithCountBelowLimit_ReturnsFalse()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 10
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var wouldExceed = config.WouldExceedSessionLimit(5);

            // Assert
            Assert.False(wouldExceed);
        }

        [Fact]
        public void WouldExceedSessionLimit_WithCountAtLimit_ReturnsTrue()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 10
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var wouldExceed = config.WouldExceedSessionLimit(10);

            // Assert
            Assert.True(wouldExceed);
        }

        [Fact]
        public void WouldExceedSessionLimit_WithCountAboveLimit_ReturnsTrue()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 10
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var wouldExceed = config.WouldExceedSessionLimit(15);

            // Assert
            Assert.True(wouldExceed);
        }

        [Fact]
        public void WouldExceedSessionLimit_WithZeroCount_ReturnsFalse()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 10
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var wouldExceed = config.WouldExceedSessionLimit(0);

            // Assert
            Assert.False(wouldExceed);
        }

        [Fact]
        public void GetCleanupInterval_ReturnsConfiguredValue()
        {
            // Arrange
            var expectedInterval = TimeSpan.FromMinutes(15);
            var sessionConfig = new SessionConfiguration
            {
                CleanupInterval = expectedInterval
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var interval = config.GetCleanupInterval();

            // Assert
            Assert.Equal(expectedInterval, interval);
        }

        [Fact]
        public void GetSessionTimeout_ReturnsConfiguredValue()
        {
            // Arrange
            var expectedTimeout = TimeSpan.FromMinutes(45);
            var sessionConfig = new SessionConfiguration
            {
                SessionTimeout = expectedTimeout
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var timeout = config.GetSessionTimeout();

            // Assert
            Assert.Equal(expectedTimeout, timeout);
        }

        [Fact]
        public void Config_Property_ReturnsNonNullConfiguration()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act & Assert
            Assert.NotNull(config.Config);
        }

        [Fact]
        public void CdbOptions_Property_ReturnsNonNullOptions()
        {
            // Arrange
            var config = new SessionManagerConfiguration();

            // Act & Assert
            Assert.NotNull(config.CdbOptions);
        }

        [Fact]
        public void WouldExceedSessionLimit_WithCurrentCountAtLimit_ReturnsTrue()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 10
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var wouldExceed = config.WouldExceedSessionLimit(10);

            // Assert
            Assert.True(wouldExceed);
        }

        [Fact]
        public void WouldExceedSessionLimit_WithCurrentCountBelowLimit_ReturnsFalse()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 10
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var wouldExceed = config.WouldExceedSessionLimit(5);

            // Assert
            Assert.False(wouldExceed);
        }

        [Fact]
        public void WouldExceedSessionLimit_WithCurrentCountAboveLimit_ReturnsTrue()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 10
            };
            var config = new SessionManagerConfiguration(Options.Create(sessionConfig));

            // Act
            var wouldExceed = config.WouldExceedSessionLimit(15);

            // Assert
            Assert.True(wouldExceed);
        }

        [Fact]
        public void Constructor_WithProvidedOptions_UsesProvidedValues()
        {
            // Arrange
            var sessionConfig = new SessionConfiguration
            {
                MaxConcurrentSessions = 50,
                SessionTimeout = TimeSpan.FromHours(2)
            };
            var cdbOptions = new CdbSessionOptions
            {
                CommandTimeoutMs = 120000
            };

            // Act
            var config = new SessionManagerConfiguration(
                Options.Create(sessionConfig),
                Options.Create(cdbOptions)
            );

            // Assert
            Assert.Equal(50, config.Config.MaxConcurrentSessions);
            Assert.Equal(TimeSpan.FromHours(2), config.Config.SessionTimeout);
            Assert.Equal(120000, config.CdbOptions.CommandTimeoutMs);
        }
    }
}
