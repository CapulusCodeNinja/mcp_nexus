using mcp_nexus.Health;
using System.Collections.Generic;
using Xunit;

namespace mcp_nexus_tests.Health
{
    public class HealthCheckResultTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var isHealthy = true;
            var message = "System is healthy";
            var data = new Dictionary<string, object> { { "cpu", 45.5 }, { "memory", 1024 } };

            // Act
            var result = new HealthCheckResult(isHealthy, message, data);

            // Assert
            Assert.Equal(isHealthy, result.IsHealthy);
            Assert.Equal(message, result.Message);
            Assert.Equal(data, result.Data);
            Assert.True(result.Timestamp <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithNullMessage_SetsEmptyString()
        {
            // Arrange
            var isHealthy = false;
            string? message = null;

            // Act
            var result = new HealthCheckResult(isHealthy, message!);

            // Assert
            Assert.Equal(string.Empty, result.Message);
        }

        [Fact]
        public void Constructor_WithNullData_SetsEmptyDictionary()
        {
            // Arrange
            var isHealthy = true;
            var message = "Test message";

            // Act
            var result = new HealthCheckResult(isHealthy, message, null);

            // Assert
            Assert.Empty(result.Data);
        }

        [Fact]
        public void Constructor_WithEmptyData_SetsEmptyDictionary()
        {
            // Arrange
            var isHealthy = true;
            var message = "Test message";
            var data = new Dictionary<string, object>();

            // Act
            var result = new HealthCheckResult(isHealthy, message, data);

            // Assert
            Assert.Empty(result.Data);
        }

        [Fact]
        public void Constructor_WithUnhealthyStatus_SetsCorrectProperties()
        {
            // Arrange
            var isHealthy = false;
            var message = "System is unhealthy";
            var data = new Dictionary<string, object> { { "error", "Database connection failed" } };

            // Act
            var result = new HealthCheckResult(isHealthy, message, data);

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal(message, result.Message);
            Assert.Equal(data, result.Data);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void IsHealthy_ReturnsCorrectValue()
        {
            // Arrange
            var result = new HealthCheckResult(true, "Test");

            // Act & Assert
            Assert.True(result.IsHealthy);
        }

        [Fact]
        public void Message_ReturnsCorrectValue()
        {
            // Arrange
            var expectedMessage = "Custom health message";
            var result = new HealthCheckResult(true, expectedMessage);

            // Act & Assert
            Assert.Equal(expectedMessage, result.Message);
        }

        [Fact]
        public void Timestamp_IsSetToCurrentUtcTime()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = new HealthCheckResult(true, "Test");

            // Assert
            var afterCreation = DateTime.UtcNow;
            Assert.True(result.Timestamp >= beforeCreation);
            Assert.True(result.Timestamp <= afterCreation);
        }

        [Fact]
        public void Data_IsReadOnly()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "key", "value" } };
            var result = new HealthCheckResult(true, "Test", data);

            // Act & Assert
            Assert.True(result.Data is IReadOnlyDictionary<string, object>);
        }

        [Fact]
        public void Data_ReturnsCorrectValues()
        {
            // Arrange
            var data = new Dictionary<string, object> 
            { 
                { "cpu_usage", 75.5 }, 
                { "memory_mb", 2048 },
                { "status", "warning" }
            };
            var result = new HealthCheckResult(true, "Test", data);

            // Act & Assert
            Assert.Equal(3, result.Data.Count);
            Assert.Equal(75.5, result.Data["cpu_usage"]);
            Assert.Equal(2048, result.Data["memory_mb"]);
            Assert.Equal("warning", result.Data["status"]);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Constructor_WithVeryLongMessage_HandlesCorrectly()
        {
            // Arrange
            var longMessage = new string('A', 10000);
            var result = new HealthCheckResult(true, longMessage);

            // Assert
            Assert.Equal(longMessage, result.Message);
        }

        [Fact]
        public void Constructor_WithEmptyStringMessage_HandlesCorrectly()
        {
            // Arrange
            var result = new HealthCheckResult(true, string.Empty);

            // Assert
            Assert.Equal(string.Empty, result.Message);
        }

        [Fact]
        public void Constructor_WithWhitespaceMessage_HandlesCorrectly()
        {
            // Arrange
            var whitespaceMessage = "   \t\n   ";
            var result = new HealthCheckResult(true, whitespaceMessage);

            // Assert
            Assert.Equal(whitespaceMessage, result.Message);
        }

        [Fact]
        public void Constructor_WithUnicodeMessage_HandlesCorrectly()
        {
            // Arrange
            var unicodeMessage = "健康检查通过 ✅";
            var result = new HealthCheckResult(true, unicodeMessage);

            // Assert
            Assert.Equal(unicodeMessage, result.Message);
        }

        [Fact]
        public void Constructor_WithLargeDataDictionary_HandlesCorrectly()
        {
            // Arrange
            var largeData = new Dictionary<string, object>();
            for (int i = 0; i < 1000; i++)
            {
                largeData[$"key_{i}"] = $"value_{i}";
            }
            var result = new HealthCheckResult(true, "Test", largeData);

            // Assert
            Assert.Equal(1000, result.Data.Count);
            Assert.Equal("value_0", result.Data["key_0"]);
            Assert.Equal("value_999", result.Data["key_999"]);
        }

        [Fact]
        public void Constructor_WithNullValuesInData_HandlesCorrectly()
        {
            // Arrange
            var data = new Dictionary<string, object>
            {
                { "null_value", null! },
                { "string_value", "test" },
                { "number_value", 42 }
            };
            var result = new HealthCheckResult(true, "Test", data);

            // Assert
            Assert.Equal(3, result.Data.Count);
            Assert.Null(result.Data["null_value"]);
            Assert.Equal("test", result.Data["string_value"]);
            Assert.Equal(42, result.Data["number_value"]);
        }

        [Fact]
        public void Constructor_WithComplexDataTypes_HandlesCorrectly()
        {
            // Arrange
            var complexData = new Dictionary<string, object>
            {
                { "list", new List<int> { 1, 2, 3 } },
                { "nested_dict", new Dictionary<string, string> { { "inner", "value" } } },
                { "array", new int[] { 4, 5, 6 } },
                { "date", DateTime.UtcNow },
                { "timespan", TimeSpan.FromMinutes(5) }
            };
            var result = new HealthCheckResult(true, "Test", complexData);

            // Assert
            Assert.Equal(5, result.Data.Count);
            Assert.IsType<List<int>>(result.Data["list"]);
            Assert.IsType<Dictionary<string, string>>(result.Data["nested_dict"]);
            Assert.IsType<int[]>(result.Data["array"]);
            Assert.IsType<DateTime>(result.Data["date"]);
            Assert.IsType<TimeSpan>(result.Data["timespan"]);
        }

        [Fact]
        public void Constructor_WithEmptyKeyInData_HandlesCorrectly()
        {
            // Arrange
            var data = new Dictionary<string, object>
            {
                { "", "empty_key_value" },
                { "normal_key", "normal_value" }
            };
            var result = new HealthCheckResult(true, "Test", data);

            // Assert
            Assert.Equal(2, result.Data.Count);
            Assert.Equal("empty_key_value", result.Data[""]);
            Assert.Equal("normal_value", result.Data["normal_key"]);
        }

        [Fact]
        public void Constructor_WithVeryLongKeyInData_HandlesCorrectly()
        {
            // Arrange
            var longKey = new string('K', 1000);
            var data = new Dictionary<string, object>
            {
                { longKey, "long_key_value" }
            };
            var result = new HealthCheckResult(true, "Test", data);

            // Assert
            Assert.Single(result.Data);
            Assert.Equal("long_key_value", result.Data[longKey]);
        }

        [Fact]
        public void Constructor_MultipleInstances_HaveDifferentTimestamps()
        {
            // Arrange & Act
            var result1 = new HealthCheckResult(true, "Test1");
            Thread.Sleep(10); // Small delay to ensure different timestamps
            var result2 = new HealthCheckResult(true, "Test2");

            // Assert
            Assert.True(result2.Timestamp > result1.Timestamp);
        }

        [Fact]
        public void Constructor_WithSpecialCharactersInData_HandlesCorrectly()
        {
            // Arrange
            var data = new Dictionary<string, object>
            {
                { "special_chars", "!@#$%^&*()_+-=[]{}|;':\",./<>?" },
                { "unicode_key_测试", "unicode_value_测试" },
                { "emoji_key_🚀", "emoji_value_🎉" }
            };
            var result = new HealthCheckResult(true, "Test", data);

            // Assert
            Assert.Equal(3, result.Data.Count);
            Assert.Equal("!@#$%^&*()_+-=[]{}|;':\",./<>?", result.Data["special_chars"]);
            Assert.Equal("unicode_value_测试", result.Data["unicode_key_测试"]);
            Assert.Equal("emoji_value_🎉", result.Data["emoji_key_🚀"]);
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void Implements_IHealthCheckResult()
        {
            // Arrange & Act
            var result = new HealthCheckResult(true, "Test");

            // Assert
            Assert.IsAssignableFrom<IHealthCheckResult>(result);
        }

        [Fact]
        public void Interface_Properties_WorkCorrectly()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "test", "value" } };
            var result = new HealthCheckResult(false, "Error message", data);

            // Act
            IHealthCheckResult interfaceResult = result;

            // Assert
            Assert.False(interfaceResult.IsHealthy);
            Assert.Equal("Error message", interfaceResult.Message);
            Assert.True(interfaceResult.Timestamp <= DateTime.UtcNow);
            Assert.Equal(data, interfaceResult.Data);
        }

        #endregion
    }
}
