using System;
using System.Collections.Generic;
using Xunit;
using mcp_nexus.Health;

namespace mcp_nexus_tests.Health
{
    /// <summary>
    /// Tests for HealthStatus and CommandQueueHealthStatus data classes - simple data containers
    /// </summary>
    public class HealthStatusTests
    {
        [Fact]
        public void HealthStatus_DefaultValues_AreCorrect()
        {
            // Act
            var healthStatus = new HealthStatus();

            // Assert
            Assert.Equal("unknown", healthStatus.Status);
            Assert.Equal(DateTime.MinValue, healthStatus.Timestamp);
            Assert.Equal(TimeSpan.Zero, healthStatus.Uptime);
            Assert.Equal(0, healthStatus.MemoryUsage);
            Assert.Equal(0, healthStatus.ActiveSessions);
            Assert.Null(healthStatus.CommandQueue);
            Assert.Equal(0, healthStatus.ProcessId);
            Assert.Equal(string.Empty, healthStatus.MachineName);
            Assert.NotNull(healthStatus.Issues);
            Assert.Empty(healthStatus.Issues);
        }

        [Fact]
        public void HealthStatus_WithValues_SetsProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var uptime = TimeSpan.FromHours(5);
            var commandQueue = new CommandQueueHealthStatus();
            commandQueue.SetStatus(10, 3, 100, 5);
            var issues = new List<string> { "High memory usage", "Slow response time" };

            // Act
            var healthStatus = new HealthStatus();
            healthStatus.SetHealthInfo("healthy", timestamp, uptime, 1024 * 1024 * 500, 5, 12345, "TEST-MACHINE");
            healthStatus.SetCommandQueue(commandQueue);
            foreach (var issue in issues)
            {
                healthStatus.AddIssue(issue);
            }

            // Assert
            Assert.Equal("healthy", healthStatus.Status);
            Assert.Equal(timestamp, healthStatus.Timestamp);
            Assert.Equal(uptime, healthStatus.Uptime);
            Assert.Equal(1024 * 1024 * 500, healthStatus.MemoryUsage);
            Assert.Equal(5, healthStatus.ActiveSessions);
            Assert.Equal(commandQueue, healthStatus.CommandQueue);
            Assert.Equal(12345, healthStatus.ProcessId);
            Assert.Equal("TEST-MACHINE", healthStatus.MachineName);
            Assert.Equal(issues, healthStatus.Issues);
        }

        [Theory]
        [InlineData("healthy")]
        [InlineData("unhealthy")]
        [InlineData("degraded")]
        [InlineData("unknown")]
        [InlineData("")]
        [InlineData("custom-status")]
        public void HealthStatus_Status_CanBeSet(string status)
        {
            // Act
            var healthStatus = new HealthStatus();
            healthStatus.SetHealthInfo(status, DateTime.UtcNow, TimeSpan.Zero, 0, 0, 0, "");

            // Assert
            Assert.Equal(status, healthStatus.Status);
        }

        [Fact]
        public void HealthStatus_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var healthStatus = new HealthStatus();
            healthStatus.SetHealthInfo("healthy", DateTime.UtcNow, TimeSpan.Zero, -1024, -1, -1, "");

            // Assert
            Assert.Equal(-1024, healthStatus.MemoryUsage);
            Assert.Equal(-1, healthStatus.ActiveSessions);
            Assert.Equal(-1, healthStatus.ProcessId);
        }

        [Fact]
        public void HealthStatus_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var healthStatus = new HealthStatus();
            healthStatus.SetHealthInfo("healthy", DateTime.UtcNow, TimeSpan.Zero, long.MaxValue, int.MaxValue, int.MaxValue, "");

            // Assert
            Assert.Equal(long.MaxValue, healthStatus.MemoryUsage);
            Assert.Equal(int.MaxValue, healthStatus.ActiveSessions);
            Assert.Equal(int.MaxValue, healthStatus.ProcessId);
        }

        [Fact]
        public void HealthStatus_WithNullValues_HandlesGracefully()
        {
            // Act
            var healthStatus = new HealthStatus();
            // Note: Properties are read-only, so we can't set them to null
            // The test will need to be updated to check default values

            // Assert
            Assert.Equal("unknown", healthStatus.Status);
            Assert.Equal(string.Empty, healthStatus.MachineName);
            Assert.NotNull(healthStatus.Issues); // Issues is a read-only list, not null
        }

        [Fact]
        public void CommandQueueHealthStatus_DefaultValues_AreCorrect()
        {
            // Act
            var commandQueueHealth = new CommandQueueHealthStatus();

            // Assert
            Assert.Equal(0, commandQueueHealth.QueueSize);
            Assert.Equal(0, commandQueueHealth.ActiveCommands);
            Assert.Equal(0, commandQueueHealth.ProcessedCommands);
            Assert.Equal(0, commandQueueHealth.FailedCommands);
        }

        [Fact]
        public void CommandQueueHealthStatus_WithValues_SetsProperties()
        {
            // Act
            var commandQueueHealth = new CommandQueueHealthStatus();
            commandQueueHealth.SetStatus(15, 5, 1000, 25);

            // Assert
            Assert.Equal(15, commandQueueHealth.QueueSize);
            Assert.Equal(5, commandQueueHealth.ActiveCommands);
            Assert.Equal(1000, commandQueueHealth.ProcessedCommands);
            Assert.Equal(25, commandQueueHealth.FailedCommands);
        }

        [Fact]
        public void CommandQueueHealthStatus_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var commandQueueHealth = new CommandQueueHealthStatus();
            commandQueueHealth.SetStatus(-5, -2, -100, -10);

            // Assert
            Assert.Equal(-5, commandQueueHealth.QueueSize);
            Assert.Equal(-2, commandQueueHealth.ActiveCommands);
            Assert.Equal(-100, commandQueueHealth.ProcessedCommands);
            Assert.Equal(-10, commandQueueHealth.FailedCommands);
        }

        [Fact]
        public void CommandQueueHealthStatus_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var commandQueueHealth = new CommandQueueHealthStatus();
            commandQueueHealth.SetStatus(int.MaxValue, int.MaxValue, long.MaxValue, long.MaxValue);

            // Assert
            Assert.Equal(int.MaxValue, commandQueueHealth.QueueSize);
            Assert.Equal(int.MaxValue, commandQueueHealth.ActiveCommands);
            Assert.Equal(long.MaxValue, commandQueueHealth.ProcessedCommands);
            Assert.Equal(long.MaxValue, commandQueueHealth.FailedCommands);
        }

        [Fact]
        public void CommandQueueHealthStatus_WithZeroValues_HandlesCorrectly()
        {
            // Act
            var commandQueueHealth = new CommandQueueHealthStatus();
            commandQueueHealth.SetStatus(0, 0, 0, 0);

            // Assert
            Assert.Equal(0, commandQueueHealth.QueueSize);
            Assert.Equal(0, commandQueueHealth.ActiveCommands);
            Assert.Equal(0, commandQueueHealth.ProcessedCommands);
            Assert.Equal(0, commandQueueHealth.FailedCommands);
        }
    }
}
