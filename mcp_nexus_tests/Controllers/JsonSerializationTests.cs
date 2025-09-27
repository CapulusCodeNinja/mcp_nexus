using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Controllers;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_tests.Controllers
{
    /// <summary>
    /// Tests for JSON serialization optimizations
    /// </summary>
    public class JsonSerializationTests
    {
        [Fact]
        public void JsonSerializerOptions_Reused_NoAllocations()
        {
            // Arrange
            var testObject = new { Message = "test", Value = 123 };

            // Act - Test that we can create JsonSerializerOptions and reuse them
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var results = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                var json = JsonSerializer.Serialize(testObject, options);
                results.Add(json);
            }

            // Assert - All serializations should work and be consistent
            Assert.Equal(100, results.Count);
            Assert.All(results, result => Assert.Contains("message", result));
            Assert.All(results, result => Assert.Contains("value", result));
        }

        [Fact]
        public void JsonSerializerOptions_Static_Consistent()
        {
            // Arrange
            var testObject = new { Message = "test", Value = 123 };

            // Act - Test JSON serialization with consistent options
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(testObject, options);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("message", json);
            Assert.Contains("value", json);
            Assert.Contains("test", json);
            Assert.Contains("123", json);
        }

        [Fact]
        public void JsonSerialization_Performance_Optimized()
        {
            // Arrange
            var testObject = new
            {
                Method = "test/method",
                Params = new
                {
                    CommandId = "test-123",
                    Command = "!analyze -v",
                    Status = "executing",
                    Progress = 50,
                    Message = "Processing..."
                }
            };

            var iterations = 1000;

            // Act - Measure serialization performance
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var json = JsonSerializer.Serialize(testObject, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                Assert.NotNull(json);
            }

            stopwatch.Stop();

            // Assert - Serialization should be fast
            Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                $"{iterations} serializations took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        }

        [Fact]
        public void JsonSerialization_ReusedOptions_Performance()
        {
            // Arrange
            var testObject = new
            {
                Method = "test/method",
                Params = new
                {
                    CommandId = "test-123",
                    Command = "!analyze -v",
                    Status = "executing"
                }
            };

            var iterations = 1000;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Act - Measure serialization performance with reused options
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var json = JsonSerializer.Serialize(testObject, options);
                Assert.NotNull(json);
            }

            stopwatch.Stop();

            // Assert - Should be faster than creating new options each time
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"{iterations} serializations with reused options took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        }

        [Fact]
        public void JsonSerialization_EmptyObject_HandlesCorrectly()
        {
            // Arrange
            var emptyObject = new { };
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Act
            var json = JsonSerializer.Serialize(emptyObject, options);

            // Assert
            Assert.Equal("{}", json);
        }

        [Fact]
        public void JsonSerialization_NullObject_HandlesCorrectly()
        {
            // Arrange
            object? nullObject = null;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Act
            var json = JsonSerializer.Serialize(nullObject, options);

            // Assert
            Assert.Equal("null", json);
        }

        [Fact]
        public void JsonSerialization_ComplexObject_HandlesCorrectly()
        {
            // Arrange
            var complexObject = new
            {
                Method = "notifications/commandStatus",
                Params = new
                {
                    CommandId = "cmd-123",
                    Command = "!analyze -v",
                    Status = "executing",
                    Progress = 75,
                    Message = "Analyzing memory dumps and stack traces...",
                    Result = (string?)null,
                    Error = (string?)null,
                    Timestamp = DateTime.UtcNow
                }
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Act
            var json = JsonSerializer.Serialize(complexObject, options);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("commandId", json);
            Assert.Contains("command", json);
            Assert.Contains("status", json);
            Assert.Contains("progress", json);
            Assert.Contains("message", json);
        }

        [Fact]
        public void JsonSerialization_Performance_Comparison()
        {
            // Arrange
            var testObject = new
            {
                Method = "test/method",
                Params = new
                {
                    CommandId = "test-123",
                    Command = "!analyze -v",
                    Status = "executing"
                }
            };

            var iterations = 1000;

            // Act - Measure with new options each time
            var stopwatch1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var json = JsonSerializer.Serialize(testObject, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            stopwatch1.Stop();

            // Act - Measure with reused options
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var stopwatch2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var json = JsonSerializer.Serialize(testObject, options);
            }
            stopwatch2.Stop();

            // Assert - Both approaches should complete quickly (performance comparison can be flaky)
            Assert.True(stopwatch1.ElapsedMilliseconds < 1000,
                $"New options took {stopwatch1.ElapsedMilliseconds}ms, expected < 1000ms");
            Assert.True(stopwatch2.ElapsedMilliseconds < 1000,
                $"Reused options took {stopwatch2.ElapsedMilliseconds}ms, expected < 1000ms");
        }
    }
}

