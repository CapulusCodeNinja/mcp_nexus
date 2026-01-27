using System.Text;
using System.Text.Json;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using NLog;

using WinAiDbg.Protocol.Tools;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Services;

/// <summary>
/// Service that provides custom MCP <c>tools/list</c> and <c>tools/call</c> handling.
/// This avoids SDK reflection binder errors being converted into generic messages, and instead
/// returns actionable tool-execution errors (via <see cref="CallToolResult.IsError"/>) that LLMs can self-correct from.
/// </summary>
internal class McpToolCallService
{
    private readonly Logger m_Logger;
    private readonly IMcpToolDefinitionService m_ToolDefinitionService;

    private readonly CancelCommandTool m_CancelCommandTool = new();
    private readonly CloseDumpAnalyzeSessionTool m_CloseDumpAnalyzeSessionTool = new();
    private readonly EnqueueAsyncDumpAnalyzeCommandTool m_EnqueueAsyncDumpAnalyzeCommandTool = new();
    private readonly EnqueueAsyncExtensionCommandTool m_EnqueueAsyncExtensionCommandTool = new();
    private readonly GetDumpAnalyzeCommandsStatusTool m_GetDumpAnalyzeCommandsStatusTool = new();
    private readonly OpenDumpAnalyzeSessionTool m_OpenDumpAnalyzeSessionTool = new();
    private readonly ReadDumpAnalyzeCommandResultTool m_ReadDumpAnalyzeCommandResultTool = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolCallService"/> class.
    /// </summary>
    /// <param name="toolDefinitionService">Tool schema provider for tool listing and argument validation.</param>
    public McpToolCallService(IMcpToolDefinitionService toolDefinitionService)
    {
        ArgumentNullException.ThrowIfNull(toolDefinitionService);

        m_Logger = LogManager.GetCurrentClassLogger();
        m_ToolDefinitionService = toolDefinitionService;
    }

    /// <summary>
    /// Lists tools using the server's tool schema definitions.
    /// </summary>
    /// <returns>A <see cref="ListToolsResult"/> containing all tools and their input schemas.</returns>
    public ListToolsResult ListTools()
    {
        var result = new ListToolsResult();
        foreach (var schema in m_ToolDefinitionService.GetAllTools())
        {
            result.Tools.Add(new Tool
            {
                Name = schema.Name,
                Description = schema.Description,
                InputSchema = JsonSerializer.SerializeToElement(schema.InputSchema),
            });
        }

        return result;
    }

    /// <summary>
    /// Handles tool invocation for <c>tools/call</c>.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="CallToolResult"/> containing either the tool output or an actionable error.</returns>
    public async ValueTask<CallToolResult> CallToolAsync(
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Params);

        return await CallToolAsync(context.Params.Name, context.Params.Arguments, cancellationToken);
    }

    /// <summary>
    /// Handles tool invocation for <c>tools/call</c>.
    /// This overload exists to enable unit testing without constructing a full <see cref="RequestContext{TParams}"/>.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <param name="arguments">The tool arguments from <c>params.arguments</c>.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="CallToolResult"/> containing either the tool output or an actionable error.</returns>
    public async ValueTask<CallToolResult> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var schema = m_ToolDefinitionService.GetTool(toolName);

        if (schema is null)
        {
            var defaultSchema = JsonSerializer.SerializeToElement(new { type = "object" });
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = CreateActionableToolErrorMarkdown(
                            toolName,
                            "Unknown tool name.",
                            defaultSchema),
                    },
                ],
            };
        }

        var schemaElement = JsonSerializer.SerializeToElement(schema.InputSchema);
        var validationError = ValidateArguments(schemaElement, arguments);

        if (!string.IsNullOrEmpty(validationError))
        {
            return CreateErrorResult(
                toolName,
                validationError,
                schemaElement);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var output = await InvokeKnownToolAsync(toolName, arguments ?? new Dictionary<string, JsonElement>(), cancellationToken);
            return new CallToolResult
            {
                IsError = false,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = output,
                    },
                ],
            };
        }
        catch (McpToolUserInputException ex)
        {
            return CreateErrorResult(
                toolName,
                ex.Message,
                schemaElement);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Unhandled exception invoking tool {ToolName}", toolName);
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = CreateActionableToolErrorMarkdown(
                            toolName,
                            $"Unexpected error: {ex.Message}",
                            schemaElement),
                    },
                ],
            };
        }
    }

    /// <summary>
    /// Invokes one of the known WinAiDbg tools by name.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <param name="arguments">The argument dictionary from the client.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tool output as text.</returns>
    private async Task<string> InvokeKnownToolAsync(
        string toolName,
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (toolName)
        {
            case "winaidbg_open_dump_analyze_session":
            case "nexus_open_dump_analyze_session":
                {
                    var dumpPath = GetRequiredString(arguments, "dumpPath");
                    var result = await m_OpenDumpAnalyzeSessionTool.Execute(dumpPath);
                    return result.ToString() ?? string.Empty;
                }

            case "winaidbg_enqueue_async_dump_analyze_command":
            case "nexus_enqueue_async_dump_analyze_command":
                {
                    var sessionId = GetRequiredString(arguments, "sessionId");
                    var command = GetRequiredString(arguments, "command");
                    var result = await m_EnqueueAsyncDumpAnalyzeCommandTool.Execute(sessionId, command);
                    return result.ToString() ?? string.Empty;
                }

            case "winaidbg_enqueue_async_extension_command":
            case "nexus_enqueue_async_extension_command":
                {
                    var sessionId = GetRequiredString(arguments, "sessionId");
                    var extensionName = GetRequiredString(arguments, "extensionName");
                    var parameters = GetOptionalObject(arguments, "parameters");
                    var result = await m_EnqueueAsyncExtensionCommandTool.Execute(sessionId, extensionName, parameters);
                    return result.ToString() ?? string.Empty;
                }

            case "winaidbg_read_dump_analyze_command_result":
            case "nexus_read_dump_analyze_command_result":
                {
                    var sessionId = GetRequiredString(arguments, "sessionId");
                    var commandId = GetRequiredString(arguments, "commandId");
                    var maxWaitSeconds = GetRequiredInt(arguments, "maxWaitSeconds");
                    var result = await m_ReadDumpAnalyzeCommandResultTool.Execute(sessionId, commandId, maxWaitSeconds);
                    return result.ToString() ?? string.Empty;
                }

            case "winaidbg_get_dump_analyze_commands_status":
            case "nexus_get_dump_analyze_commands_status":
                {
                    var sessionId = GetRequiredString(arguments, "sessionId");
                    var result = await m_GetDumpAnalyzeCommandsStatusTool.Execute(sessionId);
                    return result.ToString() ?? string.Empty;
                }

            case "winaidbg_close_dump_analyze_session":
            case "nexus_close_dump_analyze_session":
                {
                    var sessionId = GetRequiredString(arguments, "sessionId");
                    var result = await m_CloseDumpAnalyzeSessionTool.Execute(sessionId);
                    return result.ToString() ?? string.Empty;
                }

            case "winaidbg_cancel_dump_analyze_command":
            case "nexus_cancel_dump_analyze_command":
                {
                    var sessionId = GetRequiredString(arguments, "sessionId");
                    var commandId = GetRequiredString(arguments, "commandId");
                    var result = await m_CancelCommandTool.Execute(sessionId, commandId);
                    return result.ToString() ?? string.Empty;
                }

            default:
                throw new ArgumentException($"Unknown tool: '{toolName}'.", nameof(toolName));
        }
    }

    /// <summary>
    /// Validates arguments against the provided tool input schema.
    /// </summary>
    /// <param name="inputSchema">Tool input schema.</param>
    /// <param name="arguments">Arguments dictionary passed by the client.</param>
    /// <returns>
    /// Empty string if valid; otherwise a human-readable description of what is wrong.
    /// </returns>
    private static string ValidateArguments(JsonElement inputSchema, IReadOnlyDictionary<string, JsonElement>? arguments)
    {
        var safeArguments = arguments ?? new Dictionary<string, JsonElement>();

        if (inputSchema.ValueKind != JsonValueKind.Object)
        {
            return "Tool schema is invalid (expected an object schema).";
        }

        var required = new List<string>();
        if (inputSchema.TryGetProperty("required", out var requiredElement) && requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in requiredElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    required.Add(item.GetString() ?? string.Empty);
                }
            }
        }

        var missing = required.Where(r => !string.IsNullOrWhiteSpace(r) && !safeArguments.ContainsKey(r)).ToArray();
        if (missing.Length > 0)
        {
            return $"Missing required parameter(s): {string.Join(", ", missing.Select(m => $"`{m}`"))}.";
        }

        if (!inputSchema.TryGetProperty("properties", out var propertiesElement) || propertiesElement.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        // Type/value validation for schema-typed properties. (We keep this conservative; validate only when schema declares a concrete type.)
        foreach (var kvp in safeArguments)
        {
            if (!propertiesElement.TryGetProperty(kvp.Key, out var propSchema))
            {
                continue;
            }

            if (propSchema.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!propSchema.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var expectedType = typeElement.GetString();
            if (expectedType == "string" && kvp.Value.ValueKind != JsonValueKind.String)
            {
                return $"Invalid type for parameter `{kvp.Key}`: expected `string`, got `{kvp.Value.ValueKind}`.";
            }

            if (expectedType == "integer" && (kvp.Value.ValueKind != JsonValueKind.Number || !kvp.Value.TryGetInt32(out _)))
            {
                return $"Invalid type for parameter `{kvp.Key}`: expected `integer`, got `{kvp.Value.ValueKind}`.";
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Creates a tool-execution error result with an actionable message.
    /// </summary>
    /// <param name="toolName">Tool name.</param>
    /// <param name="details">Error details.</param>
    /// <param name="inputSchema">Tool input schema.</param>
    /// <returns>A <see cref="CallToolResult"/> with <see cref="CallToolResult.IsError"/> set to true.</returns>
    private static CallToolResult CreateErrorResult(string toolName, string details, JsonElement inputSchema)
    {
        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = CreateActionableToolErrorMarkdown(toolName, details, inputSchema),
                },
            ],
        };
    }

    /// <summary>
    /// Creates an actionable Markdown error message suitable for LLM self-correction.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <param name="details">Error details.</param>
    /// <param name="inputSchema">Tool input schema.</param>
    /// <returns>Markdown text.</returns>
    private static string CreateActionableToolErrorMarkdown(string toolName, string details, JsonElement inputSchema)
    {
        var requiredParameters = GetRequiredParameterNames(inputSchema);
        var optionalParameters = GetOptionalParameterNames(inputSchema);
        var sb = new StringBuilder();
        _ = sb.AppendLine("## Tool Invocation Error");

        _ = sb.AppendLine();
        _ = sb.AppendLine($"**Tool:** `{toolName}`");

        _ = sb.AppendLine("**Issue:**");
        _ = sb.AppendLine(details);

        if (requiredParameters.Count > 0)
        {
            _ = sb.AppendLine();
            _ = sb.AppendLine("### Mandatory `params.arguments` fields");
            _ = sb.AppendLine();
            foreach (var p in requiredParameters.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                _ = sb.AppendLine($"- `{p}`");
            }
        }

        if (optionalParameters.Count > 0)
        {
            _ = sb.AppendLine();
            _ = sb.AppendLine("### Optional `params.arguments` fields");
            _ = sb.AppendLine();
            foreach (var p in optionalParameters)
            {
                _ = sb.AppendLine($"- `{p}`");
            }
        }

        _ = sb.AppendLine(MarkdownFormatter.GetUsageGuideMarkdown());

        return sb.ToString();
    }

    /// <summary>
    /// Gets required parameter names from the input schema.
    /// </summary>
    /// <param name="inputSchema">Tool input schema.</param>
    /// <returns>Set of required parameter names.</returns>
    private static HashSet<string> GetRequiredParameterNames(JsonElement inputSchema)
    {
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (inputSchema.ValueKind != JsonValueKind.Object ||
            !inputSchema.TryGetProperty("required", out var requiredElement) ||
            requiredElement.ValueKind != JsonValueKind.Array)
        {
            return required;
        }

        foreach (var item in requiredElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var name = item.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _ = required.Add(name);
                }
            }
        }

        return required;
    }

    /// <summary>
    /// Gets optional parameter names from the input schema.
    /// </summary>
    /// <param name="inputSchema">Tool input schema.</param>
    /// <returns>List of optional parameter names, sorted.</returns>
    private static List<string> GetOptionalParameterNames(JsonElement inputSchema)
    {
        var required = GetRequiredParameterNames(inputSchema);
        var optional = new List<string>();

        if (inputSchema.ValueKind != JsonValueKind.Object ||
            !inputSchema.TryGetProperty("properties", out var propertiesElement) ||
            propertiesElement.ValueKind != JsonValueKind.Object)
        {
            return optional;
        }

        foreach (var prop in propertiesElement.EnumerateObject())
        {
            if (!required.Contains(prop.Name))
            {
                optional.Add(prop.Name);
            }
        }

        optional.Sort(StringComparer.OrdinalIgnoreCase);
        return optional;
    }

    /// <summary>
    /// Reads a required string argument.
    /// </summary>
    /// <param name="arguments">Arguments dictionary.</param>
    /// <param name="name">Argument name.</param>
    /// <returns>The argument value.</returns>
    private static string GetRequiredString(IReadOnlyDictionary<string, JsonElement> arguments, string name)
    {
        return arguments.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : throw new ArgumentException($"Missing required string argument '{name}'.", nameof(arguments));
    }

    /// <summary>
    /// Reads a required integer argument.
    /// </summary>
    /// <param name="arguments">Arguments dictionary.</param>
    /// <param name="name">Argument name.</param>
    /// <returns>The integer argument value.</returns>
    private static int GetRequiredInt(IReadOnlyDictionary<string, JsonElement> arguments, string name)
    {
        return arguments.TryGetValue(name, out var value) &&
               value.ValueKind == JsonValueKind.Number &&
               value.TryGetInt32(out var number)
            ? number
            : throw new ArgumentException($"Missing required integer argument '{name}'.", nameof(arguments));
    }

    /// <summary>
    /// Reads an optional object argument, deserializing it into a loosely-typed object for tool consumption.
    /// </summary>
    /// <param name="arguments">Arguments dictionary.</param>
    /// <param name="name">Argument name.</param>
    /// <returns>The deserialized object, or null if not provided.</returns>
    private static object? GetOptionalObject(IReadOnlyDictionary<string, JsonElement> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<object>(value.GetRawText());
        }
        catch
        {
            return value.ToString();
        }
    }
}

