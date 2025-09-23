using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using Xunit;

namespace mcp_nexus.tests.Services
{
	public class CommandQueueServiceTests
	{
		private static ILogger<T> CreateNullLogger<T>() => LoggerFactory.Create(b => { }).CreateLogger<T>();

		[Fact]
		public async Task QueueCommand_Executes_And_ReturnsResult()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync("OK");

			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger<CommandQueueService>());

			// Act
			var id = svc.QueueCommand("version");
			var status1 = await svc.GetCommandResult(id); // likely still executing

			// allow background processor to run
			await Task.Delay(100);

			var status2 = await svc.GetCommandResult(id);

			// Assert
			Assert.Contains("OK", status2);
		}

		[Fact]
		public async Task CancelCommand_Cancels_Current_And_Requests_Cdb_Cancel()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			// Block ExecuteCommand until cancelled
			cdbMock.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					try
					{
						await Task.Delay(5000, ct);
					}
					catch (TaskCanceledException) { }
					return "Cancelled";
				});

			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger<CommandQueueService>());

			// Act
			var id = svc.QueueCommand("!analyze -v");
			await Task.Delay(50);
			var ok = svc.CancelCommand(id);

			// Assert
			Assert.True(ok);
			cdbMock.Verify(x => x.CancelCurrentOperation(), Times.AtLeastOnce);

			var result = await svc.GetCommandResult(id);
			Assert.Contains("cancel", result, System.StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task CancelAllCommands_Cancels_Queued_And_Current_Without_Exceptions()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns<string, CancellationToken>(async (cmd, ct) =>
				{
					try { await Task.Delay(5000, ct); } catch (TaskCanceledException) { }
					return "Cancelled";
				});

			var svc = new CommandQueueService(cdbMock.Object, CreateNullLogger<CommandQueueService>());

			// queue several
			var id1 = svc.QueueCommand("k");
			var id2 = svc.QueueCommand("r");
			await Task.Delay(50);

			// Act
			var count = svc.CancelAllCommands("Test");

			// Assert
			Assert.True(count >= 1);
			cdbMock.Verify(x => x.CancelCurrentOperation(), Times.AtLeastOnce);

			var r1 = await svc.GetCommandResult(id1);
			var r2 = await svc.GetCommandResult(id2);
			Assert.Contains("cancel", r1, System.StringComparison.OrdinalIgnoreCase);
			Assert.Contains("cancel", r2, System.StringComparison.OrdinalIgnoreCase);
		}
	}
}
