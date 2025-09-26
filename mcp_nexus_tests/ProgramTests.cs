using System.CommandLine;
using System.Reflection;
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
	}
}

