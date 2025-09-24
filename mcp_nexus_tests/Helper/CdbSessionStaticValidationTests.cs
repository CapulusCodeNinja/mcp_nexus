using System;
using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using Xunit;

namespace mcp_nexus_tests.Helper
{
	public class CdbSessionStaticValidationTests
	{
		private static ILogger<CdbSession> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<CdbSession>();

		[Fact]
		public void Constructor_WithValidParameters_DoesNotThrow()
		{
			// Act & Assert - Should not throw
			var session = new CdbSession(CreateNullLogger(), 30000, null, 30000, 1, null, 2000);
			Assert.NotNull(session);
			session.Dispose();
		}

		[Fact]
		public void Constructor_WithZeroCommandTimeout_ThrowsArgumentOutOfRangeException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), 0));
			Assert.Contains("Command timeout must be positive", exception.Message);
		}

		[Fact]
		public void Constructor_WithNegativeCommandTimeout_ThrowsArgumentOutOfRangeException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), -1000));
			Assert.Contains("Command timeout must be positive", exception.Message);
		}

		[Fact]
		public void Constructor_WithNegativeSymbolServerTimeout_ThrowsArgumentOutOfRangeException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), 30000, null, -1000));
			Assert.Contains("Symbol server timeout cannot be negative", exception.Message);
		}

		[Fact]
		public void Constructor_WithNegativeSymbolServerMaxRetries_ThrowsArgumentOutOfRangeException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), 30000, null, 30000, -1));
			Assert.Contains("Symbol server max retries cannot be negative", exception.Message);
		}

		[Fact]
		public void Constructor_WithNegativeStartupDelay_ThrowsArgumentOutOfRangeException()
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), 30000, null, 30000, 1, null, -1000));
			Assert.Contains("Startup delay cannot be negative", exception.Message);
		}

		[Fact]
		public void Constructor_WithZeroSymbolServerTimeout_DoesNotThrow()
		{
			// Act & Assert - Zero timeout should be valid
			var session = new CdbSession(CreateNullLogger(), 30000, null, 0);
			Assert.NotNull(session);
			session.Dispose();
		}

		[Fact]
		public void Constructor_WithZeroSymbolServerMaxRetries_DoesNotThrow()
		{
			// Act & Assert - Zero retries should be valid
			var session = new CdbSession(CreateNullLogger(), 30000, null, 30000, 0);
			Assert.NotNull(session);
			session.Dispose();
		}

		[Fact]
		public void Constructor_WithZeroStartupDelay_DoesNotThrow()
		{
			// Act & Assert - Zero startup delay should be valid
			var session = new CdbSession(CreateNullLogger(), 30000, null, 30000, 1, null, 0);
			Assert.NotNull(session);
			session.Dispose();
		}

		[Fact]
		public void Constructor_WithCustomCdbPath_DoesNotThrow()
		{
			// Act & Assert
			var session = new CdbSession(CreateNullLogger(), 30000, "C:\\CustomPath\\cdb.exe");
			Assert.NotNull(session);
			session.Dispose();
		}

		[Fact]
		public void Constructor_WithSymbolSearchPath_DoesNotThrow()
		{
			// Act & Assert
			var session = new CdbSession(CreateNullLogger(), 30000, null, 30000, 1, "srv*c:\\symbols*https://msdl.microsoft.com/download/symbols");
			Assert.NotNull(session);
			session.Dispose();
		}

		[Fact]
		public void Constructor_WithAllCustomParameters_DoesNotThrow()
		{
			// Act & Assert
			var session = new CdbSession(
				CreateNullLogger(), 
				60000, // commandTimeoutMs
				"C:\\CustomPath\\cdb.exe", // customCdbPath
				45000, // symbolServerTimeoutMs
				3, // symbolServerMaxRetries
				"srv*c:\\symbols*https://msdl.microsoft.com/download/symbols", // symbolSearchPath
				5000); // startupDelayMs
			Assert.NotNull(session);
			session.Dispose();
		}

		[Fact]
		public void Constructor_WithMinimumValidValues_DoesNotThrow()
		{
			// Act & Assert
			var session = new CdbSession(
				CreateNullLogger(), 
				1, // Minimum command timeout
				null, 
				0, // Minimum symbol server timeout
				0, // Minimum retries
				null, 
				0); // Minimum startup delay
			Assert.NotNull(session);
			session.Dispose();
		}

		[Fact]
		public void Constructor_WithLargeValidValues_DoesNotThrow()
		{
			// Act & Assert
			var session = new CdbSession(
				CreateNullLogger(), 
				300000, // 5 minute timeout
				null, 
				120000, // 2 minute symbol timeout
				10, // 10 retries
				null, 
				10000); // 10 second startup delay
			Assert.NotNull(session);
			session.Dispose();
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(-100)]
		[InlineData(-999999)]
		public void Constructor_WithVariousNegativeCommandTimeouts_ThrowsArgumentOutOfRangeException(int invalidTimeout)
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), invalidTimeout));
			Assert.Contains("Command timeout must be positive", exception.Message);
			Assert.Equal("commandTimeoutMs", exception.ParamName);
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(-100)]
		[InlineData(-999999)]
		public void Constructor_WithVariousNegativeSymbolTimeouts_ThrowsArgumentOutOfRangeException(int invalidTimeout)
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), 30000, null, invalidTimeout));
			Assert.Contains("Symbol server timeout cannot be negative", exception.Message);
			Assert.Equal("symbolServerTimeoutMs", exception.ParamName);
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(-5)]
		[InlineData(-100)]
		public void Constructor_WithVariousNegativeRetries_ThrowsArgumentOutOfRangeException(int invalidRetries)
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), 30000, null, 30000, invalidRetries));
			Assert.Contains("Symbol server max retries cannot be negative", exception.Message);
			Assert.Equal("symbolServerMaxRetries", exception.ParamName);
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(-100)]
		[InlineData(-999999)]
		public void Constructor_WithVariousNegativeStartupDelays_ThrowsArgumentOutOfRangeException(int invalidDelay)
		{
			// Act & Assert
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(CreateNullLogger(), 30000, null, 30000, 1, null, invalidDelay));
			Assert.Contains("Startup delay cannot be negative", exception.Message);
			Assert.Equal("startupDelayMs", exception.ParamName);
		}
	}
}
