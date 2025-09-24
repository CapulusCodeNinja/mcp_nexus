using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;

namespace mcp_nexus.tests.Helper
{
	/// <summary>
	/// Tests for CdbSession input validation and error handling
	/// </summary>
	public class CdbSessionValidationTests
	{
		private readonly Mock<ILogger<CdbSession>> m_mockLogger;
		private readonly CdbSession m_session;

		public CdbSessionValidationTests()
		{
			m_mockLogger = new Mock<ILogger<CdbSession>>();
			m_session = new CdbSession(m_mockLogger.Object, 30000, null, 30000, 1, null, 2000);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public async Task StartSession_WithInvalidTarget_ThrowsArgumentException(string invalidTarget)
		{
			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(() => m_session.StartSession(invalidTarget));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public async Task ExecuteCommand_WithInvalidCommand_ThrowsArgumentException(string invalidCommand)
		{
			// Act & Assert
			await Assert.ThrowsAsync<ArgumentException>(() => m_session.ExecuteCommand(invalidCommand));
		}

		[Fact]
		public async Task ExecuteCommand_WhenNotActive_ThrowsInvalidOperationException()
		{
			// Arrange
			// Session is not started, so IsActive should be false

			// Act & Assert
			await Assert.ThrowsAsync<InvalidOperationException>(() => m_session.ExecuteCommand("version"));
		}

		[Fact]
		public void CancelCurrentOperation_WhenNotActive_DoesNotThrow()
		{
			// Arrange
			// Session is not started, so IsActive should be false

			// Act & Assert
			var exception = Record.Exception(() => m_session.CancelCurrentOperation());
			Assert.Null(exception);
		}

		[Fact]
		public async Task StopSession_WhenNotActive_ReturnsFalse()
		{
			// Arrange
			// Session is not started, so IsActive should be false

			// Act
			var result = await m_session.StopSession();

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void IsActive_InitialState_ReturnsFalse()
		{
			// Act
			var isActive = m_session.IsActive;

			// Assert
			Assert.False(isActive);
		}

	[Fact]
	public async Task StartSession_WithVeryLongTarget_HandlesGracefully()
	{
		// Arrange
		var longTarget = new string('a', 1000);

		// Act & Assert - Should not throw an exception, regardless of success/failure
		var exception = await Record.ExceptionAsync(() => m_session.StartSession(longTarget));
		
		// The key is that it handles the long target gracefully without crashing
		Assert.Null(exception);
	}

		[Fact]
		public async Task StartSession_WithSpecialCharacters_HandlesGracefully()
		{
			// Arrange
			var specialCharTarget = "-z \"C:\\Test\\File With Spaces & Special @#$%.dmp\"";

			// Act
			var result = await m_session.StartSession(specialCharTarget);

			// Assert
			Assert.IsType<bool>(result); // Should not crash
		}

		[Fact]
		public void Constructor_WithNegativeTimeout_ThrowsArgumentException()
		{
			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(m_mockLogger.Object, -1000, null, 30000, 1, null, 2000));
		}

		[Fact]
		public void Constructor_WithZeroTimeout_ThrowsArgumentException()
		{
			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(m_mockLogger.Object, 0, null, 30000, 1, null, 2000));
		}

		[Fact]
		public void Constructor_WithNegativeSymbolTimeout_ThrowsArgumentException()
		{
			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(m_mockLogger.Object, 30000, null, -1000, 1, null, 2000));
		}

		[Fact]
		public void Constructor_WithNegativeRetries_ThrowsArgumentException()
		{
			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(m_mockLogger.Object, 30000, null, 30000, -1, null, 2000));
		}

		[Fact]
		public void Constructor_WithNegativeStartupDelay_ThrowsArgumentException()
		{
			// Act & Assert
			Assert.Throws<ArgumentOutOfRangeException>(() => 
				new CdbSession(m_mockLogger.Object, 30000, null, 30000, 1, null, -1000));
		}

		[Fact]
		public void Constructor_WithValidParameters_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => 
				new CdbSession(m_mockLogger.Object, 30000, null, 30000, 1, null, 2000));
			Assert.Null(exception);
		}

		[Fact]
		public void Constructor_WithCustomCdbPath_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => 
				new CdbSession(m_mockLogger.Object, 30000, @"C:\Custom\Path\cdb.exe", 30000, 1, null, 2000));
			Assert.Null(exception);
		}

		[Fact]
		public void Constructor_WithCustomSymbolPath_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => 
				new CdbSession(m_mockLogger.Object, 30000, null, 30000, 1, @"C:\Symbols", 2000));
			Assert.Null(exception);
		}

		[Fact]
		public void Dispose_WhenCalled_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => m_session.Dispose());
			Assert.Null(exception);
		}

		[Fact]
		public void Dispose_CalledMultipleTimes_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => 
			{
				m_session.Dispose();
				m_session.Dispose();
				m_session.Dispose();
			});
			Assert.Null(exception);
		}

		[Fact]
		public async Task StartSession_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_session.Dispose();

			// Act & Assert
			await Assert.ThrowsAsync<ObjectDisposedException>(() => 
				m_session.StartSession("-z \"test.dmp\""));
		}

		[Fact]
		public async Task ExecuteCommand_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_session.Dispose();

			// Act & Assert
			await Assert.ThrowsAsync<ObjectDisposedException>(() => 
				m_session.ExecuteCommand("version"));
		}

		[Fact]
		public void CancelCurrentOperation_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_session.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => m_session.CancelCurrentOperation());
		}

		[Fact]
		public async Task StopSession_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_session.Dispose();

			// Act & Assert
			await Assert.ThrowsAsync<ObjectDisposedException>(() => m_session.StopSession());
		}

		[Fact]
		public void IsActive_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_session.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => _ = m_session.IsActive);
		}
	}
}
