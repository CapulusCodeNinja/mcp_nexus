using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Tools;
using Xunit;

namespace mcp_nexus_tests.Tools
{
	public class WindbgToolTests
	{
		private static ILogger<T> CreateNullLogger<T>() => LoggerFactory.Create(b => { }).CreateLogger<T>();

		[Fact]
		public async Task CloseWindbgDump_CallsCancelAllAndStopSession()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.StopSession()).ReturnsAsync(true);
			queueMock.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(3);

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.CloseWindbgDump();

			// Assert
			Assert.Contains("Successfully closed", result);
			queueMock.Verify(x => x.CancelAllCommands("Session stop requested"), Times.Once);
			cdbMock.Verify(x => x.StopSession(), Times.Once);
		}

		[Fact]
		public async Task CloseWindbgRemote_CallsCancelAllAndStopSession()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.StopSession()).ReturnsAsync(true);
			queueMock.Setup(x => x.CancelAllCommands(It.IsAny<string>())).Returns(2);

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.CloseWindbgRemote();

			// Assert
			Assert.Contains("Successfully disconnected", result);
			queueMock.Verify(x => x.CancelAllCommands("Remote session stop requested"), Times.Once);
			cdbMock.Verify(x => x.StopSession(), Times.Once);
		}

		[Fact]
		public async Task CloseWindbgDump_NoActiveSession_ReturnsNoActiveMessage()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(false);

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.CloseWindbgDump();

			// Assert
			Assert.Contains("No active session", result);
			queueMock.Verify(x => x.CancelAllCommands(It.IsAny<string>()), Times.Never);
			cdbMock.Verify(x => x.StopSession(), Times.Never);
		}

		[Fact]
		public async Task RunWindbgCmdAsync_ReturnsCommandId()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			queueMock.Setup(x => x.QueueCommand("version")).Returns("test-id-123");

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.RunWindbgCmdAsync("version");

			// Assert
			Assert.Contains("test-id-123", result);
			Assert.Contains("commandId", result);
			Assert.Contains("QUEUED", result);
			queueMock.Verify(x => x.QueueCommand("version"), Times.Once);
		}

		[Fact]
		public async Task RunWindbgCmdAsync_NoActiveSession_ReturnsErrorMessage()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(false);

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.RunWindbgCmdAsync("version");

			// Assert
			Assert.Contains("No active debugging session", result);
			queueMock.Verify(x => x.QueueCommand(It.IsAny<string>()), Times.Never);
		}

		[Fact]
		public async Task GetCommandStatus_CompletedCommand_ReturnsResult()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			queueMock.Setup(x => x.GetCommandResult("test-id")).ReturnsAsync("Microsoft (R) Windows Debugger Version");

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.GetCommandStatus("test-id");

			// Assert
			Assert.Contains("completed", result);
			Assert.Contains("Microsoft (R) Windows Debugger Version", result);
			Assert.Contains("result", result);
		}

		[Fact]
		public async Task GetCommandStatus_StillExecuting_ReturnsStatus()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			queueMock.Setup(x => x.GetCommandResult("test-id")).ReturnsAsync("Command is still executing...");
			queueMock.Setup(x => x.GetQueueStatus()).Returns(new[]
			{
				("test-id", "!analyze -v", DateTime.UtcNow.AddSeconds(-30), "Executing")
			});

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.GetCommandStatus("test-id");

			// Assert
			Assert.Contains("executing", result);
			Assert.Contains("!analyze -v", result);
			Assert.Contains("waitTimeSeconds", result);
		}

		[Fact]
		public async Task CancelCommand_ValidId_ReturnsSuccessMessage()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			queueMock.Setup(x => x.CancelCommand("test-id")).Returns(true);

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.CancelCommand("test-id");

			// Assert
			Assert.Contains("CANCELLED", result);
			Assert.Contains("test-id", result);
			queueMock.Verify(x => x.CancelCommand("test-id"), Times.Once);
		}

		[Fact]
		public async Task CancelCommand_InvalidId_ReturnsErrorMessage()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			queueMock.Setup(x => x.CancelCommand("invalid-id")).Returns(false);

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.CancelCommand("invalid-id");

			// Assert
			Assert.Contains("error", result);
			Assert.Contains("not found", result);
		}

		[Fact]
		public async Task OpenWindbgDump_ValidPath_CallsStartSession()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.Setup(x => x.StartSession(It.IsAny<string>(), null)).ReturnsAsync(true);

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Create a temporary dump file for testing
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "dummy dump");

			try
			{
				// Act
				var result = await tool.OpenWindbgDump(tempFile);

				// Assert
				Assert.Contains("Successfully opened", result);
				cdbMock.Verify(x => x.StartSession(It.Is<string>(s => s.Contains(tempFile)), null), Times.Once);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public async Task OpenWindbgDump_InvalidPath_ReturnsError()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();

			var tool = new WindbgTool(CreateNullLogger<WindbgTool>(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.OpenWindbgDump("nonexistent.dmp");

			// Assert
			Assert.Contains("not found", result);
			cdbMock.Verify(x => x.StartSession(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		}
	}
}
