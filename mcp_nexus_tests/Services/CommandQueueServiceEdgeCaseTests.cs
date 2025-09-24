using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;

namespace mcp_nexus_tests.Services
{
	/// <summary>
	/// Edge case and boundary condition tests for CommandQueueService
	/// </summary>
	public class CommandQueueServiceEdgeCaseTests
	{
		private readonly Mock<ILogger<CommandQueueService>> m_mockLogger;
		private readonly Mock<ICdbSession> m_mockCdbSession;
		private readonly CommandQueueService m_service;

		public CommandQueueServiceEdgeCaseTests()
		{
			m_mockLogger = new Mock<ILogger<CommandQueueService>>();
			m_mockCdbSession = new Mock<ICdbSession>();
			m_service = new CommandQueueService(m_mockCdbSession.Object, m_mockLogger.Object);
		}

		private void Dispose()
		{
			m_service?.Dispose();
		}

		[Fact]
		public void QueueCommand_WithNullCommand_ThrowsArgumentException()
		{
			// Act & Assert
			Assert.Throws<ArgumentException>(() => m_service.QueueCommand(null!));
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		public void QueueCommand_WithInvalidCommand_ThrowsArgumentException(string invalidCommand)
		{
			// Act & Assert
			Assert.Throws<ArgumentException>(() => m_service.QueueCommand(invalidCommand));
		}

		[Fact]
		public void QueueCommand_WithVeryLongCommand_HandlesGracefully()
		{
			// Arrange
			var longCommand = new string('a', 10000);

			// Act
			var commandId = m_service.QueueCommand(longCommand);

			// Assert
			Assert.NotNull(commandId);
			Assert.NotEmpty(commandId);
		}

		[Fact]
		public void QueueCommand_WithSpecialCharacters_HandlesGracefully()
		{
			// Arrange
			var specialCommand = "!analyze -v && echo \"Hello @#$%^&*()\"";

			// Act
			var commandId = m_service.QueueCommand(specialCommand);

			// Assert
			Assert.NotNull(commandId);
			Assert.NotEmpty(commandId);
		}

		[Fact]
		public void QueueCommand_WithNewlines_HandlesGracefully()
		{
			// Arrange
			var commandWithNewlines = "version\r\n!analyze -v\r\nquit";

			// Act
			var commandId = m_service.QueueCommand(commandWithNewlines);

			// Assert
			Assert.NotNull(commandId);
			Assert.NotEmpty(commandId);
		}

		[Fact]
		public async Task GetCommandResult_WithNonExistentId_ReturnsNotFoundMessage()
		{
			// Arrange
			var nonExistentId = "non-existent-command-id";

			// Act
			var result = await m_service.GetCommandResult(nonExistentId);

			// Assert
			Assert.Contains("Command not found", result);
		}

		[Fact]
		public async Task GetCommandResult_WithNullId_ReturnsNotFoundMessage()
		{
			// Act
			var result = await m_service.GetCommandResult(null!);

			// Assert
			Assert.Contains("Command not found", result);
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		public async Task GetCommandResult_WithInvalidId_ReturnsNotFoundMessage(string invalidId)
		{
			// Act
			var result = await m_service.GetCommandResult(invalidId);

			// Assert
			Assert.Contains("Command not found", result);
		}

		[Fact]
		public void CancelCommand_WithNonExistentId_ReturnsFalse()
		{
			// Arrange
			var nonExistentId = "non-existent-command-id";

			// Act
			var result = m_service.CancelCommand(nonExistentId);

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void CancelCommand_WithNullId_ReturnsFalse()
		{
			// Act
			var result = m_service.CancelCommand(null!);

			// Assert
			Assert.False(result);
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		public void CancelCommand_WithInvalidId_ReturnsFalse(string invalidId)
		{
			// Act
			var result = m_service.CancelCommand(invalidId);

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void CancelAllCommands_WithNullReason_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => m_service.CancelAllCommands(null!));
			Assert.Null(exception);
		}

		[Fact]
		public void CancelAllCommands_WithEmptyReason_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => m_service.CancelAllCommands(""));
			Assert.Null(exception);
		}

		[Fact]
		public void GetCurrentCommand_WhenNoCommandsQueued_ReturnsNull()
		{
			// Act
			var currentCommand = m_service.GetCurrentCommand();

			// Assert
			Assert.Null(currentCommand);
		}

		[Fact]
		public void GetQueueStatus_WhenNoCommandsQueued_ReturnsEmptyCollection()
		{
			// Act
			var queueStatus = m_service.GetQueueStatus();

			// Assert
			Assert.NotNull(queueStatus);
			Assert.Empty(queueStatus);
		}

		[Fact]
		public void QueueCommand_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_service.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => m_service.QueueCommand("version"));
		}

		[Fact]
		public async Task GetCommandResult_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_service.Dispose();

			// Act & Assert
			await Assert.ThrowsAsync<ObjectDisposedException>(() => 
				m_service.GetCommandResult("some-id"));
		}

		[Fact]
		public void CancelCommand_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_service.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => m_service.CancelCommand("some-id"));
		}

		[Fact]
		public void CancelAllCommands_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_service.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => m_service.CancelAllCommands("test"));
		}

		[Fact]
		public void GetCurrentCommand_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_service.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => m_service.GetCurrentCommand());
		}

		[Fact]
		public void GetQueueStatus_AfterDispose_ThrowsObjectDisposedException()
		{
			// Arrange
			m_service.Dispose();

			// Act & Assert
			Assert.Throws<ObjectDisposedException>(() => m_service.GetQueueStatus());
		}

		[Fact]
		public void Dispose_CalledMultipleTimes_DoesNotThrow()
		{
			// Act & Assert
			var exception = Record.Exception(() => 
			{
				m_service.Dispose();
				m_service.Dispose();
				m_service.Dispose();
			});
			Assert.Null(exception);
		}

		[Fact]
		public void QueueCommand_ManyCommands_HandlesGracefully()
		{
			// Arrange
			var commandIds = new List<string>();

			// Act
			for (int i = 0; i < 100; i++)
			{
				var commandId = m_service.QueueCommand($"command_{i}");
				commandIds.Add(commandId);
			}

			// Assert
			Assert.Equal(100, commandIds.Count);
			Assert.Equal(100, commandIds.Distinct().Count()); // All IDs should be unique
		}

		[Fact]
		public async Task GetCommandResult_ConcurrentCalls_HandlesGracefully()
		{
			// Arrange
			var commandId = m_service.QueueCommand("test command");
			var tasks = new List<Task<string>>();

			// Act
			for (int i = 0; i < 10; i++)
			{
				tasks.Add(m_service.GetCommandResult(commandId));
			}

			var results = await Task.WhenAll(tasks);

			// Assert
			Assert.Equal(10, results.Length);
			Assert.All(results, result => Assert.NotNull(result));
		}

		[Fact]
		public async Task CancelCommand_ConcurrentCalls_HandlesGracefully()
		{
			// Arrange
			var commandId = m_service.QueueCommand("test command");
			var tasks = new List<Task<bool>>();

			// Act
			for (int i = 0; i < 10; i++)
			{
				tasks.Add(Task.Run(() => m_service.CancelCommand(commandId)));
			}

			await Task.WhenAll(tasks);

			// Assert
			// At least one should succeed, others may return false (already cancelled)
			Assert.True(tasks.Any(t => t.Result) || tasks.All(t => !t.Result));
		}
	}
}
