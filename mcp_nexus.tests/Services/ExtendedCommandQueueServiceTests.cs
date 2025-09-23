using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using Xunit;

namespace mcp_nexus.tests.Services
{
	public class ExtendedCommandQueueServiceTests
	{
		private static ILogger<CommandQueueService> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<CommandQueueService>();

		[Fact]
		public async Task GetCommandResult_NonExistentCommand_ReturnsNotFound()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Act
			var result = await svc.GetCommandResult("non-existent-id");

			// Assert
			Assert.Contains("Command not found", result);
		}

		[Fact]
		public void CancelCommand_NonExistentCommand_ReturnsFalse()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Act
			var result = svc.CancelCommand("non-existent-id");

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void GetCurrentCommand_NoCurrentCommand_ReturnsNull()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Act
			var result = svc.GetCurrentCommand();

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetQueueStatus_EmptyQueue_ReturnsEmpty()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Act
			var result = svc.GetQueueStatus();

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public async Task QueueCommand_CdbSessionThrows_HandlesException()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("Test exception"));

			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Act
			var id = svc.QueueCommand("test");
			await Task.Delay(100); // Let background processor run

			var result = await svc.GetCommandResult(id);

			// Assert
			Assert.Contains("failed", result);
		}

		[Fact]
		public async Task CancelAllCommands_WithReason_IncludesReasonInResult()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					await Task.Delay(5000, ct);
					return "OK";
				});

			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Queue some commands
			var id1 = svc.QueueCommand("cmd1");
			var id2 = svc.QueueCommand("cmd2");
			await Task.Delay(50);

			// Act
			var count = svc.CancelAllCommands("Test shutdown");

			// Assert
			Assert.True(count >= 1);
			var result1 = await svc.GetCommandResult(id1);
			var result2 = await svc.GetCommandResult(id2);
			Assert.Contains("Test shutdown", result1);
			Assert.Contains("Test shutdown", result2);
		}

		[Fact]
		public async Task GetQueueStatus_WithQueuedAndExecutingCommands_ReturnsCorrectStatus()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					await Task.Delay(2000, ct);
					return "OK";
				});

			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Act
			var id1 = svc.QueueCommand("executing-cmd");
			var id2 = svc.QueueCommand("queued-cmd");
			await Task.Delay(50); // Let first command start executing

			var statuses = svc.GetQueueStatus().ToList();

			// Assert
			Assert.NotEmpty(statuses);
			var executingCmd = statuses.FirstOrDefault(s => s.Status == "Executing");
			
			Assert.NotEqual(default, executingCmd);
			Assert.Equal("executing-cmd", executingCmd.Command);
		}

		[Fact]
		public void Dispose_CalledTwice_DoesNotThrow()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Act & Assert - should not throw
			svc.Dispose();
			svc.Dispose();
		}

		[Fact]
		public async Task CommandExecution_WithCancellationToken_PropagatesCancellation()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					try
					{
						await Task.Delay(5000, ct);
						return "OK";
					}
					catch (OperationCanceledException)
					{
						return "Cancelled";
					}
				});

			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger());

			// Act
			var id = svc.QueueCommand("long-running-cmd");
			await Task.Delay(50); // Let command start
			
			var cancelled = svc.CancelCommand(id);
			await Task.Delay(100); // Let cancellation propagate

			var result = await svc.GetCommandResult(id);

			// Assert
			Assert.True(cancelled);
			Assert.Contains("cancel", result, StringComparison.OrdinalIgnoreCase);
		}
	}
}
