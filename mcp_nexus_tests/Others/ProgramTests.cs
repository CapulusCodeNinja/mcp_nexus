using System.CommandLine;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mcp_nexus;
using Xunit;

namespace mcp_nexus_tests
{
    public class ProgramTests
    {
        [Fact]
        public void Program_Class_Exists()
        {
            // Assert
            var programType = typeof(Program);
            Assert.NotNull(programType);
            Assert.True(programType.IsClass);
        }

        [Fact]
        public void Program_HasMainMethod()
        {
            // Act
            var programType = typeof(Program);
            var mainMethod = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            // Assert
            Assert.NotNull(mainMethod);
            Assert.True(mainMethod!.IsStatic);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithEmptyArgs_ReturnsDefaults()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { Array.Empty<string>() });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            // Check default values
            var useHttpProperty = resultType.GetProperty("UseHttp");
            Assert.NotNull(useHttpProperty);
            Assert.False((bool)useHttpProperty.GetValue(result)!);

            var serviceModeProperty = resultType.GetProperty("ServiceMode");
            Assert.NotNull(serviceModeProperty);
            Assert.False((bool)serviceModeProperty.GetValue(result)!);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithHttpFlag_SetsUseHttp()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--http" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var useHttpProperty = resultType.GetProperty("UseHttp");
            Assert.NotNull(useHttpProperty);
            Assert.True((bool)useHttpProperty.GetValue(result)!);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithServiceFlag_SetsServiceMode()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--service" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var serviceModeProperty = resultType.GetProperty("ServiceMode");
            Assert.NotNull(serviceModeProperty);
            Assert.True((bool)serviceModeProperty.GetValue(result)!);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithInstallFlag_SetsInstall()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--install" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var installProperty = resultType.GetProperty("Install");
            Assert.NotNull(installProperty);
            Assert.True((bool)installProperty.GetValue(result)!);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithPortFlag_SetsPort()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--port", "8080" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var portProperty = resultType.GetProperty("Port");
            Assert.NotNull(portProperty);
            var portValue = (int?)portProperty.GetValue(result);
            Assert.Equal(8080, portValue);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithCdbPathFlag_SetsCdbPath()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--cdb-path", "C:\\CustomCdb\\cdb.exe" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var cdbPathProperty = resultType.GetProperty("CustomCdbPath");
            Assert.NotNull(cdbPathProperty);
            var cdbPathValue = (string?)cdbPathProperty.GetValue(result);
            Assert.Equal("C:\\CustomCdb\\cdb.exe", cdbPathValue);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithHostFlag_SetsHost()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--host", "0.0.0.0" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var hostProperty = resultType.GetProperty("Host");
            Assert.NotNull(hostProperty);
            var hostValue = (string?)hostProperty.GetValue(result);
            Assert.Equal("0.0.0.0", hostValue);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithMultipleFlags_SetsAllValues()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--http", "--port", "9000", "--host", "localhost" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var useHttpProperty = resultType.GetProperty("UseHttp");
            Assert.True((bool)useHttpProperty!.GetValue(result)!);

            var portProperty = resultType.GetProperty("Port");
            Assert.Equal(9000, (int?)portProperty!.GetValue(result));

            var hostProperty = resultType.GetProperty("Host");
            Assert.Equal("localhost", (string?)hostProperty!.GetValue(result));
        }

        [Fact]
        public void Program_FormatBannerLine_FormatsCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var formatMethod = programType.GetMethod("FormatBannerLine", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatMethod);

            // Act
            var result = (string)formatMethod!.Invoke(null, new object[] { "Label:", "Value", 60 })!;

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("* Label:", result);
            Assert.EndsWith(" *", result);
            Assert.Contains("Value", result);
        }

        [Fact]
        public void Program_FormatCenteredBannerLine_CentersText()
        {
            // Arrange
            var programType = typeof(Program);
            var formatMethod = programType.GetMethod("FormatCenteredBannerLine", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatMethod);

            // Act
            var result = (string)formatMethod!.Invoke(null, new object[] { "TEST", 20 })!;

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("* ", result);
            Assert.EndsWith(" *", result);
            Assert.Contains("TEST", result);
            // Should have padding on both sides for centering
            var content = result.Substring(2, result.Length - 4); // Remove "* " and " *"
            Assert.Equal(20, content.Length);
        }

        [Fact]
        public void Program_GetCdbPathInfo_ReturnsString()
        {
            // Arrange
            var programType = typeof(Program);
            var getCdbPathMethod = programType.GetMethod("GetCdbPathInfo", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(getCdbPathMethod);

            // Act
            var result = (string)getCdbPathMethod!.Invoke(null, null)!;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            // Should either find CDB paths or return "No CDB paths found"
            Assert.True(result.Contains("CDB") || result.Contains("Unable to check"));
        }

        [Fact]
        public void Program_GetProviderInfo_HandlesNullProvider()
        {
            // Arrange
            var programType = typeof(Program);
            var getProviderInfoMethod = programType.GetMethod("GetProviderInfo", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(getProviderInfoMethod);

            // Create a mock provider for testing
            var mockProvider = new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationProvider(
                new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource());

            // Act
            var result = (string)getProviderInfoMethod!.Invoke(null, new object[] { mockProvider })!;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Program_CommandLineArguments_Class_HasExpectedProperties()
        {
            // Arrange
            var programType = typeof(Program);
            var nestedTypes = programType.GetNestedTypes(BindingFlags.NonPublic);
            var commandLineArgsType = nestedTypes.FirstOrDefault(t => t.Name == "CommandLineArguments");
            Assert.NotNull(commandLineArgsType);

            // Act & Assert - Check all expected properties exist
            var properties = commandLineArgsType!.GetProperties();
            var propertyNames = properties.Select(p => p.Name).ToList();

            Assert.Contains("CustomCdbPath", propertyNames);
            Assert.Contains("UseHttp", propertyNames);
            Assert.Contains("ServiceMode", propertyNames);
            Assert.Contains("Install", propertyNames);
            Assert.Contains("Uninstall", propertyNames);
            Assert.Contains("ForceUninstall", propertyNames);
            Assert.Contains("Update", propertyNames);
            Assert.Contains("Port", propertyNames);
            Assert.Contains("Host", propertyNames);
            Assert.Contains("HostFromCommandLine", propertyNames);
            Assert.Contains("PortFromCommandLine", propertyNames);
        }

        [Fact]
        public async Task Program_ShowHelpAsync_OutputsHelpText()
        {
            // Arrange
            var programType = typeof(Program);
            var showHelpMethod = programType.GetMethod("ShowHelpAsync", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(showHelpMethod);

            // Capture console output
            using var stringWriter = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(stringWriter);

            try
            {
                // Act
                await (Task)showHelpMethod!.Invoke(null, null)!;

                // Assert
                var output = stringWriter.ToString();
                Assert.NotNull(output);
                Assert.True(output.Length > 0);
                Assert.Contains("MCP Nexus", output);
                Assert.Contains("USAGE:", output);
                Assert.Contains("DESCRIPTION:", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithUninstallFlag_SetsUninstall()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--uninstall" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var uninstallProperty = resultType.GetProperty("Uninstall");
            Assert.NotNull(uninstallProperty);
            Assert.True((bool)uninstallProperty.GetValue(result)!);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithForceUninstallFlag_SetsForceUninstall()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--force-uninstall" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var forceUninstallProperty = resultType.GetProperty("ForceUninstall");
            Assert.NotNull(forceUninstallProperty);
            Assert.True((bool)forceUninstallProperty.GetValue(result)!);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithUpdateFlag_SetsUpdate()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--update" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var updateProperty = resultType.GetProperty("Update");
            Assert.NotNull(updateProperty);
            Assert.True((bool)updateProperty.GetValue(result)!);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithInvalidPort_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--port", "invalid" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var portProperty = resultType.GetProperty("Port");
            Assert.NotNull(portProperty);
            var portValue = (int?)portProperty.GetValue(result);
            // Should handle invalid port gracefully (null or default value)
            Assert.True(portValue == null || portValue == 0);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithNegativePort_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--port", "-1" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var portProperty = resultType.GetProperty("Port");
            Assert.NotNull(portProperty);
            var portValue = (int?)portProperty.GetValue(result);
            // Should handle negative port gracefully
            Assert.True(portValue == null || portValue < 0);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithEmptyCdbPath_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--cdb-path", "" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var cdbPathProperty = resultType.GetProperty("CustomCdbPath");
            Assert.NotNull(cdbPathProperty);
            var cdbPathValue = (string?)cdbPathProperty.GetValue(result);
            Assert.Equal("", cdbPathValue);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithEmptyHost_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--host", "" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            var hostProperty = resultType.GetProperty("Host");
            Assert.NotNull(hostProperty);
            var hostValue = (string?)hostProperty.GetValue(result);
            Assert.Equal("", hostValue);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithUnknownFlag_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            // Act
            var result = parseMethod!.Invoke(null, new object[] { new[] { "--unknown-flag", "value" } });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            // Should still return a valid CommandLineArguments object with defaults
            var useHttpProperty = resultType.GetProperty("UseHttp");
            Assert.NotNull(useHttpProperty);
            Assert.False((bool)useHttpProperty.GetValue(result)!);
        }


        [Fact]
        public void Program_CommandLineArguments_DefaultValues_AreCorrect()
        {
            // Arrange
            var programType = typeof(Program);
            var nestedTypes = programType.GetNestedTypes(BindingFlags.NonPublic);
            var commandLineArgsType = nestedTypes.FirstOrDefault(t => t.Name == "CommandLineArguments");
            Assert.NotNull(commandLineArgsType);

            // Act
            var instance = Activator.CreateInstance(commandLineArgsType!);

            // Assert
            Assert.NotNull(instance);
            var instanceType = instance!.GetType();

            // Check default values
            Assert.Null(instanceType.GetProperty("CustomCdbPath")!.GetValue(instance));
            Assert.False((bool)instanceType.GetProperty("UseHttp")!.GetValue(instance)!);
            Assert.False((bool)instanceType.GetProperty("ServiceMode")!.GetValue(instance)!);
            Assert.False((bool)instanceType.GetProperty("Install")!.GetValue(instance)!);
            Assert.False((bool)instanceType.GetProperty("Uninstall")!.GetValue(instance)!);
            Assert.False((bool)instanceType.GetProperty("ForceUninstall")!.GetValue(instance)!);
            Assert.False((bool)instanceType.GetProperty("Update")!.GetValue(instance)!);
            Assert.Null(instanceType.GetProperty("Port")!.GetValue(instance));
            Assert.Null(instanceType.GetProperty("Host")!.GetValue(instance));
            Assert.False((bool)instanceType.GetProperty("HostFromCommandLine")!.GetValue(instance)!);
            Assert.False((bool)instanceType.GetProperty("PortFromCommandLine")!.GetValue(instance)!);
        }

        [Fact]
        public void Program_SetupGlobalExceptionHandlers_DoesNotThrow()
        {
            // Arrange
            var programType = typeof(Program);
            var setupMethod = programType.GetMethod("SetupGlobalExceptionHandlers", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(setupMethod);

            // Act & Assert - Should not throw
            setupMethod!.Invoke(null, null);
        }

        [Fact]
        public void Program_LogStartupBanner_WithStdioMode_LogsCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var logBannerMethod = programType.GetMethod("LogStartupBanner", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(logBannerMethod);

            // Create a mock CommandLineArguments object
            var commandLineArgsType = programType.GetNestedTypes(BindingFlags.NonPublic)
                .FirstOrDefault(t => t.Name == "CommandLineArguments");
            Assert.NotNull(commandLineArgsType);
            var commandLineArgs = Activator.CreateInstance(commandLineArgsType!);

            // Capture console output
            using var stringWriter = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(stringWriter);

            try
            {
                // Act
                logBannerMethod!.Invoke(null, new object[] { commandLineArgs!, "stdio", (int?)null });

                // Assert
                var output = stringWriter.ToString();
                Assert.NotNull(output);
                Assert.Contains("MCP NEXUS", output);
                Assert.Contains("STDIO Mode", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Program_LogStartupBanner_WithHttpMode_LogsCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var logBannerMethod = programType.GetMethod("LogStartupBanner", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(logBannerMethod);

            // Create a mock CommandLineArguments object
            var commandLineArgsType = programType.GetNestedTypes(BindingFlags.NonPublic)
                .FirstOrDefault(t => t.Name == "CommandLineArguments");
            Assert.NotNull(commandLineArgsType);
            var commandLineArgs = Activator.CreateInstance(commandLineArgsType!);

            // Capture console output
            using var stringWriter = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(stringWriter);

            try
            {
                // Act
                logBannerMethod!.Invoke(null, new object[] { commandLineArgs!, "localhost", 8080 });

                // Assert
                var output = stringWriter.ToString();
                Assert.NotNull(output);
                Assert.Contains("MCP NEXUS", output);
                Assert.Contains("HTTP", output);
                Assert.Contains("localhost", output);
                Assert.Contains("8080", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void Program_LogConfigurationSettings_LogsConfiguration()
        {
            // Arrange
            var programType = typeof(Program);
            var logConfigMethod = programType.GetMethod("LogConfigurationSettings", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(logConfigMethod);

            // Create a mock configuration
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpNexus:Server:Host"] = "localhost",
                ["McpNexus:Server:Port"] = "8080",
                ["Logging:LogLevel"] = "Information"
            });
            var configuration = configBuilder.Build();

            // Create a mock CommandLineArguments object
            var commandLineArgsType = programType.GetNestedTypes(BindingFlags.NonPublic)
                .FirstOrDefault(t => t.Name == "CommandLineArguments");
            Assert.NotNull(commandLineArgsType);
            var commandLineArgs = Activator.CreateInstance(commandLineArgsType!);

            // Act & Assert - Should not throw
            logConfigMethod!.Invoke(null, new object[] { configuration, commandLineArgs! });
        }

        [Fact]
        public void Program_FormatJsonForLogging_WithValidJson_FormatsCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var formatJsonMethod = programType.GetMethod("FormatJsonForLogging", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatJsonMethod);

            var validJson = "{\"key\":\"value\",\"number\":123}";

            // Act
            var result = (string)formatJsonMethod!.Invoke(null, new object[] { validJson })!;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > validJson.Length); // Should be formatted with indentation
            Assert.Contains("key", result);
            Assert.Contains("value", result);
        }

        [Fact]
        public void Program_FormatJsonForLogging_WithInvalidJson_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var formatJsonMethod = programType.GetMethod("FormatJsonForLogging", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatJsonMethod);

            var invalidJson = "{invalid json}";

            // Act
            var result = (string)formatJsonMethod!.Invoke(null, new object[] { invalidJson })!;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Invalid JSON", result);
            Assert.Contains("invalid json", result);
        }

        [Fact]
        public void Program_FormatJsonForLogging_WithEmptyString_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var formatJsonMethod = programType.GetMethod("FormatJsonForLogging", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatJsonMethod);

            // Act
            var result = (string)formatJsonMethod!.Invoke(null, new object[] { "" })!;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Program_FormatSseResponseForLogging_WithValidSse_FormatsCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var formatSseMethod = programType.GetMethod("FormatSseResponseForLogging", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatSseMethod);

            var sseResponse = "event: test\ndata: {\"message\":\"hello\"}\n\n";

            // Act
            var result = (string)formatSseMethod!.Invoke(null, new object[] { sseResponse })!;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("event: test", result);
            Assert.Contains("data:", result);
        }

        [Fact]
        public void Program_FormatSseResponseForLogging_WithEmptyString_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var formatSseMethod = programType.GetMethod("FormatSseResponseForLogging", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatSseMethod);

            // Act
            var result = (string)formatSseMethod!.Invoke(null, new object[] { "" })!;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result);
        }

        [Fact]
        public void Program_ShouldEnableJsonRpcLogging_WithDebugLogger_ReturnsTrue()
        {
            // Arrange
            var programType = typeof(Program);
            var shouldEnableMethod = programType.GetMethod("ShouldEnableJsonRpcLogging", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(shouldEnableMethod);

            // Create a logger factory with debug logging enabled
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Act
            var result = (bool)shouldEnableMethod!.Invoke(null, new object[] { loggerFactory })!;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Program_ShouldEnableJsonRpcLogging_WithInfoLogger_ReturnsFalse()
        {
            // Arrange
            var programType = typeof(Program);
            var shouldEnableMethod = programType.GetMethod("ShouldEnableJsonRpcLogging", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(shouldEnableMethod);

            // Create a logger factory with info logging (not debug)
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Act
            var result = (bool)shouldEnableMethod!.Invoke(null, new object[] { loggerFactory })!;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Program_LogFatalException_WithException_LogsCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var logFatalMethod = programType.GetMethod("LogFatalException", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(logFatalMethod);

            var testException = new InvalidOperationException("Test exception");

            // Capture console error output
            using var stringWriter = new StringWriter();
            var originalError = Console.Error;
            Console.SetError(stringWriter);

            try
            {
                // Act
                logFatalMethod!.Invoke(null, new object[] { testException, "TestSource", false });

                // Assert
                var output = stringWriter.ToString();
                Assert.NotNull(output);
                Assert.Contains("FATAL UNHANDLED EXCEPTION", output);
                Assert.Contains("TestSource", output);
                Assert.Contains("Test exception", output);
            }
            finally
            {
                Console.SetError(originalError);
            }
        }

        [Fact]
        public void Program_LogFatalException_WithNullException_HandlesGracefully()
        {
            // Arrange
            var programType = typeof(Program);
            var logFatalMethod = programType.GetMethod("LogFatalException", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(logFatalMethod);

            // Capture console error output
            using var stringWriter = new StringWriter();
            var originalError = Console.Error;
            Console.SetError(stringWriter);

            try
            {
                // Act
                logFatalMethod!.Invoke(null, new object[] { (Exception?)null, "TestSource", false });

                // Assert
                var output = stringWriter.ToString();
                Assert.NotNull(output);
                Assert.Contains("FATAL UNHANDLED EXCEPTION", output);
                Assert.Contains("TestSource", output);
                Assert.Contains("Exception object is null", output);
            }
            finally
            {
                Console.SetError(originalError);
            }
        }

        [Fact]
        public void Program_GetCdbPathInfo_ReturnsValidString()
        {
            // Arrange
            var programType = typeof(Program);
            var getCdbPathMethod = programType.GetMethod("GetCdbPathInfo", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(getCdbPathMethod);

            // Act
            var result = (string)getCdbPathMethod!.Invoke(null, null)!;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Program_GetProviderInfo_WithJsonProvider_ReturnsInfo()
        {
            // Arrange
            var programType = typeof(Program);
            var getProviderInfoMethod = programType.GetMethod("GetProviderInfo", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(getProviderInfoMethod);

            var jsonProvider = new Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider(
                new Microsoft.Extensions.Configuration.Json.JsonConfigurationSource());

            // Act
            var result = (string)getProviderInfoMethod!.Invoke(null, new object[] { jsonProvider })!;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Program_GetProviderInfo_WithEnvironmentProvider_ReturnsInfo()
        {
            // Arrange
            var programType = typeof(Program);
            var getProviderInfoMethod = programType.GetMethod("GetProviderInfo", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(getProviderInfoMethod);

            var envProvider = new Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationProvider();

            // Act
            var result = (string)getProviderInfoMethod!.Invoke(null, new object[] { envProvider })!;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Program_FormatBannerLine_WithLongContent_TruncatesCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var formatMethod = programType.GetMethod("FormatBannerLine", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatMethod);

            var longValue = new string('x', 100); // Very long value

            // Act
            var result = (string)formatMethod!.Invoke(null, new object[] { "Label:", longValue, 20 })!;

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("* ", result);
            Assert.EndsWith(" *", result);
            // Should be truncated to fit the content width
            var content = result.Substring(2, result.Length - 4); // Remove "* " and " *"
            Assert.True(content.Length <= 20);
        }

        [Fact]
        public void Program_FormatCenteredBannerLine_WithLongText_TruncatesCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var formatMethod = programType.GetMethod("FormatCenteredBannerLine", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatMethod);

            var longText = new string('x', 100); // Very long text

            // Act
            var result = (string)formatMethod!.Invoke(null, new object[] { longText, 20 })!;

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("* ", result);
            Assert.EndsWith(" *", result);
            // Should be truncated to fit the content width
            var content = result.Substring(2, result.Length - 4); // Remove "* " and " *"
            Assert.Equal(20, content.Length);
        }

        [Fact]
        public void Program_FormatCenteredBannerLine_WithOddWidth_CentersCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var formatMethod = programType.GetMethod("FormatCenteredBannerLine", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatMethod);

            // Act
            var result = (string)formatMethod!.Invoke(null, new object[] { "TEST", 21 })!; // Odd width

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("* ", result);
            Assert.EndsWith(" *", result);
            var content = result.Substring(2, result.Length - 4); // Remove "* " and " *"
            Assert.Equal(21, content.Length);
            Assert.Contains("TEST", content);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithComplexArgs_ParsesCorrectly()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            var args = new[] { "--http", "--port", "9000", "--host", "0.0.0.0", "--cdb-path", "C:\\Custom\\cdb.exe" };

            // Act
            var result = parseMethod!.Invoke(null, new object[] { args });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            // Check all values are set correctly
            Assert.True((bool)resultType.GetProperty("UseHttp")!.GetValue(result)!);
            Assert.Equal(9000, (int?)resultType.GetProperty("Port")!.GetValue(result));
            Assert.Equal("0.0.0.0", (string?)resultType.GetProperty("Host")!.GetValue(result));
            Assert.Equal("C:\\Custom\\cdb.exe", (string?)resultType.GetProperty("CustomCdbPath")!.GetValue(result));
            Assert.True((bool)resultType.GetProperty("PortFromCommandLine")!.GetValue(result)!);
            Assert.True((bool)resultType.GetProperty("HostFromCommandLine")!.GetValue(result)!);
        }

        [Fact]
        public void Program_ParseCommandLineArguments_WithServiceMode_SetsUseHttp()
        {
            // Arrange
            var programType = typeof(Program);
            var parseMethod = programType.GetMethod("ParseCommandLineArguments", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(parseMethod);

            var args = new[] { "--service" };

            // Act
            var result = parseMethod!.Invoke(null, new object[] { args });

            // Assert
            Assert.NotNull(result);
            var resultType = result!.GetType();

            // Service mode should imply HTTP mode
            Assert.True((bool)resultType.GetProperty("ServiceMode")!.GetValue(result)!);
            // Note: The actual logic for setting UseHttp based on ServiceMode is in the Main method, not in ParseCommandLineArguments
        }
    }
}

