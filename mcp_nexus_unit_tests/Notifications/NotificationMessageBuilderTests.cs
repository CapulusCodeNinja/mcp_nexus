using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Notifications;
using Xunit;

namespace mcp_nexus_unit_tests.Notifications
{
    public class NotificationMessageBuilderTests
    {
        [Fact]
        public void NotificationMessageBuilder_Class_Exists()
        {
            // Act
            var type = typeof(NotificationMessageBuilder);

            // Assert
            Assert.NotNull(type);
        }

        [Fact]
        public void NotificationMessageBuilder_DefaultConstructor_CreatesInstance()
        {
            // Act
            var builder = new NotificationMessageBuilder();

            // Assert
            Assert.NotNull(builder);
        }

        [Fact]
        public void NotificationMessageBuilder_ClassCharacteristics_AreCorrect()
        {
            // Arrange
            var type = typeof(NotificationMessageBuilder);

            // Assert
            Assert.True(type.IsClass);
            Assert.False(type.IsSealed);
            Assert.False(type.IsAbstract);
            Assert.False(type.IsInterface);
            Assert.False(type.IsEnum);
            Assert.False(type.IsValueType);
        }

        [Fact]
        public void SetMethod_WithValidMethod_SetsMethod()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test/method";

            // Act
            var result = builder.SetMethod(method);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(method, notification.Method);
        }

        [Fact]
        public void SetMethod_WithNullMethod_SetsNullMethod()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();

            // Act
            var result = builder.SetMethod(null!);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Null(notification.Method);
        }

        [Fact]
        public void SetMethod_WithEmptyMethod_SetsEmptyMethod()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "";

            // Act
            var result = builder.SetMethod(method);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(method, notification.Method);
        }

        [Fact]
        public void SetMethod_WithWhitespaceMethod_SetsWhitespaceMethod()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "   ";

            // Act
            var result = builder.SetMethod(method);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(method, notification.Method);
        }

        [Fact]
        public void SetMethod_WithUnicodeMethod_SetsUnicodeMethod()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test/方法/测试";

            // Act
            var result = builder.SetMethod(method);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(method, notification.Method);
        }

        [Fact]
        public void SetMethod_WithVeryLongMethod_SetsVeryLongMethod()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = new string('a', 10000);

            // Act
            var result = builder.SetMethod(method);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(method, notification.Method);
        }

        [Fact]
        public void SetMethod_WithSpecialCharacters_SetsSpecialCharacters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            var result = builder.SetMethod(method);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(method, notification.Method);
        }

        [Fact]
        public void SetMethod_ChainedCalls_OverwritesPreviousMethod()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var firstMethod = "first/method";
            var secondMethod = "second/method";

            // Act
            builder.SetMethod(firstMethod).SetMethod(secondMethod);

            // Assert
            var notification = builder.Build();
            Assert.Equal(secondMethod, notification.Method);
        }

        [Fact]
        public void SetParameters_WithValidParameters_SetsParameters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var parameters = new { test = "value", number = 42 };

            // Act
            var result = builder.SetParameters(parameters);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(parameters, notification.Params);
        }

        [Fact]
        public void SetParameters_WithNullParameters_SetsNullParameters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();

            // Act
            var result = builder.SetParameters(null!);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Null(notification.Params);
        }

        [Fact]
        public void SetParameters_WithStringParameters_SetsStringParameters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var parameters = "test string";

            // Act
            var result = builder.SetParameters(parameters);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(parameters, notification.Params);
        }

        [Fact]
        public void SetParameters_WithNumberParameters_SetsNumberParameters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var parameters = 42;

            // Act
            var result = builder.SetParameters(parameters);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(parameters, notification.Params);
        }

        [Fact]
        public void SetParameters_WithBooleanParameters_SetsBooleanParameters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var parameters = true;

            // Act
            var result = builder.SetParameters(parameters);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(parameters, notification.Params);
        }

        [Fact]
        public void SetParameters_WithArrayParameters_SetsArrayParameters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var parameters = new[] { "item1", "item2", "item3" };

            // Act
            var result = builder.SetParameters(parameters);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(parameters, notification.Params);
        }

        [Fact]
        public void SetParameters_WithDictionaryParameters_SetsDictionaryParameters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var parameters = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "key3", true }
            };

            // Act
            var result = builder.SetParameters(parameters);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(parameters, notification.Params);
        }

        [Fact]
        public void SetParameters_WithComplexObject_SetsComplexObject()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var parameters = new
            {
                id = 123,
                name = "test",
                items = new[] { "a", "b", "c" },
                metadata = new { created = DateTime.Now, version = "1.0" }
            };

            // Act
            var result = builder.SetParameters(parameters);

            // Assert
            Assert.Same(builder, result);
            var notification = builder.Build();
            Assert.Equal(parameters, notification.Params);
        }

        [Fact]
        public void SetParameters_ChainedCalls_OverwritesPreviousParameters()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var firstParams = new { first = "value" };
            var secondParams = new { second = "value" };

            // Act
            builder.SetParameters(firstParams).SetParameters(secondParams);

            // Assert
            var notification = builder.Build();
            Assert.Equal(secondParams, notification.Params);
        }

        [Fact]
        public void Build_WithDefaultValues_ReturnsNotificationWithDefaults()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();

            // Act
            var result = builder.Build();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2.0", result.JsonRpc);
            Assert.Equal(string.Empty, result.Method);
            Assert.Null(result.Params);
        }

        [Fact]
        public void Build_WithSetValues_ReturnsNotificationWithValues()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test/method";
            var parameters = new { test = "value" };

            // Act
            var result = builder.SetMethod(method).SetParameters(parameters).Build();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2.0", result.JsonRpc);
            Assert.Equal(method, result.Method);
            Assert.Equal(parameters, result.Params);
        }

        [Fact]
        public void Build_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();

            // Act
            var result1 = builder.Build();
            var result2 = builder.Build();

            // Assert
            // The Build() method returns the same McpNotification instance each time
            // This is the expected behavior since it's a builder pattern
            Assert.Same(result1, result2);
            Assert.Equal(result1.Method, result2.Method);
            Assert.Equal(result1.Params, result2.Params);
        }

        [Fact]
        public void BuildJson_WithDefaultValues_ReturnsValidJson()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();

            // Act
            var result = builder.BuildJson();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            // Verify it's valid JSON
            var notification = JsonSerializer.Deserialize<McpNotification>(result);
            Assert.NotNull(notification);
            Assert.Equal("2.0", notification.JsonRpc);
            Assert.Equal(string.Empty, notification.Method);
            Assert.Null(notification.Params);
        }

        [Fact]
        public void BuildJson_WithSetValues_ReturnsValidJson()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test/method";
            var parameters = new { test = "value", number = 42 };

            // Act
            var result = builder.SetMethod(method).SetParameters(parameters).BuildJson();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            // Verify it's valid JSON
            var notification = JsonSerializer.Deserialize<McpNotification>(result);
            Assert.NotNull(notification);
            Assert.Equal("2.0", notification.JsonRpc);
            Assert.Equal(method, notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void BuildJson_WithUnicodeContent_ReturnsValidJson()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test/方法";
            var parameters = new { message = "测试消息", unicode = "🚀" };

            // Act
            var result = builder.SetMethod(method).SetParameters(parameters).BuildJson();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // JSON serializer escapes Unicode characters, so check for escaped versions
            Assert.Contains("\\u65B9\\u6CD5", result); // "方法" escaped
            Assert.Contains("\\u6D4B\\u8BD5", result); // "测试" escaped
            Assert.Contains("\\uD83D\\uDE80", result); // "🚀" escaped

            // Verify it's valid JSON
            var notification = JsonSerializer.Deserialize<McpNotification>(result);
            Assert.NotNull(notification);
            Assert.Equal(method, notification.Method);
        }

        [Fact]
        public void BuildJson_WithSpecialCharacters_ReturnsValidJson()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test/method";
            var parameters = new { special = "!@#$%^&*()_+-=[]{}|;':\",./<>?" };

            // Act
            var result = builder.SetMethod(method).SetParameters(parameters).BuildJson();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            // Verify it's valid JSON
            var notification = JsonSerializer.Deserialize<McpNotification>(result);
            Assert.NotNull(notification);
            Assert.Equal(method, notification.Method);
        }

        [Fact]
        public void BuildJson_MultipleCalls_ReturnsSameJson()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test/method";
            var parameters = new { test = "value" };

            // Act
            var result1 = builder.SetMethod(method).SetParameters(parameters).BuildJson();
            var result2 = builder.BuildJson();

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void CreateCommandStatusNotification_WithValidParameters_ReturnsBuilder()
        {
            // Arrange
            var commandId = "cmd-123";
            var status = "completed";
            var result = "success";

            // Act
            var builder = NotificationMessageBuilder.CreateCommandStatusNotification(commandId, status, result);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/commandStatus", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateCommandStatusNotification_WithNullResult_ReturnsBuilder()
        {
            // Arrange
            var commandId = "cmd-123";
            var status = "completed";

            // Act
            var builder = NotificationMessageBuilder.CreateCommandStatusNotification(commandId, status);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/commandStatus", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateCommandStatusNotification_WithEmptyParameters_ReturnsBuilder()
        {
            // Arrange
            var commandId = "";
            var status = "";

            // Act
            var builder = NotificationMessageBuilder.CreateCommandStatusNotification(commandId, status);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/commandStatus", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateCommandStatusNotification_WithUnicodeParameters_ReturnsBuilder()
        {
            // Arrange
            var commandId = "命令-123";
            var status = "已完成";
            var result = "成功";

            // Act
            var builder = NotificationMessageBuilder.CreateCommandStatusNotification(commandId, status, result);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/commandStatus", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateCommandStatusNotification_WithSpecialCharacters_ReturnsBuilder()
        {
            // Arrange
            var commandId = "cmd!@#$%^&*()_+-=[]{}|;':\",./<>?";
            var status = "status!@#$%^&*()_+-=[]{}|;':\",./<>?";
            var result = "result!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            var builder = NotificationMessageBuilder.CreateCommandStatusNotification(commandId, status, result);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/commandStatus", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateSessionEventNotification_WithValidParameters_ReturnsBuilder()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "created";
            var data = new { timestamp = DateTime.Now, user = "test" };

            // Act
            var builder = NotificationMessageBuilder.CreateSessionEventNotification(sessionId, eventType, data);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/sessionEvent", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateSessionEventNotification_WithNullData_ReturnsBuilder()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "created";

            // Act
            var builder = NotificationMessageBuilder.CreateSessionEventNotification(sessionId, eventType, null!);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/sessionEvent", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateSessionEventNotification_WithEmptyParameters_ReturnsBuilder()
        {
            // Arrange
            var sessionId = "";
            var eventType = "";
            var data = new { };

            // Act
            var builder = NotificationMessageBuilder.CreateSessionEventNotification(sessionId, eventType, data);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/sessionEvent", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateSessionEventNotification_WithUnicodeParameters_ReturnsBuilder()
        {
            // Arrange
            var sessionId = "会话-123";
            var eventType = "已创建";
            var data = new { 时间 = DateTime.Now, 用户 = "测试" };

            // Act
            var builder = NotificationMessageBuilder.CreateSessionEventNotification(sessionId, eventType, data);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/sessionEvent", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateServerHealthNotification_WithValidParameters_ReturnsBuilder()
        {
            // Arrange
            var healthStatus = "healthy";
            var status = "all systems operational";

            // Act
            var builder = NotificationMessageBuilder.CreateServerHealthNotification(healthStatus, status);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/serverHealth", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateServerHealthNotification_WithNullStatus_ReturnsBuilder()
        {
            // Arrange
            var healthStatus = "healthy";

            // Act
            var builder = NotificationMessageBuilder.CreateServerHealthNotification(healthStatus);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/serverHealth", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateServerHealthNotification_WithEmptyParameters_ReturnsBuilder()
        {
            // Arrange
            var healthStatus = "";
            var status = "";

            // Act
            var builder = NotificationMessageBuilder.CreateServerHealthNotification(healthStatus, status);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/serverHealth", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateServerHealthNotification_WithUnicodeParameters_ReturnsBuilder()
        {
            // Arrange
            var healthStatus = "健康";
            var status = "所有系统正常运行";

            // Act
            var builder = NotificationMessageBuilder.CreateServerHealthNotification(healthStatus, status);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/serverHealth", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void CreateServerHealthNotification_WithSpecialCharacters_ReturnsBuilder()
        {
            // Arrange
            var healthStatus = "health!@#$%^&*()_+-=[]{}|;':\",./<>?";
            var status = "status!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            var builder = NotificationMessageBuilder.CreateServerHealthNotification(healthStatus, status);

            // Assert
            Assert.NotNull(builder);
            var notification = builder.Build();
            Assert.Equal("notifications/serverHealth", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void NotificationMessageBuilder_CanBeUsedInCollections()
        {
            // Arrange
            var builders = new List<NotificationMessageBuilder>
            {
                new NotificationMessageBuilder().SetMethod("method1"),
                new NotificationMessageBuilder().SetMethod("method2"),
                new NotificationMessageBuilder().SetMethod("method3")
            };

            // Act & Assert
            Assert.Equal(3, builders.Count);
            Assert.All(builders, builder => Assert.NotNull(builder));
        }

        [Fact]
        public void NotificationMessageBuilder_CanBeSerialized()
        {
            // Arrange
            var builder = new NotificationMessageBuilder()
                .SetMethod("test/method")
                .SetParameters(new { test = "value" });

            // Act
            var json = builder.BuildJson();

            // Assert
            Assert.NotNull(json);
            Assert.NotEmpty(json);

            // Verify it can be deserialized
            var deserialized = JsonSerializer.Deserialize<McpNotification>(json);
            Assert.NotNull(deserialized);
            Assert.Equal("test/method", deserialized.Method);
        }

        [Fact]
        public void NotificationMessageBuilder_MultipleInstances_AreIndependent()
        {
            // Arrange
            var builder1 = new NotificationMessageBuilder();
            var builder2 = new NotificationMessageBuilder();

            // Act
            builder1.SetMethod("method1").SetParameters(new { value1 = "test1" });
            builder2.SetMethod("method2").SetParameters(new { value2 = "test2" });

            // Assert
            var notification1 = builder1.Build();
            var notification2 = builder2.Build();

            Assert.NotEqual(notification1.Method, notification2.Method);
            Assert.NotEqual(notification1.Params, notification2.Params);
        }

        [Fact]
        public void NotificationMessageBuilder_ChainedCalls_WorkCorrectly()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();

            // Act
            var result = builder
                .SetMethod("test/method")
                .SetParameters(new { test = "value" })
                .Build();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test/method", result.Method);
            Assert.NotNull(result.Params);
        }

        [Fact]
        public void NotificationMessageBuilder_WithVeryLongJson_HandlesCorrectly()
        {
            // Arrange
            var builder = new NotificationMessageBuilder();
            var method = "test/method";
            var parameters = new
            {
                longString = new string('a', 10000),
                array = Enumerable.Range(0, 1000).ToArray(),
                nested = new
                {
                    level1 = new
                    {
                        level2 = new
                        {
                            level3 = new string('b', 1000)
                        }
                    }
                }
            };

            // Act
            var result = builder.SetMethod(method).SetParameters(parameters).BuildJson();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            // Verify it's valid JSON
            var notification = JsonSerializer.Deserialize<McpNotification>(result);
            Assert.NotNull(notification);
            Assert.Equal(method, notification.Method);
        }
    }
}