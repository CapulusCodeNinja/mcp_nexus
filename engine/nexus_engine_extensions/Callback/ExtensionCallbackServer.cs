using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Nexus.Engine.Extensions.Security;

using NLog;

namespace Nexus.Engine.Extensions.Callback;

/// <summary>
/// HTTP server for handling extension script callbacks.
/// </summary>
public class ExtensionCallbackServer
{
    private readonly Logger m_Logger;
    private readonly ExtensionTokenValidator m_TokenValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionCallbackServer"/> class.
    /// </summary>
    public ExtensionCallbackServer()
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_TokenValidator = new ExtensionTokenValidator();
    }

    /// <summary>
    /// Configures the callback server routes.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public void ConfigureRoutes(WebApplication app)
    {
        m_Logger.Info("Configuring extension callback server routes");

        // Execute command endpoint
        _ = app.MapPost("/extension-callback/execute", (HttpContext context) =>
        {
            try
            {
                using var reader = new StreamReader(context.Request.Body);
                var json = reader.ReadToEnd();
                var request = JsonSerializer.Deserialize<ExecuteCommandRequest>(json);

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

                // For now, return a placeholder response
                // In a real implementation, this would call the debug engine
                var response = new CommandResponse
                {
                    Success = true,
                    Output = "Command executed successfully (placeholder)",
                    CommandId = commandId ?? "unknown"
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
        _ = app.MapPost("/extension-callback/queue", (HttpContext context) =>
        {
            try
            {
                using var reader = new StreamReader(context.Request.Body);
                var json = reader.ReadToEnd();
                var request = JsonSerializer.Deserialize<QueueCommandRequest>(json);

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

                // For now, return a placeholder response
                var response = new CommandResponse
                {
                    Success = true,
                    Output = "Command queued successfully (placeholder)",
                    CommandId = commandId ?? "unknown"
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
        _ = app.MapGet("/extension-callback/read/{commandId}", (HttpContext context, string commandId) =>
        {
            try
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                var (isValid, sessionId, _) = m_TokenValidator.ValidateToken(token ?? "");

                if (!isValid)
                {
                    return Results.Unauthorized();
                }

                // For now, return a placeholder response
                var response = new CommandResponse
                {
                    Success = true,
                    Output = $"Result for command {commandId} (placeholder)",
                    CommandId = commandId
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

                if (!isValid)
                {
                    return Results.Unauthorized();
                }

                // For now, return a placeholder response
                var response = new CommandResponse
                {
                    Success = true,
                    Output = $"Status for command {commandId} (placeholder)",
                    CommandId = commandId
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
        _ = app.MapPost("/extension-callback/log", (HttpContext context) =>
        {
            try
            {
                using var reader = new StreamReader(context.Request.Body);
                var json = reader.ReadToEnd();
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
