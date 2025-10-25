using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Nexus.Engine.Extensions.Security;
using Nexus.Engine.Share;

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
    public CallbackServer(IDebugEngine engine)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_TokenValidator = new TokenValidator();
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
        _ = app.MapPost("/extension-callback/execute", async (HttpContext context) =>
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
                var (isValid, sessionId, _) = m_TokenValidator.ValidateToken(token ?? "");

                if (!isValid || sessionId == null)
                {
                    return Results.Unauthorized();
                }

                // Enqueue the command and wait for completion
                var newCommandId = m_Engine.EnqueueCommand(sessionId, request.Command);
                var commandInfo = await m_Engine.GetCommandInfoAsync(sessionId, newCommandId, CancellationToken.None);

                var response = new CommandResponse
                {
                    Success = commandInfo.IsSuccess ?? false,
                    Output = commandInfo.Output ?? string.Empty,
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
        });

        // Queue command endpoint
        _ = app.MapPost("/extension-callback/queue", async (HttpContext context) =>
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
                var (isValid, sessionId, _) = m_TokenValidator.ValidateToken(token ?? "");

                if (!isValid || sessionId == null)
                {
                    return Results.Unauthorized();
                }

                // Enqueue the command without waiting
                var newCommandId = m_Engine.EnqueueCommand(sessionId, request.Command);

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
        });

        // Read command result endpoint
        _ = app.MapGet("/extension-callback/read/{commandId}", async (HttpContext context, string commandId) =>
        {
            try
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                var (isValid, sessionId, _) = m_TokenValidator.ValidateToken(token ?? "");

                if (!isValid || sessionId == null)
                {
                    return Results.Unauthorized();
                }

                // Get command info and wait for completion
                var commandInfo = await m_Engine.GetCommandInfoAsync(sessionId, commandId, CancellationToken.None);

                var response = new CommandResponse
                {
                    Success = commandInfo.IsSuccess ?? false,
                    Output = commandInfo.Output ?? string.Empty,
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
        });

        // Get command status endpoint
        _ = app.MapGet("/extension-callback/status/{commandId}", (HttpContext context, string commandId) =>
        {
            try
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                var (isValid, sessionId, _) = m_TokenValidator.ValidateToken(token ?? "");

                if (!isValid || sessionId == null)
                {
                    return Results.Unauthorized();
                }

                // Get command info without waiting
                var commandInfo = m_Engine.GetCommandInfo(sessionId, commandId);

                if (commandInfo == null)
                {
                    return Results.NotFound(new { error = "Command not found" });
                }

                var response = new CommandResponse
                {
                    Success = commandInfo.IsSuccess ?? true,
                    Output = commandInfo.Output ?? string.Empty,
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
        });

        // Log endpoint
        _ = app.MapPost("/extension-callback/log", async (HttpContext context) =>
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
                var (isValid, sessionId, commandId) = m_TokenValidator.ValidateToken(token ?? "");

                if (!isValid)
                {
                    return Results.Unauthorized();
                }

                // Log the message
                m_Logger.Info("Extension log from session {SessionId}, command {CommandId}: {Message}",
                    sessionId, commandId, request.Message);

                return Results.Ok();
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Error in log callback");
                return Results.Problem("Internal server error");
            }
        });

        m_Logger.Info("Extension callback server routes configured successfully");
    }
}
