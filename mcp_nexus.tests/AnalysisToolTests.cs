using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Tools;
using Xunit;

namespace mcp_nexus.tests
{
	public class AnalysisToolTests
	{
		private static ILogger<WindbgTool> CreateNullLogger() => LoggerFactory.Create(b => { }).CreateLogger<WindbgTool>();

		[Fact]
		public async Task GetSessionInfo_ActiveSession_ReturnsSessionDetails()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand("version")).ReturnsAsync("Microsoft (R) Windows Debugger Version 10.0");
			cdbMock.Setup(x => x.ExecuteCommand("!process 0 0")).ReturnsAsync("Process information...");
			cdbMock.Setup(x => x.ExecuteCommand("~")).ReturnsAsync("Thread information...");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.GetSessionInfo();

			// Assert
			Assert.Contains("Version Information", result);
			Assert.Contains("Process Information", result);
			Assert.Contains("Thread Information", result);
			Assert.Contains("Microsoft (R) Windows Debugger", result);
		}

		[Fact]
		public async Task AnalyzeCallStack_ActiveSession_ReturnsCallStackDetails()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand("k")).ReturnsAsync("Call stack trace...");
			cdbMock.Setup(x => x.ExecuteCommand(".ecxr")).ReturnsAsync("No exception context available");
			cdbMock.Setup(x => x.ExecuteCommand("r")).ReturnsAsync("Register values...");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeCallStack();

			// Assert
			Assert.Contains("Call Stack", result);
			Assert.Contains("Registers", result);
			Assert.Contains("Call stack trace", result);
			Assert.Contains("Register values", result);
		}

		[Fact]
		public async Task AnalyzeCallStack_WithExceptionContext_IncludesExceptionInfo()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand("k")).ReturnsAsync("Call stack trace...");
			cdbMock.Setup(x => x.ExecuteCommand(".ecxr")).ReturnsAsync("Exception context available");
			cdbMock.Setup(x => x.ExecuteCommand("r")).ReturnsAsync("Register values...");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeCallStack();

			// Assert
			Assert.Contains("Exception Context", result);
			Assert.Contains("Exception context available", result);
		}

		[Fact]
		public async Task AnalyzeMemory_ActiveSession_ReturnsMemoryDetails()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand("!vprot")).ReturnsAsync("Virtual memory information...");
			cdbMock.Setup(x => x.ExecuteCommand("!heap -s")).ReturnsAsync("Heap summary...");
			cdbMock.Setup(x => x.ExecuteCommand("lm")).ReturnsAsync("Loaded modules...");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeMemory();

			// Assert
			Assert.Contains("Virtual Memory Information", result);
			Assert.Contains("Heap Information", result);
			Assert.Contains("Loaded Modules", result);
			Assert.Contains("Virtual memory information", result);
			Assert.Contains("Heap summary", result);
			Assert.Contains("Loaded modules", result);
		}

		[Fact]
		public async Task AnalyzeCrashPatterns_NoPatterns_ReturnsSuccess()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(".ecxr")).ReturnsAsync("No exception");
			cdbMock.Setup(x => x.ExecuteCommand("k")).ReturnsAsync("Call stack with 5 frames");
			cdbMock.Setup(x => x.ExecuteCommand("!heap -s")).ReturnsAsync("Heap is fine");
			cdbMock.Setup(x => x.ExecuteCommand("~*k")).ReturnsAsync("Normal threads");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeCrashPatterns();

			// Assert
			Assert.Contains("No obvious crash patterns detected", result);
			Assert.Contains("✅", result);
		}

		[Fact]
		public async Task AnalyzeCrashPatterns_AccessViolation_DetectsPattern()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(".ecxr")).ReturnsAsync("Access violation reading location 0x00000000");
			cdbMock.Setup(x => x.ExecuteCommand("k")).ReturnsAsync("Call stack with 5 frames");
			cdbMock.Setup(x => x.ExecuteCommand("!heap -s")).ReturnsAsync("Heap is fine");
			cdbMock.Setup(x => x.ExecuteCommand("~*k")).ReturnsAsync("Normal threads");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeCrashPatterns();

			// Assert
			Assert.Contains("ACCESS VIOLATION DETECTED", result);
			Assert.Contains("⚠️", result);
			Assert.Contains("Dereferencing null or invalid pointers", result);
		}

		[Fact]
		public async Task AnalyzeCrashPatterns_StackOverflow_DetectsPattern()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			// Create a long call stack (>100 frames)
			var longCallStack = string.Join("\n", Enumerable.Range(0, 150).Select(i => $"Frame {i}: function_{i}"));
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(".ecxr")).ReturnsAsync("No exception");
			cdbMock.Setup(x => x.ExecuteCommand("k")).ReturnsAsync(longCallStack);
			cdbMock.Setup(x => x.ExecuteCommand("!heap -s")).ReturnsAsync("Heap is fine");
			cdbMock.Setup(x => x.ExecuteCommand("~*k")).ReturnsAsync("Normal threads");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeCrashPatterns();

			// Assert
			Assert.Contains("POTENTIAL STACK OVERFLOW", result);
			Assert.Contains("⚠️", result);
			Assert.Contains("Infinite recursion", result);
		}

		[Fact]
		public async Task AnalyzeCrashPatterns_HeapCorruption_DetectsPattern()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(".ecxr")).ReturnsAsync("No exception");
			cdbMock.Setup(x => x.ExecuteCommand("k")).ReturnsAsync("Call stack with 5 frames");
			cdbMock.Setup(x => x.ExecuteCommand("!heap -s")).ReturnsAsync("Heap corrupted at address 0x12345678");
			cdbMock.Setup(x => x.ExecuteCommand("~*k")).ReturnsAsync("Normal threads");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeCrashPatterns();

			// Assert
			Assert.Contains("HEAP CORRUPTION DETECTED", result);
			Assert.Contains("⚠️", result);
			Assert.Contains("Buffer overruns", result);
		}

		[Fact]
		public async Task AnalyzeCrashPatterns_PotentialDeadlock_DetectsPattern()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(".ecxr")).ReturnsAsync("No exception");
			cdbMock.Setup(x => x.ExecuteCommand("k")).ReturnsAsync("Call stack with 5 frames");
			cdbMock.Setup(x => x.ExecuteCommand("!heap -s")).ReturnsAsync("Heap is fine");
			cdbMock.Setup(x => x.ExecuteCommand("~*k")).ReturnsAsync("Thread waiting on WaitForSingleObject");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeCrashPatterns();

			// Assert
			Assert.Contains("POTENTIAL DEADLOCK DETECTED", result);
			Assert.Contains("⚠️", result);
			Assert.Contains("waiting for synchronization objects", result);
		}

		[Fact]
		public async Task AnalyzeCrashPatterns_MultiplePatterns_DetectsAll()
		{
			// Arrange
			var cdbMock = new Mock<ICdbSession>();
			var queueMock = new Mock<ICommandQueueService>();
			
			var longCallStack = string.Join("\n", Enumerable.Range(0, 150).Select(i => $"Frame {i}: function_{i}"));
			
			cdbMock.SetupGet(x => x.IsActive).Returns(true);
			cdbMock.Setup(x => x.ExecuteCommand(".ecxr")).ReturnsAsync("Access violation reading location 0x00000000");
			cdbMock.Setup(x => x.ExecuteCommand("k")).ReturnsAsync(longCallStack);
			cdbMock.Setup(x => x.ExecuteCommand("!heap -s")).ReturnsAsync("Heap corrupted at address 0x12345678");
			cdbMock.Setup(x => x.ExecuteCommand("~*k")).ReturnsAsync("Thread waiting on WaitForMultipleObjects");

			var tool = new WindbgTool(CreateNullLogger(), cdbMock.Object, queueMock.Object);

			// Act
			var result = await tool.AnalyzeCrashPatterns();

			// Assert
			Assert.Contains("ACCESS VIOLATION DETECTED", result);
			Assert.Contains("POTENTIAL STACK OVERFLOW", result);
			Assert.Contains("HEAP CORRUPTION DETECTED", result);
			Assert.Contains("POTENTIAL DEADLOCK DETECTED", result);
		}
	}
}
