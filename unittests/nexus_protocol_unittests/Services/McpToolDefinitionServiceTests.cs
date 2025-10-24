using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using nexus.protocol.Notifications;
using nexus.protocol.Services;

namespace nexus.protocol.unittests.Services;

/// <summary>
/// Unit tests for McpToolDefinitionService class.
/// Tests tool schema management and discovery.
/// </summary>
public class McpToolDefinitionServiceTests
{
    private readonly Mock<IMcpNotificationService> m_MockNotificationService;
    private readonly McpToolDefinitionService m_Service;

    /// <summary>
    /// Initializes a new instance of the McpToolDefinitionServiceTests class.
    /// </summary>
    public McpToolDefinitionServiceTests()
    {
        m_MockNotificationService = new Mock<IMcpNotificationService>();
        m_Service = new McpToolDefinitionService(m_MockNotificationService.Object);
    }

    /// <summary>
    /// Verifies that GetAllTools returns expected number of tools.
    /// </summary>
    [Fact]
    public void GetAllTools_ReturnsExpectedNumberOfTools()
    {
        var tools = m_Service.GetAllTools();

        tools.Should().NotBeNull();
        tools.Should().HaveCount(6);
    }

    /// <summary>
    /// Verifies that GetAllTools contains open session tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsOpenSessionTool()
    {
        var tools = m_Service.GetAllTools();

        tools.Should().Contain(t => t.Name == "nexus_open_dump_analyze_session");
    }

    /// <summary>
    /// Verifies that GetAllTools contains enqueue command tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsEnqueueCommandTool()
    {
        var tools = m_Service.GetAllTools();

        tools.Should().Contain(t => t.Name == "nexus_enqueue_async_dump_analyze_command");
    }

    /// <summary>
    /// Verifies that GetAllTools contains read result tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsReadResultTool()
    {
        var tools = m_Service.GetAllTools();

        tools.Should().Contain(t => t.Name == "nexus_read_dump_analyze_command_result");
    }

    /// <summary>
    /// Verifies that GetAllTools contains get commands status tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsGetCommandsStatusTool()
    {
        var tools = m_Service.GetAllTools();

        tools.Should().Contain(t => t.Name == "nexus_get_dump_analyze_commands_status");
    }

    /// <summary>
    /// Verifies that GetAllTools contains close session tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsCloseSessionTool()
    {
        var tools = m_Service.GetAllTools();

        tools.Should().Contain(t => t.Name == "nexus_close_dump_analyze_session");
    }

    /// <summary>
    /// Verifies that GetAllTools contains cancel command tool.
    /// </summary>
    [Fact]
    public void GetAllTools_ContainsCancelCommandTool()
    {
        var tools = m_Service.GetAllTools();

        tools.Should().Contain(t => t.Name == "nexus_cancel_command");
    }

    /// <summary>
    /// Verifies that GetTool returns tool when tool exists.
    /// </summary>
    [Fact]
    public void GetTool_ExistingTool_ReturnsTool()
    {
        var tool = m_Service.GetTool("nexus_open_dump_analyze_session");

        tool.Should().NotBeNull();
        tool!.Name.Should().Be("nexus_open_dump_analyze_session");
    }

    /// <summary>
    /// Verifies that GetTool returns null when tool does not exist.
    /// </summary>
    [Fact]
    public void GetTool_NonExistingTool_ReturnsNull()
    {
        var tool = m_Service.GetTool("nonexistent_tool");

        tool.Should().BeNull();
    }

    /// <summary>
    /// Verifies that NotifyToolsChangedAsync calls notification service.
    /// </summary>
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

        tools.Should().AllSatisfy(tool =>
        {
            tool.Name.Should().NotBeNullOrEmpty();
            tool.Description.Should().NotBeNullOrEmpty();
            tool.InputSchema.Should().NotBeNull();
        });
    }
}
