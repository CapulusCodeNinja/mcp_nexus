using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Tools;
using Xunit;

namespace mcp_nexus_tests.Tools
{
	public class ExtendedWindbgToolTests
	{
		private static ILogger<WindbgTool> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<WindbgTool>();

		[Fact]
		public async Task NexusExecDebuggerCommandAsync_EmptyCommand_ReturnsError()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusExecDebuggerCommandAsync("");

			// Assert
			Assert.Contains("cannot be null or empty", result);
		}

		[Fact]
		public async Task NexusExecDebuggerCommandAsync_WhitespaceCommand_ReturnsError()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusExecDebuggerCommandAsync("   ");

			// Assert
			Assert.Contains("cannot be null or empty", result);
		}

		[Fact]
		public async Task NexusDebuggerCommandStatus_NonExistentCommand_ReturnsError()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			queueMock.Setup(x => x.GetCommandResult("invalid")).ReturnsAsync("Command not found: invalid");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusDebuggerCommandStatus("invalid");

			// Assert
			Assert.Contains("error", result);
			Assert.Contains("Command not found", result);
		}

		[Fact]
		public async Task NexusDebuggerCommandStatus_QueuedCommand_ReturnsQueuedStatus()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			queueMock.Setup(x => x.GetCommandResult("test-id")).ReturnsAsync("Command is still executing...");
			queueMock.Setup(x => x.GetQueueStatus()).Returns(new[]
			{
				("test-id", "version", DateTime.UtcNow.AddSeconds(-5), "Queued")
			});

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusDebuggerCommandStatus("test-id");

			// Assert
			Assert.Contains("queued", result);
			Assert.Contains("Check again in 5-10 seconds", result);
		}

		[Fact]
		public async Task NexusDebuggerCommandStatus_CancelledCommand_ReturnsCancelledStatus()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			queueMock.Setup(x => x.GetCommandResult("test-id")).ReturnsAsync("Command is still executing...");
			queueMock.Setup(x => x.GetQueueStatus()).Returns(new[]
			{
				("test-id", "version", DateTime.UtcNow.AddSeconds(-5), "Cancelled")
			});

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusDebuggerCommandStatus("test-id");

			// Assert
			Assert.Contains("cancelled", result);
			Assert.Contains("Command was cancelled", result);
		}

		[Fact]
		public async Task NexusListDebuggerCommands_WithCurrentAndQueuedCommands_ReturnsFormattedList()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			var currentCommand = new QueuedCommand("current-id", "!analyze -v", DateTime.UtcNow.AddSeconds(-30), 
				new TaskCompletionSource<string>(), new CancellationTokenSource());
			
			queueMock.Setup(x => x.GetCurrentCommand()).Returns(currentCommand);
			queueMock.Setup(x => x.GetQueueStatus()).Returns(new[]
			{
				("queue-id-1", "version", DateTime.UtcNow.AddSeconds(-10), "Queued"),
				("queue-id-2", "k", DateTime.UtcNow.AddSeconds(-5), "Queued")
			});

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusListDebuggerCommands();

			// Assert
			Assert.Contains("currentlyExecuting", result);
			Assert.Contains("current-id", result);
			Assert.Contains("!analyze -v", result);
			Assert.Contains("queuedCommands", result);
			Assert.Contains("queue-id-1", result);
			Assert.Contains("queue-id-2", result);
		}

		[Fact]
		public async Task NexusListDebuggerCommands_NoCommands_ReturnsEmptyList()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			queueMock.Setup(x => x.GetCurrentCommand()).Returns((QueuedCommand?)null);
			queueMock.Setup(x => x.GetQueueStatus()).Returns(Array.Empty<(string, string, DateTime, string)>());

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusListDebuggerCommands();

			// Assert
			Assert.Contains("currentlyExecuting\": null", result);
			Assert.Contains("queueSize\": 0", result);
		}

		[Fact]
		public async Task NexusOpenDump_EmptyPath_ReturnsError()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusOpenDump("");

			// Assert
			Assert.Contains("cannot be null or empty", result);
		}

		[Fact]
		public async Task NexusOpenDump_WithSymbolsPath_CallsStartSessionWithSymbols()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.Setup(x => x.StartSession(It.IsAny<string>(), null)).ReturnsAsync(true);

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Create temporary files
			var tempDump = Path.GetTempFileName();
			var tempSymbols = Path.GetTempPath();
			
			try
			{
				// Act
				var result = await tool.NexusOpenDump(tempDump, tempSymbols);

				// Assert
				Assert.Contains("Successfully opened", result);
				cdbMock.Verify(x => x.StartSession(It.Is<string>(s => s.Contains(tempSymbols) && s.Contains(tempDump)), null), Times.Once);
			}
			finally
			{
				File.Delete(tempDump);
			}
		}

		[Fact]
		public async Task NexusOpenDump_StartSessionFails_ReturnsError()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.Setup(x => x.StartSession(It.IsAny<string>(), null)).ReturnsAsync(false);

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			var tempDump = Path.GetTempFileName();
			
			try
			{
				// Act
				var result = await tool.NexusOpenDump(tempDump);

				// Assert
				Assert.Contains("Failed to open", result);
			}
			finally
			{
				File.Delete(tempDump);
			}
		}

		[Fact]
		public async Task NexusStartRemoteDebug_EmptyConnectionString_ReturnsError()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.NexusStartRemoteDebug("");

			// Assert
			Assert.Contains("cannot be null or empty", result);
		}

		[Fact]
		public async Task NexusStartRemoteDebug_WithSymbolsPath_CallsStartSessionWithSymbols()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.Setup(x => x.StartSession(It.IsAny<string>(), null)).ReturnsAsync(true);

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			var tempSymbols = Path.GetTempPath();

			// Act
			var result = await tool.NexusStartRemoteDebug("tcp:Port=5005,Server=localhost", tempSymbols);

			// Assert
			Assert.Contains("Successfully connected", result);
			cdbMock.Verify(x => x.StartSession(It.Is<string>(s => s.Contains(tempSymbols) && s.Contains("tcp:Port=5005")), null), Times.Once);
		}

		// GetSessionInfo test removed - method is obsolete
	}
}

