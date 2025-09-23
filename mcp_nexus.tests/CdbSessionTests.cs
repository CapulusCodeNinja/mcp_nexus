using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using Xunit;

namespace mcp_nexus.tests
{
	public class CdbSessionTests
	{
		private static ILogger<CdbSession> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<CdbSession>();

		[Fact]
		public async Task StopSession_NoActiveSession_ReturnsFalse()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			var result = await session.StopSession();

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void IsActive_NewSession_ReturnsFalse()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act & Assert
			Assert.False(session.IsActive);
		}

		[Fact]
		public void CancelCurrentOperation_NoActiveSession_DoesNotThrow()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act & Assert - should not throw
			session.CancelCurrentOperation();
		}

		[Fact]
		public async Task ExecuteCommand_NoActiveSession_ReturnsErrorMessage()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act
			var result = await session.ExecuteCommand("version");

			// Assert
			Assert.Contains("No active debug session", result);
		}

		[Fact]
		public void Dispose_MultipleCallsSafe()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act & Assert - should not throw
			session.Dispose();
			session.Dispose(); // Second call should be safe
		}
	}
}
