using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Nexus.Engine.Extensions.Security;
using Nexus.Engine.Share;
using Nexus.Engine.Share.Models;

using NLog;

namespace Nexus.Engine.Extensions.Callback;

/// <summary>
/// HTTP server for handling extension script callbacks.
/// </summary>
internal class CallbackServer
{
    private readonly Logger m_Logger;
    private readonly IDebugEngine m_Engine;
    private readonly TokenValidator m_TokenValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackServer"/> class.
    /// </summary>
    public CallbackServer(IDebugEngine engine, TokenValidator tokenValidator)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_TokenValidator = tokenValidator;
        m_Engine = engine;
    }

    /// <summary>
    /// Configures the callback server routes.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public void ConfigureRoutes(WebApplication app)
    {
        m_Logger.Info("Configuring extension callback server routes");

        // Execute command endpoint
        _ = app.MapPost("/extension-callback/execute", HandleExecuteAsync);

        // Queue command endpoint
        _ = app.MapPost("/extension-callback/queue", HandleQueueAsync);

        // Read command result endpoint
        _ = app.MapGet("/extension-callback/read/{commandId}", HandleReadAsync);

        // Get command status endpoint
        _ = app.MapGet("/extension-callback/status/{commandId}", HandleStatus);

        // Bulk status endpoint
        _ = app.MapPost("/extension-callback/status", HandleBulkStatusAsync);

        // Log endpoint
        _ = app.MapPost("/extension-callback/log", HandleLogAsync);

        m_Logger.Info("Extension callback server routes configured successfully");
    }

    /// <summary>
    /// Handles execute requests by validating the token, enqueuing a command, and awaiting completion.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>An HTTP result containing the command response or an error.</returns>
    private async Task<IResult> HandleExecuteAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var json = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ExecuteCommandRequest>(json);

            if (request == null)
            {
                return Results.BadRequest("Invalid request body");
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var (isValid, extensionSessionId, extensionCommandId) = m_TokenValidator.ValidateToken(token ?? "");

            if (!isValid || extensionSessionId == null || extensionCommandId == null)
            {
                return Results.Unauthorized();
            }

            var callbackCounter = context.Request.Headers["X-Callback-Counter"].FirstOrDefault() ?? "0";
            var extensionPid = context.Request.Headers["X-Extension-PID"].FirstOrDefault() ?? "0";
            var extensionName = context.Request.Headers["X-Script-Extension-Name"].FirstOrDefault() ?? string.Empty;

            m_Logger.Trace("[Extension] [{ExtensionPid}] [{CallbackCounter}] [{ExtensionCommandId}] [{extensionName}]: Execute request received",
                extensionPid, callbackCounter, extensionCommandId, extensionName);

            var newCommandId = m_Engine.EnqueueCommand(extensionSessionId, request.Command);
            var commandInfo = await m_Engine.GetCommandInfoAsync(extensionSessionId, newCommandId, CancellationToken.None);

            var response = new CommandResponse
            {
                Success = commandInfo.IsSuccess ?? false,
                Output = commandInfo.AggregatedOutput ?? string.Empty,
                CommandId = newCommandId,
                State = commandInfo.State.ToString(),
                Error = commandInfo.ErrorMessage
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error in execute command callback");
            return Results.Problem("Internal server error");
        }
    }

    /// <summary>
    /// Handles queue requests by validating the token and enqueuing a command without waiting.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>An HTTP result indicating the command was queued.</returns>
    private async Task<IResult> HandleQueueAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var json = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<QueueCommandRequest>(json);

            if (request == null)
            {
                return Results.BadRequest("Invalid request body");
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var (isValid, extensionSessionId, extensionCommandId) = m_TokenValidator.ValidateToken(token ?? "");

            if (!isValid || extensionSessionId == null || extensionCommandId == null)
            {
                return Results.Unauthorized();
            }

            var callbackCounter = context.Request.Headers["X-Callback-Counter"].FirstOrDefault() ?? "0";
            var extensionPid = context.Request.Headers["X-Extension-PID"].FirstOrDefault() ?? "0";
            var extensionName = context.Request.Headers["X-Script-Extension-Name"].FirstOrDefault() ?? string.Empty;

            m_Logger.Trace("[Extension] [{ExtensionPid}] [{CallbackCounter}] [{ExtensionCommandId}] [{extensionName}]: Enqueue request received",
                extensionPid, callbackCounter, extensionCommandId, extensionName);

            var newCommandId = m_Engine.EnqueueCommand(extensionSessionId, request.Command);

            var response = new CommandResponse
            {
                Success = true,
                Output = "Command queued successfully",
                CommandId = newCommandId,
                State = "Queued"
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error in queue command callback");
            return Results.Problem("Internal server error");
        }
    }

    /// <summary>
    /// Handles read requests by validating the token and awaiting command completion.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>An HTTP result containing the command response or an error.</returns>
    private async Task<IResult> HandleReadAsync(HttpContext context, string commandId)
    {
        try
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var (isValid, extensionSessionId, extensionCommandId) = m_TokenValidator.ValidateToken(token ?? "");

            if (!isValid || extensionSessionId == null || extensionCommandId == null)
            {
                return Results.Unauthorized();
            }

            var callbackCounter = context.Request.Headers["X-Callback-Counter"].FirstOrDefault() ?? "0";
            var extensionPid = context.Request.Headers["X-Extension-PID"].FirstOrDefault() ?? "0";
            var extensionName = context.Request.Headers["X-Script-Extension-Name"].FirstOrDefault() ?? string.Empty;

            m_Logger.Trace("[Extension] [{ExtensionPid}] [{CallbackCounter}] [{ExtensionCommandId}] [{extensionName}]: Read request received for command {CommandId}",
                extensionPid, callbackCounter, extensionCommandId, extensionName, commandId);

            var commandInfo = await m_Engine.GetCommandInfoAsync(extensionSessionId, commandId, CancellationToken.None);

            var response = new CommandResponse
            {
                Success = commandInfo.IsSuccess ?? false,
                Output = commandInfo.AggregatedOutput ?? string.Empty,
                CommandId = commandId,
                State = commandInfo.State.ToString(),
                Error = commandInfo.ErrorMessage
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error in read command result callback");
            return Results.Problem("Internal server error");
        }
    }

    /// <summary>
    /// Handles status requests by validating the token and returning the current command status without waiting.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>An HTTP result containing the command status or an error.</returns>
    private IResult HandleStatus(HttpContext context, string commandId)
    {
        try
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var (isValid, extensionSessionId, extensionCommandId) = m_TokenValidator.ValidateToken(token ?? "");

            if (!isValid || extensionSessionId == null || extensionCommandId == null)
            {
                return Results.Unauthorized();
            }

            var callbackCounter = context.Request.Headers["X-Callback-Counter"].FirstOrDefault() ?? "0";
            var extensionPid = context.Request.Headers["X-Extension-PID"].FirstOrDefault() ?? "0";
            var extensionName = context.Request.Headers["X-Script-Extension-Name"].FirstOrDefault() ?? string.Empty;

            m_Logger.Trace("[Extension] [{ExtensionPid}] [{CallbackCounter}] [{ExtensionCommandId}] [{extensionName}]: Status request received for command {CommandId}",
                extensionPid, callbackCounter, extensionCommandId, extensionName, commandId);

            var commandInfo = m_Engine.GetCommandInfo(extensionSessionId, commandId);

            if (commandInfo == null)
            {
                return Results.NotFound(new { error = "Command not found" });
            }

            var response = new CommandResponse
            {
                Success = commandInfo.IsSuccess ?? true,
                Output = commandInfo.AggregatedOutput ?? string.Empty,
                CommandId = commandId,
                State = commandInfo.State.ToString(),
                Error = commandInfo.ErrorMessage
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error in get command status callback");
            return Results.Problem("Internal server error");
        }
    }

    /// <summary>
    /// Handles bulk status requests by validating the token and returning statuses for multiple commands.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>An HTTP result containing a map of command IDs to their statuses.</returns>
    private async Task<IResult> HandleBulkStatusAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var json = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<BulkStatusRequest>(json);

            if (request == null || request.CommandIds == null || request.CommandIds.Count == 0)
            {
                return Results.BadRequest("Invalid request body or empty command IDs");
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var (isValid, extensionSessionId, extensionCommandId) = m_TokenValidator.ValidateToken(token ?? "");

            if (!isValid || extensionSessionId == null || extensionCommandId == null)
            {
                return Results.Unauthorized();
            }

            var callbackCounter = context.Request.Headers["X-Callback-Counter"].FirstOrDefault() ?? "0";
            var extensionPid = context.Request.Headers["X-Extension-PID"].FirstOrDefault() ?? "0";
            var extensionName = context.Request.Headers["X-Script-Extension-Name"].FirstOrDefault() ?? string.Empty;

            m_Logger.Trace("[Extension] [{ExtensionPid}] [{CallbackCounter}] [{ExtensionCommandId}] [{extensionName}]: Status request received",
                extensionPid, callbackCounter, extensionCommandId, extensionName);

            var results = new Dictionary<string, object>();

            foreach (var commandId in request.CommandIds)
            {
                var commandInfo = m_Engine.GetCommandInfo(extensionSessionId, commandId);

                if (commandInfo != null)
                {
                    var isCompleted = commandInfo.State is CommandState.Completed or
                                    CommandState.Failed or
                                    CommandState.Cancelled;

                    var status = commandInfo.State.ToString();
                    if (commandInfo.IsSuccess == true && commandInfo.State == CommandState.Completed)
                    {
                        status = "Success";
                    }
                    else if (commandInfo.IsSuccess == false && commandInfo.State == CommandState.Completed)
                    {
                        status = "Failed";
                    }

                    results[commandId] = new
                    {
                        commandId,
                        command = commandInfo.Command,
                        state = status,
                        isCompleted,
                        output = commandInfo.AggregatedOutput ?? string.Empty,
                        error = commandInfo.ErrorMessage,
                        queuedTime = commandInfo.QueuedTime,
                        startTime = commandInfo.StartTime,
                        endTime = commandInfo.EndTime
                    };
                }
            }

            var response = new { results };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error in bulk status callback");
            return Results.Problem("Internal server error");
        }
    }

    /// <summary>
    /// Handles extension log requests by validating the token and logging with the requested level.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>An HTTP result indicating success or error.</returns>
    private async Task<IResult> HandleLogAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var json = await reader.ReadToEndAsync();
            var request = JsonSerializer.Deserialize<LogRequest>(json);

            if (request == null)
            {
                return Results.BadRequest("Invalid request body");
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var (isValid, extensionSessionId, extensionCommandId) = m_TokenValidator.ValidateToken(token ?? "");

            if (!isValid || extensionSessionId == null || extensionCommandId == null)
            {
                return Results.Unauthorized();
            }

            var callbackCounter = context.Request.Headers["X-Callback-Counter"].FirstOrDefault() ?? "0";
            var extensionPid = context.Request.Headers["X-Extension-PID"].FirstOrDefault() ?? "0";
            var extensionName = context.Request.Headers["X-Script-Extension-Name"].FirstOrDefault() ?? string.Empty;

            var commandNumber = GetCommandNumber(extensionSessionId, extensionCommandId);

            LogMessageWithLevel(m_Logger, request.Level, "[EXT] [{ExtensionPid}] [{CallbackCounter}] [{ExtensionCommandId}] [{extensionName}]: {Message}",
                extensionPid, callbackCounter, commandNumber, extensionName, request.Message);

            return Results.Ok();
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error in log callback");
            return Results.Problem("Internal server error");
        }
    }

    /// <summary>
    /// Logs a message with the specified level.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="level">The log level (case-insensitive).</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="args">The message arguments.</param>
    private static void LogMessageWithLevel(Logger logger, string level, string messageTemplate, params object[] args)
    {
        var normalizedLevel = level.ToUpperInvariant();
        switch (normalizedLevel)
        {
            case "TRACE":
                logger.Trace(messageTemplate, args);
                break;
            case "DEBUG":
                logger.Debug(messageTemplate, args);
                break;
            case "INFORMATION":
                logger.Info(messageTemplate, args);
                break;
            case "WARNING":
                logger.Warn(messageTemplate, args);
                break;
            case "ERROR":
                logger.Error(messageTemplate, args);
                break;
            case "FATAL":
                logger.Fatal(messageTemplate, args);
                break;
            default:
                // Default to Info for unknown levels
                logger.Fatal("Invalid log level: " + level + " - " + messageTemplate, args);
                break;
        }
    }

    /// <summary>
    /// Gets the command number from the command identifier.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>The command number.</returns>
    private static int GetCommandNumber(string sessionId, string commandId)
    {
        var prefix = $"cmd-{sessionId}-";
        var indexString = commandId.Replace(prefix, "");

        return int.TryParse(indexString, out var index) ? index : 0;
    }
}
