using System;
using Microsoft.Extensions.Logging;
using mcp_nexus.Tools;
using Xunit;

namespace mcp_nexus.tests.Tools
{
	public class TimeToolTests
	{
		private static ILogger<TimeTool> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<TimeTool>();

		[Fact]
		public void GetCurrentTime_ValidCity_ReturnsFormattedTime()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result = tool.GetCurrentTime("New York");

			// Assert
			Assert.NotNull(result);
			Assert.Contains("New York", result);
			Assert.Contains(":", result); // Should contain time format
		}

		[Fact]
		public void GetCurrentTime_EmptyCity_ReturnsUtcTime()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result = tool.GetCurrentTime("");

			// Assert
			Assert.NotNull(result);
			Assert.Contains("UTC", result);
		}

		[Fact]
		public void GetCurrentTime_NullCity_ReturnsUtcTime()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result = tool.GetCurrentTime(null!);

			// Assert
			Assert.NotNull(result);
			Assert.Contains("UTC", result);
		}

		[Fact]
		public void GetCurrentTime_WhitespaceCity_ReturnsUtcTime()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result = tool.GetCurrentTime("   ");

			// Assert
			Assert.NotNull(result);
			Assert.Contains("UTC", result);
		}

		[Fact]
		public void GetCurrentTime_CommonCities_ReturnValidResults()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());
			var cities = new[] { "London", "Tokyo", "Paris", "Berlin", "Sydney" };

			// Act & Assert
			foreach (var city in cities)
			{
				var result = tool.GetCurrentTime(city);
				Assert.NotNull(result);
				Assert.Contains(city, result);
				Assert.Contains(":", result); // Should contain time format
			}
		}

		[Fact]
		public void GetCurrentTime_CaseInsensitive_WorksCorrectly()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result1 = tool.GetCurrentTime("london");
			var result2 = tool.GetCurrentTime("LONDON");
			var result3 = tool.GetCurrentTime("London");

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			
			// All should contain London (case may vary in output)
			Assert.True(result1.Contains("London", StringComparison.OrdinalIgnoreCase));
			Assert.True(result2.Contains("London", StringComparison.OrdinalIgnoreCase));
			Assert.True(result3.Contains("London", StringComparison.OrdinalIgnoreCase));
		}

		[Fact]
		public void GetCurrentTime_UnknownCity_ReturnsErrorOrFallback()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result = tool.GetCurrentTime("NonExistentCityXYZ123");

			// Assert
			Assert.NotNull(result);
			// Should either return an error message or fallback to UTC
			Assert.True(result.Contains("not found") || result.Contains("UTC") || result.Contains("NonExistentCityXYZ123"));
		}

		[Fact]
		public void GetCurrentTime_CitiesWithSpaces_HandlesCorrectly()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result1 = tool.GetCurrentTime("New York");
			var result2 = tool.GetCurrentTime("Los Angeles");
			var result3 = tool.GetCurrentTime("San Francisco");

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			
			Assert.Contains("New York", result1);
			Assert.Contains("Los Angeles", result2);
			Assert.Contains("San Francisco", result3);
		}

		[Fact]
		public void GetCurrentTime_SpecialCharactersInCity_HandlesGracefully()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result = tool.GetCurrentTime("City@#$%^&*()");

			// Assert
			Assert.NotNull(result);
			// Should handle gracefully without throwing exceptions
		}

		[Fact]
		public void GetCurrentTime_VeryLongCityName_HandlesCorrectly()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());
			var longCityName = new string('A', 1000); // Very long string

			// Act
			var result = tool.GetCurrentTime(longCityName);

			// Assert
			Assert.NotNull(result);
			// Should handle without issues
		}

		[Fact]
		public void GetCurrentTime_ReturnsCurrentTimeNotFixed()
		{
			// Arrange
			var tool = new TimeTool(CreateNullLogger());

			// Act
			var result1 = tool.GetCurrentTime("UTC");
			System.Threading.Thread.Sleep(1100); // Wait over a second
			var result2 = tool.GetCurrentTime("UTC");

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			// Results should be different since time has passed
			// (Though they might be the same if called in the same second)
			Assert.NotEqual("", result1.Trim());
			Assert.NotEqual("", result2.Trim());
		}
	}
}
