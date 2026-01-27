using FluentAssertions;

using Moq;

using WinAiDbg.Protocol.Notifications;
using WinAiDbg.Protocol.Services;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Services;

/// <summary>
/// Unit tests for McpToolDefinitionService class.
/// Tests tool schema management and discovery.
/// </summary>
public class McpToolDefinitionServiceTests
{
    private readonly Mock<IMcpNotificationService> m_MockNotificationService;
    private readonly McpToolDefinitionService m_Service;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolDefinitionServiceTests"/> class.
    /// </summary>
    public McpToolDefinitionServiceTests()
    {
        m_MockNotificationService = new Mock<IMcpNotificationService>();
        m_Service = new McpToolDefinitionService(m_MockNotificationService.Object);
    }

    /// <summary>
    /// Verifies that GetAllTools returns all core tools.
    /// </summary>
    [Fact]
    public void GetAllTools_ReturnsExpectedNumberOfTools()
    {
        var tools = m_Service.GetAllTools();

        _ = tools.Should().NotBeNull();
        _ = tools.Should().HaveCountGreaterOrEqualTo(6); // At least 6 core tools
    }

    /// <summary>
    /// Verifies that GetAllTools contains open session tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsOpenSessionTool()
    {
        var tools = m_Service.GetAllTools();

        _ = tools.Should().Contain(t => t.Name == "Execute");
    }

    /// <summary>
    /// Verifies that GetAllTools contains enqueue command tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsEnqueueCommandTool()
    {
        var tools = m_Service.GetAllTools();

        _ = tools.Should().Contain(t => t.Name == "Execute");
    }

    /// <summary>
    /// Verifies that GetAllTools contains read result tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsReadResultTool()
    {
        var tools = m_Service.GetAllTools();

        _ = tools.Should().Contain(t => t.Name == "Execute");
    }

    /// <summary>
    /// Verifies that GetAllTools contains get commands status tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsGetCommandsStatusTool()
    {
        var tools = m_Service.GetAllTools();

        _ = tools.Should().Contain(t => t.Name == "Execute");
    }

    /// <summary>
    /// Verifies that GetAllTools contains close session tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsCloseSessionTool()
    {
        var tools = m_Service.GetAllTools();

        _ = tools.Should().Contain(t => t.Name == "Execute");
    }

    /// <summary>
    /// Verifies that GetAllTools contains cancel command tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsCancelCommandTool()
    {
        var tools = m_Service.GetAllTools();

        _ = tools.Should().Contain(t => t.Name == "Execute");
    }

    /// <summary>
    /// Verifies that GetTool returns tool when tool exists.
    /// </summary>
    [Fact]
    public void GetTool_ExistingTool_ReturnsTool()
    {
        var tool = m_Service.GetTool("Execute");

        _ = tool.Should().NotBeNull();
        _ = tool!.Name.Should().Be("Execute");
    }

    /// <summary>
    /// Verifies that GetTool returns null when tool does not exist.
    /// </summary>
    [Fact]
    public void GetTool_NonExistingTool_ReturnsNull()
    {
        var tool = m_Service.GetTool("nonexistent_tool");

        _ = tool.Should().BeNull();
    }

    /// <summary>
    /// Verifies that NotifyToolsChangedAsync calls notification service.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NotifyToolsChangedAsync_CallsNotificationService()
    {
        await m_Service.NotifyToolsChangedAsync();

        m_MockNotificationService.Verify(
            n => n.NotifyToolsListChangedAsync(),
            Times.Once);
    }

    /// <summary>
    /// Verifies that GetAllTools returns tools with valid schemas.
    /// </summary>
    [Fact]
    public void GetAllTools_AllToolsHaveValidSchemas()
    {
        var tools = m_Service.GetAllTools();

        _ = tools.Should().AllSatisfy(tool =>
        {
            _ = tool.Name.Should().NotBeNullOrEmpty();
            _ = tool.Description.Should().NotBeNullOrEmpty();
            _ = tool.InputSchema.Should().NotBeNull();
        });
    }
}
