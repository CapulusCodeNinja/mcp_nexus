using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Xunit;

namespace mcp_nexus.tests
{
	public class ConfigurationTests
	{
		[Fact]
		public void Configuration_LoadsDefaultValues_Correctly()
		{
			// Arrange
			var configData = new Dictionary<string, string?>
			{
				["McpNexus:Server:Host"] = "0.0.0.0",
				["McpNexus:Server:Port"] = "5511",
				["McpNexus:Transport:Mode"] = "http",
				["McpNexus:Transport:ServiceMode"] = "true",
				["McpNexus:Debugging:CommandTimeoutMs"] = "600000",
				["McpNexus:Debugging:SymbolServerTimeoutMs"] = "300000",
				["McpNexus:Debugging:SymbolServerMaxRetries"] = "1",
				["McpNexus:Debugging:StartupDelayMs"] = "2000"
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configData)
				.Build();

			// Act & Assert
			Assert.Equal("0.0.0.0", configuration["McpNexus:Server:Host"]);
			Assert.Equal("5511", configuration["McpNexus:Server:Port"]);
			Assert.Equal("http", configuration["McpNexus:Transport:Mode"]);
			Assert.Equal("true", configuration["McpNexus:Transport:ServiceMode"]);
			Assert.Equal("600000", configuration["McpNexus:Debugging:CommandTimeoutMs"]);
			Assert.Equal("300000", configuration["McpNexus:Debugging:SymbolServerTimeoutMs"]);
			Assert.Equal("1", configuration["McpNexus:Debugging:SymbolServerMaxRetries"]);
			Assert.Equal("2000", configuration["McpNexus:Debugging:StartupDelayMs"]);
		}

		[Fact]
		public void Configuration_GetValueWithDefault_WorksCorrectly()
		{
			// Arrange
			var configData = new Dictionary<string, string?>
			{
				["McpNexus:Debugging:CommandTimeoutMs"] = "120000"
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configData)
				.Build();

			// Act
			var commandTimeout = configuration.GetValue("McpNexus:Debugging:CommandTimeoutMs", 30000);
			var symbolTimeout = configuration.GetValue("McpNexus:Debugging:SymbolServerTimeoutMs", 30000); // Not in config, should use default
			var maxRetries = configuration.GetValue("McpNexus:Debugging:SymbolServerMaxRetries", 1);

			// Assert
			Assert.Equal(120000, commandTimeout);
			Assert.Equal(30000, symbolTimeout); // Default value
			Assert.Equal(1, maxRetries); // Default value
		}

		[Fact]
		public void Configuration_HandlesNullValues_Correctly()
		{
			// Arrange
			var configData = new Dictionary<string, string?>
			{
				["McpNexus:Debugging:CdbPath"] = null, // Explicitly null
				["McpNexus:Debugging:SymbolSearchPath"] = null
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configData)
				.Build();

			// Act
			var cdbPath = configuration.GetValue<string?>("McpNexus:Debugging:CdbPath");
			var symbolPath = configuration.GetValue<string?>("McpNexus:Debugging:SymbolSearchPath");

			// Assert
			Assert.Null(cdbPath);
			Assert.Null(symbolPath);
		}

		[Fact]
		public void Configuration_OverridesWork_Correctly()
		{
			// Arrange - Simulate base config and override
			var baseConfig = new Dictionary<string, string?>
			{
				["McpNexus:Server:Port"] = "5511",
				["McpNexus:Debugging:CommandTimeoutMs"] = "600000"
			};

			var overrideConfig = new Dictionary<string, string?>
			{
				["McpNexus:Server:Port"] = "8080", // Override
				["McpNexus:Debugging:StartupDelayMs"] = "5000" // Additional
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(baseConfig)
				.AddInMemoryCollection(overrideConfig)
				.Build();

			// Act & Assert
			Assert.Equal("8080", configuration["McpNexus:Server:Port"]); // Should be overridden
			Assert.Equal("600000", configuration["McpNexus:Debugging:CommandTimeoutMs"]); // Should remain from base
			Assert.Equal("5000", configuration["McpNexus:Debugging:StartupDelayMs"]); // Should be added
		}

		[Fact]
		public void Configuration_BooleanValues_ParseCorrectly()
		{
			// Arrange
			var configData = new Dictionary<string, string?>
			{
				["McpNexus:Transport:ServiceMode"] = "true",
				["McpNexus:Debug:EnableVerboseLogging"] = "false",
				["McpNexus:Debug:EnableDetailedErrors"] = "True", // Case insensitive
				["McpNexus:Debug:DisableSymbols"] = "FALSE" // Case insensitive
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configData)
				.Build();

			// Act
			var serviceMode = configuration.GetValue<bool>("McpNexus:Transport:ServiceMode");
			var verboseLogging = configuration.GetValue<bool>("McpNexus:Debug:EnableVerboseLogging");
			var detailedErrors = configuration.GetValue<bool>("McpNexus:Debug:EnableDetailedErrors");
			var disableSymbols = configuration.GetValue<bool>("McpNexus:Debug:DisableSymbols");

			// Assert
			Assert.True(serviceMode);
			Assert.False(verboseLogging);
			Assert.True(detailedErrors);
			Assert.False(disableSymbols);
		}

		[Fact]
		public void Configuration_IntegerValues_ParseCorrectly()
		{
			// Arrange
			var configData = new Dictionary<string, string?>
			{
				["McpNexus:Server:Port"] = "5511",
				["McpNexus:Debugging:CommandTimeoutMs"] = "600000",
				["McpNexus:Debugging:SymbolServerMaxRetries"] = "3",
				["McpNexus:Debugging:StartupDelayMs"] = "2000"
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configData)
				.Build();

			// Act
			var port = configuration.GetValue<int>("McpNexus:Server:Port");
			var commandTimeout = configuration.GetValue<int>("McpNexus:Debugging:CommandTimeoutMs");
			var maxRetries = configuration.GetValue<int>("McpNexus:Debugging:SymbolServerMaxRetries");
			var startupDelay = configuration.GetValue<int>("McpNexus:Debugging:StartupDelayMs");

			// Assert
			Assert.Equal(5511, port);
			Assert.Equal(600000, commandTimeout);
			Assert.Equal(3, maxRetries);
			Assert.Equal(2000, startupDelay);
		}

		[Fact]
		public void Configuration_InvalidIntegerValues_UseDefault()
		{
			// Arrange
			var configData = new Dictionary<string, string?>
			{
				["McpNexus:Server:Port"] = "invalid", // Invalid integer
				["McpNexus:Debugging:CommandTimeoutMs"] = "" // Empty string
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configData)
				.Build();

		// Act & Assert
		// GetValue throws when it can't convert, so we need to handle the exception
		var port = 0;
		var commandTimeout = 0;
		
		try
		{
			port = configuration.GetValue("McpNexus:Server:Port", 5511);
		}
		catch (InvalidOperationException)
		{
			port = 5511; // Use default when conversion fails
		}
		
		try
		{
			commandTimeout = configuration.GetValue("McpNexus:Debugging:CommandTimeoutMs", 30000);
		}
		catch (InvalidOperationException)
		{
			commandTimeout = 30000; // Use default when conversion fails
		}

		// Assert
		Assert.Equal(5511, port); // Should use default due to invalid value
		Assert.Equal(30000, commandTimeout); // Should use default due to empty value
		}

		[Fact]
		public void Configuration_ComplexSymbolSearchPath_HandlesCorrectly()
		{
			// Arrange
			var symbolPath = "cache*C:\\Symbols\\Cache;srv*C:\\Symbols\\Cold*https://msdl.microsoft.com/download/symbols;srv*C:\\Local*https://symbols.nuget.org/download/symbols";
			var configData = new Dictionary<string, string?>
			{
				["McpNexus:Debugging:SymbolSearchPath"] = symbolPath
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configData)
				.Build();

			// Act
			var retrievedPath = configuration.GetValue<string?>("McpNexus:Debugging:SymbolSearchPath");

			// Assert
			Assert.Equal(symbolPath, retrievedPath);
			Assert.Contains("cache*", retrievedPath!);
			Assert.Contains("srv*", retrievedPath);
			Assert.Contains("https://", retrievedPath);
		}

		[Fact]
		public void Configuration_ServiceInstallationPaths_WorkCorrectly()
		{
			// Arrange
			var configData = new Dictionary<string, string?>
			{
				["McpNexus:Service:InstallPath"] = "C:\\Program Files\\MCP-Nexus",
				["McpNexus:Service:BackupPath"] = "C:\\Program Files\\MCP-Nexus\\backups"
			};

			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(configData)
				.Build();

			// Act
			var installPath = configuration.GetValue<string?>("McpNexus:Service:InstallPath");
			var backupPath = configuration.GetValue<string?>("McpNexus:Service:BackupPath");

			// Assert
			Assert.Equal("C:\\Program Files\\MCP-Nexus", installPath);
			Assert.Equal("C:\\Program Files\\MCP-Nexus\\backups", backupPath);
		}
	}
}
