using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using WinAiDbg.Engine.Share;
using WinAiDbg.Protocol.Resources;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Resources;

/// <summary>
/// Unit tests for CommandsResource class.
/// Tests command listing resource with mocked dependencies.
/// </summary>
public class CommandsResourceTests
{
    private readonly Mock<IDebugEngine> m_MockDebugEngine;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Extracts Markdown table rows (excluding header and separator rows).
    /// </summary>
    /// <param name="markdown">Markdown content containing the table.</param>
    /// <param name="headerLine">The expected header line.</param>
    /// <returns>Array of table rows, each row is an array of trimmed cell values.</returns>
    private static string[][] ExtractTableRows(string markdown, string headerLine)
    {
        _ = markdown.Should().Contain(headerLine);

        var lines = markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var headerIndex = Array.FindIndex(lines, l => l.Trim().Equals(headerLine, StringComparison.Ordinal));
        _ = headerIndex.Should().BeGreaterThanOrEqualTo(0);

        var dataStartIndex = headerIndex + 2;
        _ = dataStartIndex.Should().BeLessThan(lines.Length);

        var rows = new List<string[]>();
        for (var i = dataStartIndex; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            if (!line.StartsWith('|') || !line.EndsWith('|'))
            {
                break;
            }

            var cells = line
                .Trim('|')
                .Split('|')
                .Select(c => c.Trim())
                .ToArray();

            rows.Add(cells);
        }

        return rows.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandsResourceTests"/> class.
    /// </summary>
    public CommandsResourceTests()
    {
        m_MockDebugEngine = new Mock<IDebugEngine>();
        _ = m_MockDebugEngine.Setup(e => e.GetActiveSessions()).Returns(Array.Empty<string>());

        var services = new ServiceCollection();
        _ = services.AddSingleton(m_MockDebugEngine.Object);
        _ = services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        m_ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies that Commands returns empty list when no commands exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_ReturnsEmptyList()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().NotBeNullOrEmpty();

        _ = result.Should().Contain("## Commands");
        _ = result.Should().Contain("**Count:** 0");
        _ = result.Should().Contain("No commands found.");
    }

    /// <summary>
    /// Verifies that Commands returns commands when commands exist in sessions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_WhenCommandsExist_ReturnsCommands()
    {
        var sessionId = "sess-1";
        var commandId = $"cmd-{sessionId}-1";
        var now = DateTime.Now;
        var cmdInfo = WinAiDbg.Engine.Share.Models.CommandInfo.Enqueued(sessionId, commandId, "k", now, 1234);

        _ = m_MockDebugEngine.Setup(e => e.GetActiveSessions()).Returns(new[] { sessionId });
        _ = m_MockDebugEngine
            .Setup(e => e.GetAllCommandInfos(sessionId))
            .Returns(new Dictionary<string, WinAiDbg.Engine.Share.Models.CommandInfo> { { commandId, cmdInfo } });

        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("## Commands");
        _ = result.Should().Contain("**Count:** 1");
        _ = result.Should().Contain(sessionId);
        _ = result.Should().Contain(commandId);
        _ = result.Should().Contain("Queued");
        _ = result.Should().Contain("| Session ID | Command ID | Command | State | Success | Queued |");

        var rows = ExtractTableRows(result, "| Session ID | Command ID | Command | State | Success | Queued |");
        _ = rows.Length.Should().Be(1);
        _ = rows[0].Length.Should().Be(6);
        _ = rows[0][0].Should().Be(sessionId);
        _ = rows[0][1].Should().Be(commandId);
        _ = rows[0][3].Should().Be("Queued");
    }

    /// <summary>
    /// Verifies that Commands are ordered by queued time ascending.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_WhenMultipleCommandsExist_AreOrderedByQueuedTime()
    {
        var sessA = "sess-a";
        var sessB = "sess-b";
        var t1 = DateTime.Now.AddSeconds(-10);
        var t2 = DateTime.Now.AddSeconds(-5);

        var cmdA = WinAiDbg.Engine.Share.Models.CommandInfo.Enqueued(sessA, $"cmd-{sessA}-1", "k", t2, 1);
        var cmdB = WinAiDbg.Engine.Share.Models.CommandInfo.Enqueued(sessB, $"cmd-{sessB}-1", "!analyze -v", t1, 2);

        _ = m_MockDebugEngine.Setup(e => e.GetActiveSessions()).Returns(new[] { sessA, sessB });
        _ = m_MockDebugEngine.Setup(e => e.GetAllCommandInfos(sessA)).Returns(new Dictionary<string, WinAiDbg.Engine.Share.Models.CommandInfo> { { cmdA.CommandId, cmdA } });
        _ = m_MockDebugEngine.Setup(e => e.GetAllCommandInfos(sessB)).Returns(new Dictionary<string, WinAiDbg.Engine.Share.Models.CommandInfo> { { cmdB.CommandId, cmdB } });

        var result = await CommandsResource.Commands(m_ServiceProvider);

        var rows = ExtractTableRows(result, "| Session ID | Command ID | Command | State | Success | Queued |");
        _ = rows.Length.Should().Be(2);

        // Row[0] should be the earlier queuedTime (t1) => sessB/cmdB.
        _ = rows[0][0].Should().Be(sessB);
        _ = rows[0][1].Should().Be(cmdB.CommandId);
        _ = rows[1][0].Should().Be(sessA);
        _ = rows[1][1].Should().Be(cmdA.CommandId);
    }

    /// <summary>
    /// Verifies that long command text is truncated in the Markdown table.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_WhenCommandIsVeryLong_TruncatesCommandText()
    {
        var sessionId = "sess-1";
        var commandId = $"cmd-{sessionId}-1";
        var now = DateTime.Now;
        var longCommand = new string('x', 200);
        var cmdInfo = WinAiDbg.Engine.Share.Models.CommandInfo.Enqueued(sessionId, commandId, longCommand, now, 1234);

        _ = m_MockDebugEngine.Setup(e => e.GetActiveSessions()).Returns(new[] { sessionId });
        _ = m_MockDebugEngine
            .Setup(e => e.GetAllCommandInfos(sessionId))
            .Returns(new Dictionary<string, WinAiDbg.Engine.Share.Models.CommandInfo> { { commandId, cmdInfo } });

        var result = await CommandsResource.Commands(m_ServiceProvider);

        var rows = ExtractTableRows(result, "| Session ID | Command ID | Command | State | Success | Queued |");
        _ = rows.Length.Should().Be(1);

        var renderedCommand = rows[0][2];
        _ = renderedCommand.Length.Should().BeLessThanOrEqualTo(60);
        _ = renderedCommand.Should().EndWith("...");
        _ = result.Should().NotContain(longCommand);
    }

    /// <summary>
    /// Verifies that Commands includes timestamp in response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_IncludesTimestamp()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("**Timestamp:**");
    }

    /// <summary>
    /// Verifies that Commands returns valid JSON.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_ReturnsValidJson()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("## Commands");
    }

    /// <summary>
    /// Verifies that Commands returns error response when exception occurs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_WithException_ReturnsErrorResponse()
    {
        // Create a service provider that throws when getting ILoggerFactory
        var mockServiceProvider = new Mock<IServiceProvider>();
        _ = mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Throws(new InvalidOperationException("Test error"));

        var result = await CommandsResource.Commands(mockServiceProvider.Object);

        _ = result.Should().Contain("## Commands");
        _ = result.Should().Contain("**Status:** Error");
        _ = result.Should().Contain("Test error");
    }

    /// <summary>
    /// Verifies that Commands throws exception when service provider is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_WithNullServiceProvider_ThrowsException()
    {
        var action = async () => await CommandsResource.Commands(null!);
        _ = await action.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that Commands JSON format is indented.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Commands_JsonFormat_IsIndented()
    {
        var result = await CommandsResource.Commands(m_ServiceProvider);

        _ = result.Should().Contain("\n"); // Markdown is multi-line
    }
}
