using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mcp_nexus.Helper;
using Xunit;

namespace mcp_nexus_tests.Helper
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
		public async Task ExecuteCommand_NoActiveSession_ThrowsInvalidOperationException()
		{
			// Arrange
			var session = new CdbSession(CreateNullLogger());

			// Act & Assert
			await Assert.ThrowsAsync<InvalidOperationException>(() => session.ExecuteCommand("version"));
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
