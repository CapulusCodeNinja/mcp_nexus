using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using mcp_nexus.Recovery;

namespace mcp_nexus_tests.Recovery
{
    public class SessionDiagnosticsTests
    {
        [Fact]
        public void SessionDiagnostics_Class_Exists()
        {
            // Act
            var type = typeof(SessionDiagnostics);

            // Assert
            Assert.NotNull(type);
            Assert.True(type.IsClass);
        }

        [Fact]
        public void SessionDiagnostics_DefaultValues_AreCorrect()
        {
            // Act
            var diagnostics = new SessionDiagnostics();

            // Assert
            Assert.False(diagnostics.IsActive);
            Assert.Equal(DateTime.MinValue, diagnostics.LastHealthCheck);
            Assert.Equal(TimeSpan.Zero, diagnostics.TimeSinceLastCheck);
            Assert.False(diagnostics.IsHealthCheckDue);
            Assert.Null(diagnostics.ErrorMessage);
            Assert.NotNull(diagnostics.AdditionalInfo);
            Assert.Empty(diagnostics.AdditionalInfo);
        }

        [Fact]
        public void IsActive_CanBeSetAndRetrieved()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                IsActive = true
            };

            // Assert
            Assert.True(diagnostics.IsActive);
        }

        [Fact]
        public void IsActive_WithFalseValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                IsActive = false
            };

            // Assert
            Assert.False(diagnostics.IsActive);
        }

        [Fact]
        public void LastHealthCheck_CanBeSetAndRetrieved()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var testTime = DateTime.UtcNow;

            // Act
            diagnostics.LastHealthCheck = testTime;

            // Assert
            Assert.Equal(testTime, diagnostics.LastHealthCheck);
        }

        [Fact]
        public void LastHealthCheck_WithMinValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                LastHealthCheck = DateTime.MinValue
            };

            // Assert
            Assert.Equal(DateTime.MinValue, diagnostics.LastHealthCheck);
        }

        [Fact]
        public void LastHealthCheck_WithMaxValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                LastHealthCheck = DateTime.MaxValue
            };

            // Assert
            Assert.Equal(DateTime.MaxValue, diagnostics.LastHealthCheck);
        }

        [Fact]
        public void LastHealthCheck_WithUtcNow_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var now = DateTime.UtcNow;

            // Act
            diagnostics.LastHealthCheck = now;

            // Assert
            Assert.Equal(now, diagnostics.LastHealthCheck);
        }

        [Fact]
        public void LastHealthCheck_WithLocalTime_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var localTime = DateTime.Now;

            // Act
            diagnostics.LastHealthCheck = localTime;

            // Assert
            Assert.Equal(localTime, diagnostics.LastHealthCheck);
        }

        [Fact]
        public void TimeSinceLastCheck_CanBeSetAndRetrieved()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var testSpan = TimeSpan.FromMinutes(30);

            // Act
            diagnostics.TimeSinceLastCheck = testSpan;

            // Assert
            Assert.Equal(testSpan, diagnostics.TimeSinceLastCheck);
        }

        [Fact]
        public void TimeSinceLastCheck_WithZeroValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                TimeSinceLastCheck = TimeSpan.Zero
            };

            // Assert
            Assert.Equal(TimeSpan.Zero, diagnostics.TimeSinceLastCheck);
        }

        [Fact]
        public void TimeSinceLastCheck_WithNegativeValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                TimeSinceLastCheck = TimeSpan.FromMinutes(-5)
            };

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(-5), diagnostics.TimeSinceLastCheck);
        }

        [Fact]
        public void TimeSinceLastCheck_WithMaxValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                TimeSinceLastCheck = TimeSpan.MaxValue
            };

            // Assert
            Assert.Equal(TimeSpan.MaxValue, diagnostics.TimeSinceLastCheck);
        }

        [Fact]
        public void TimeSinceLastCheck_WithMinValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                TimeSinceLastCheck = TimeSpan.MinValue
            };

            // Assert
            Assert.Equal(TimeSpan.MinValue, diagnostics.TimeSinceLastCheck);
        }

        [Fact]
        public void TimeSinceLastCheck_WithVariousValues_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var testSpans = new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(1),
                TimeSpan.FromMilliseconds(500)
            };

            // Act & Assert
            foreach (var span in testSpans)
            {
                diagnostics.TimeSinceLastCheck = span;
                Assert.Equal(span, diagnostics.TimeSinceLastCheck);
            }
        }

        [Fact]
        public void IsHealthCheckDue_CanBeSetAndRetrieved()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                IsHealthCheckDue = true
            };

            // Assert
            Assert.True(diagnostics.IsHealthCheckDue);
        }

        [Fact]
        public void IsHealthCheckDue_WithFalseValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                IsHealthCheckDue = false
            };

            // Assert
            Assert.False(diagnostics.IsHealthCheckDue);
        }

        [Fact]
        public void ErrorMessage_CanBeSetAndRetrieved()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var errorMessage = "Test error message";

            // Act
            diagnostics.ErrorMessage = errorMessage;

            // Assert
            Assert.Equal(errorMessage, diagnostics.ErrorMessage);
        }

        [Fact]
        public void ErrorMessage_WithNullValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                ErrorMessage = null
            };

            // Assert
            Assert.Null(diagnostics.ErrorMessage);
        }

        [Fact]
        public void ErrorMessage_WithEmptyString_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                ErrorMessage = string.Empty
            };

            // Assert
            Assert.Equal(string.Empty, diagnostics.ErrorMessage);
        }

        [Fact]
        public void ErrorMessage_WithWhitespace_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                ErrorMessage = "   "
            };

            // Assert
            Assert.Equal("   ", diagnostics.ErrorMessage);
        }

        [Fact]
        public void ErrorMessage_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var unicodeMessage = "Error: 错误信息 🚨";

            // Act
            diagnostics.ErrorMessage = unicodeMessage;

            // Assert
            Assert.Equal(unicodeMessage, diagnostics.ErrorMessage);
        }

        [Fact]
        public void ErrorMessage_WithVeryLongString_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var longMessage = new string('A', 10000);

            // Act
            diagnostics.ErrorMessage = longMessage;

            // Assert
            Assert.Equal(longMessage, diagnostics.ErrorMessage);
        }

        [Fact]
        public void AdditionalInfo_CanBeSetAndRetrieved()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var testInfo = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "key3", true }
            };

            // Act
            diagnostics.AdditionalInfo = testInfo;

            // Assert
            Assert.Equal(testInfo, diagnostics.AdditionalInfo);
            Assert.Equal(3, diagnostics.AdditionalInfo.Count);
            Assert.Equal("value1", diagnostics.AdditionalInfo["key1"]);
            Assert.Equal(42, diagnostics.AdditionalInfo["key2"]);
            Assert.Equal(true, diagnostics.AdditionalInfo["key3"]);
        }

        [Fact]
        public void AdditionalInfo_WithEmptyDictionary_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                AdditionalInfo = []
            };

            // Assert
            Assert.NotNull(diagnostics.AdditionalInfo);
            Assert.Empty(diagnostics.AdditionalInfo);
        }

        [Fact]
        public void AdditionalInfo_WithNullValue_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                AdditionalInfo = null!
            };

            // Assert
            Assert.Null(diagnostics.AdditionalInfo);
        }
        private static readonly int[] value = new[] { 1, 2, 3 };

        [Fact]
        public void AdditionalInfo_WithComplexObjects_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var complexInfo = new Dictionary<string, object>
            {
                { "string", "test" },
                { "int", 123 },
                { "double", 45.67 },
                { "bool", true },
                { "array", value },
                { "nested", new Dictionary<string, object> { { "inner", "value" } } }
            };

            // Act
            diagnostics.AdditionalInfo = complexInfo;

            // Assert
            Assert.Equal(complexInfo, diagnostics.AdditionalInfo);
            Assert.Equal(6, diagnostics.AdditionalInfo.Count);
        }

        [Fact]
        public void AdditionalInfo_WithUnicodeKeys_HandlesCorrectly()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics();
            var unicodeInfo = new Dictionary<string, object>
            {
                { "测试", "value" },
                { "🔍", "emoji" },
                { "key with spaces", "value" }
            };

            // Act
            diagnostics.AdditionalInfo = unicodeInfo;

            // Assert
            Assert.Equal(unicodeInfo, diagnostics.AdditionalInfo);
            Assert.Equal(3, diagnostics.AdditionalInfo.Count);
        }

        [Fact]
        public void AllProperties_CanBeSetIndependently()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act
                IsActive = true,
                LastHealthCheck = DateTime.UtcNow,
                TimeSinceLastCheck = TimeSpan.FromMinutes(5),
                IsHealthCheckDue = true,
                ErrorMessage = "Test error",
                AdditionalInfo = new Dictionary<string, object> { { "test", "value" } }
            };

            // Assert
            Assert.True(diagnostics.IsActive);
            Assert.NotEqual(DateTime.MinValue, diagnostics.LastHealthCheck);
            Assert.Equal(TimeSpan.FromMinutes(5), diagnostics.TimeSinceLastCheck);
            Assert.True(diagnostics.IsHealthCheckDue);
            Assert.Equal("Test error", diagnostics.ErrorMessage);
            Assert.Single(diagnostics.AdditionalInfo);
        }

        [Fact]
        public void AllProperties_CanBeSetMultipleTimes()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                // Act & Assert
                IsActive = true
            };
            Assert.True(diagnostics.IsActive);
            diagnostics.IsActive = false;
            Assert.False(diagnostics.IsActive);

            var time1 = DateTime.UtcNow;
            var time2 = time1.AddMinutes(1);
            diagnostics.LastHealthCheck = time1;
            Assert.Equal(time1, diagnostics.LastHealthCheck);
            diagnostics.LastHealthCheck = time2;
            Assert.Equal(time2, diagnostics.LastHealthCheck);

            var span1 = TimeSpan.FromMinutes(1);
            var span2 = TimeSpan.FromMinutes(2);
            diagnostics.TimeSinceLastCheck = span1;
            Assert.Equal(span1, diagnostics.TimeSinceLastCheck);
            diagnostics.TimeSinceLastCheck = span2;
            Assert.Equal(span2, diagnostics.TimeSinceLastCheck);

            diagnostics.IsHealthCheckDue = true;
            Assert.True(diagnostics.IsHealthCheckDue);
            diagnostics.IsHealthCheckDue = false;
            Assert.False(diagnostics.IsHealthCheckDue);

            diagnostics.ErrorMessage = "Error 1";
            Assert.Equal("Error 1", diagnostics.ErrorMessage);
            diagnostics.ErrorMessage = "Error 2";
            Assert.Equal("Error 2", diagnostics.ErrorMessage);

            var info1 = new Dictionary<string, object> { { "key1", "value1" } };
            var info2 = new Dictionary<string, object> { { "key2", "value2" } };
            diagnostics.AdditionalInfo = info1;
            Assert.Equal(info1, diagnostics.AdditionalInfo);
            diagnostics.AdditionalInfo = info2;
            Assert.Equal(info2, diagnostics.AdditionalInfo);
        }

        [Fact]
        public void MultipleInstances_AreIndependent()
        {
            // Arrange
            var diagnostics1 = new SessionDiagnostics();
            var diagnostics2 = new SessionDiagnostics();

            // Act
            diagnostics1.IsActive = true;
            diagnostics1.LastHealthCheck = DateTime.UtcNow;
            diagnostics1.TimeSinceLastCheck = TimeSpan.FromMinutes(5);
            diagnostics1.IsHealthCheckDue = true;
            diagnostics1.ErrorMessage = "Error 1";
            diagnostics1.AdditionalInfo = new Dictionary<string, object> { { "key1", "value1" } };

            diagnostics2.IsActive = false;
            diagnostics2.LastHealthCheck = DateTime.MinValue;
            diagnostics2.TimeSinceLastCheck = TimeSpan.Zero;
            diagnostics2.IsHealthCheckDue = false;
            diagnostics2.ErrorMessage = "Error 2";
            diagnostics2.AdditionalInfo = new Dictionary<string, object> { { "key2", "value2" } };

            // Assert
            Assert.True(diagnostics1.IsActive);
            Assert.NotEqual(DateTime.MinValue, diagnostics1.LastHealthCheck);
            Assert.Equal(TimeSpan.FromMinutes(5), diagnostics1.TimeSinceLastCheck);
            Assert.True(diagnostics1.IsHealthCheckDue);
            Assert.Equal("Error 1", diagnostics1.ErrorMessage);
            Assert.Single(diagnostics1.AdditionalInfo);

            Assert.False(diagnostics2.IsActive);
            Assert.Equal(DateTime.MinValue, diagnostics2.LastHealthCheck);
            Assert.Equal(TimeSpan.Zero, diagnostics2.TimeSinceLastCheck);
            Assert.False(diagnostics2.IsHealthCheckDue);
            Assert.Equal("Error 2", diagnostics2.ErrorMessage);
            Assert.Single(diagnostics2.AdditionalInfo);
        }

        [Fact]
        public void SessionDiagnostics_ClassCharacteristics_AreCorrect()
        {
            // Arrange
            var type = typeof(SessionDiagnostics);

            // Assert
            Assert.True(type.IsClass);
            Assert.False(type.IsSealed);
            Assert.False(type.IsAbstract);
            Assert.False(type.IsInterface);
            Assert.False(type.IsEnum);
            Assert.False(type.IsValueType);
        }

        [Fact]
        public void SessionDiagnostics_CanBeUsedInCollections()
        {
            // Arrange
            var diagnostics1 = new SessionDiagnostics { IsActive = true };
            var diagnostics2 = new SessionDiagnostics { IsActive = false };
            var diagnostics3 = new SessionDiagnostics { IsActive = true };

            // Act
            var list = new List<SessionDiagnostics> { diagnostics1, diagnostics2, diagnostics3 };
            var activeCount = list.Count(d => d.IsActive);

            // Assert
            Assert.Equal(3, list.Count);
            Assert.Equal(2, activeCount);
        }

        [Fact]
        public void SessionDiagnostics_CanBeSerialized()
        {
            // Arrange
            var diagnostics = new SessionDiagnostics
            {
                IsActive = true,
                LastHealthCheck = DateTime.UtcNow,
                TimeSinceLastCheck = TimeSpan.FromMinutes(30),
                IsHealthCheckDue = true,
                ErrorMessage = "Test error",
                AdditionalInfo = new Dictionary<string, object> { { "test", "value" } }
            };

            // Act & Assert
            var exception = Record.Exception(() =>
            {
                var json = JsonSerializer.Serialize(diagnostics);
                var deserialized = JsonSerializer.Deserialize<SessionDiagnostics>(json);
                Assert.NotNull(deserialized);
                Assert.Equal(diagnostics.IsActive, deserialized.IsActive);
                Assert.Equal(diagnostics.LastHealthCheck, deserialized.LastHealthCheck);
                Assert.Equal(diagnostics.TimeSinceLastCheck, deserialized.TimeSinceLastCheck);
                Assert.Equal(diagnostics.IsHealthCheckDue, deserialized.IsHealthCheckDue);
                Assert.Equal(diagnostics.ErrorMessage, deserialized.ErrorMessage);
                Assert.Equal(diagnostics.AdditionalInfo.Count, deserialized.AdditionalInfo.Count);
            });

            Assert.Null(exception);
        }
    }
}
