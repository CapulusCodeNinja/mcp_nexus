using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mcp_nexus.Session;
using mcp_nexus.CommandQueue;
using System.Net;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Controller for handling extension callback requests.
    /// Provides HTTP REST API endpoints for extensions to execute and read commands.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ExtensionCallbackController"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tokenValidator">The token validator for authentication.</param>
    /// <param name="sessionManager">The session manager for accessing sessions.</param>
    /// <param name="extensionTracker">The extension command tracker.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    [ApiController]
    [Route("extension-callback")]
    public class ExtensionCallbackController(
        ILogger<ExtensionCallbackController> logger,
        IExtensionTokenValidator tokenValidator,
        ISessionManager sessionManager,
        IExtensionCommandTracker extensionTracker) : ControllerBase
    {
        private readonly ILogger<ExtensionCallbackController> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IExtensionTokenValidator m_TokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        private readonly ISessionManager m_SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        private readonly IExtensionCommandTracker m_ExtensionTracker = extensionTracker ?? throw new ArgumentNullException(nameof(extensionTracker));

        /// <summary>
        /// Executes a WinDBG command via extension callback.
        /// Extensions use this endpoint to queue commands in the session's command queue.
        /// </summary>
        /// <param name="request">The command execution request.</param>
        /// <returns>The command execution result.</returns>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(ExtensionCallbackExecuteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExecuteCommand([FromBody] ExtensionCallbackExecuteRequest request)
        {
            // Validate request is from localhost
            if (!IsLocalhost())
            {
                m_Logger.LogWarning("Extension callback denied from non-localhost IP: {IP}",
                    HttpContext.Connection.RemoteIpAddress);
                return StatusCode(403, new ExtensionCallbackErrorResponse
                {
                    Error = "Forbidden",
                    Message = "Extension callbacks are only accessible from localhost",
                    Hint = "This endpoint is for internal extension use only"
                });
            }

            // Extract and validate token
            var token = ExtractBearerToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                m_Logger.LogWarning("Extension callback missing authorization token");
                return StatusCode(401, new ExtensionCallbackErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "Missing or invalid authorization token"
                });
            }

            var (isValid, sessionId, commandId) = m_TokenValidator.ValidateToken(token);
            if (!isValid || string.IsNullOrWhiteSpace(sessionId))
            {
                m_Logger.LogWarning("Extension callback invalid token");
                return StatusCode(401, new ExtensionCallbackErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "Invalid extension token"
                });
            }

            // Validate request
            if (request == null || string.IsNullOrWhiteSpace(request.Command))
            {
                return BadRequest(new ExtensionCallbackErrorResponse
                {
                    Error = "Bad Request",
                    Message = "Command cannot be null or empty"
                });
            }

            try
            {
                m_Logger.LogDebug("Extension callback execute command: {Command} for session {SessionId}",
                    request.Command, sessionId);

                // Increment callback count
                if (!string.IsNullOrWhiteSpace(commandId))
                {
                    m_ExtensionTracker.IncrementCallbackCount(commandId);
                }

                // Get command queue for the session
                if (!m_SessionManager.TryGetCommandQueue(sessionId, out var commandQueue) || commandQueue == null)
                {
                    return StatusCode(400, new ExtensionCallbackErrorResponse
                    {
                        Error = "Bad Request",
                        Message = $"Session {sessionId} not found or command queue not available"
                    });
                }

                // Queue the command
                var queuedCommandId = commandQueue.QueueCommand(request.Command);

                // Wait for command completion
                var timeout = request.TimeoutSeconds > 0
                    ? TimeSpan.FromSeconds(request.TimeoutSeconds)
                    : TimeSpan.FromMinutes(5);

                var result = await WaitForCommandCompletionAsync(sessionId, queuedCommandId, timeout);

                return Ok(new ExtensionCallbackExecuteResponse
                {
                    CommandId = queuedCommandId,
                    Status = result != null && result.IsSuccess ? "Success" : "Failed",
                    Output = result?.Output,
                    Error = result?.ErrorMessage
                });
            }
            catch (TimeoutException ex)
            {
                m_Logger.LogWarning(ex, "Extension callback command timed out for session {SessionId}", sessionId);
                return StatusCode(504, new ExtensionCallbackErrorResponse
                {
                    Error = "Timeout",
                    Message = "Command execution timed out"
                });
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Extension callback execute failed for session {SessionId}", sessionId);
                return StatusCode(500, new ExtensionCallbackErrorResponse
                {
                    Error = "Internal Server Error",
                    Message = $"Failed to execute command: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Reads the result of a previously executed command.
        /// Extensions use this endpoint to check command status and retrieve results.
        /// </summary>
        /// <param name="request">The command read request.</param>
        /// <returns>The command result.</returns>
        [HttpPost("read")]
        [ProducesResponseType(typeof(ExtensionCallbackReadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReadCommandResult([FromBody] ExtensionCallbackReadRequest request)
        {
            // Validate request is from localhost
            if (!IsLocalhost())
            {
                m_Logger.LogWarning("Extension callback denied from non-localhost IP: {IP}",
                    HttpContext.Connection.RemoteIpAddress);
                return StatusCode(403, new ExtensionCallbackErrorResponse
                {
                    Error = "Forbidden",
                    Message = "Extension callbacks are only accessible from localhost"
                });
            }

            // Extract and validate token
            var token = ExtractBearerToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(401, new ExtensionCallbackErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "Missing or invalid authorization token"
                });
            }

            var (isValid, sessionId, _) = m_TokenValidator.ValidateToken(token);
            if (!isValid || string.IsNullOrWhiteSpace(sessionId))
            {
                return StatusCode(401, new ExtensionCallbackErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "Invalid extension token"
                });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.CommandId))
            {
                return BadRequest(new ExtensionCallbackErrorResponse
                {
                    Error = "Bad Request",
                    Message = "CommandId cannot be null or empty"
                });
            }

            try
            {
                // Get command info and result
                var (commandInfo, commandResult) = await m_SessionManager.GetCommandInfoAndResultAsync(
                    sessionId, request.CommandId);

                if (commandInfo == null)
                {
                    return NotFound(new ExtensionCallbackErrorResponse
                    {
                        Error = "Not Found",
                        Message = $"Command {request.CommandId} not found"
                    });
                }

                return Ok(new ExtensionCallbackReadResponse
                {
                    CommandId = request.CommandId,
                    Status = commandInfo.State.ToString(),
                    IsCompleted = commandInfo.IsCompleted,
                    Output = commandResult?.Output,
                    Error = commandResult?.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Extension callback read failed for session {SessionId}, command {CommandId}",
                    sessionId, request.CommandId);
                return StatusCode(500, new ExtensionCallbackErrorResponse
                {
                    Error = "Internal Server Error",
                    Message = $"Failed to read command result: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Waits for a command to complete with timeout.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="commandId">The command ID.</param>
        /// <param name="timeout">The maximum time to wait.</param>
        /// <returns>The command result.</returns>
        /// <exception cref="TimeoutException">Thrown when the command times out.</exception>
        private async Task<ICommandResult> WaitForCommandCompletionAsync(
            string sessionId,
            string commandId,
            TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            var pollInterval = TimeSpan.FromMilliseconds(100);

            while (DateTime.UtcNow - startTime < timeout)
            {
                var (commandInfo, commandResult) = await m_SessionManager.GetCommandInfoAndResultAsync(
                    sessionId, commandId);

                if (commandInfo != null && commandInfo.IsCompleted && commandResult != null)
                {
                    return commandResult;
                }

                await Task.Delay(pollInterval);
            }

            throw new TimeoutException($"Command {commandId} did not complete within {timeout.TotalSeconds} seconds");
        }

        /// <summary>
        /// Writes a log message from an extension script to the server log.
        /// Extensions use this endpoint to log diagnostic information during execution.
        /// </summary>
        /// <param name="request">The log request.</param>
        /// <returns>HTTP 200 OK on success, error code otherwise.</returns>
        [HttpPost("log")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ExtensionCallbackErrorResponse), StatusCodes.Status400BadRequest)]
        public IActionResult WriteLog([FromBody] ExtensionCallbackLogRequest request)
        {
            // Validate request is from localhost
            if (!IsLocalhost())
            {
                m_Logger.LogWarning("Extension log callback denied from non-localhost IP: {IP}",
                    HttpContext.Connection.RemoteIpAddress);
                return StatusCode(403, new ExtensionCallbackErrorResponse
                {
                    Error = "Forbidden",
                    Message = "Extension callbacks must originate from localhost"
                });
            }

            // Validate token
            var token = ExtractBearerToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return StatusCode(401, new ExtensionCallbackErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "Missing or invalid authorization token"
                });
            }

            var (isValid, sessionId, commandId) = m_TokenValidator.ValidateToken(token);
            if (!isValid || string.IsNullOrWhiteSpace(sessionId))
            {
                return StatusCode(401, new ExtensionCallbackErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "Invalid extension token"
                });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ExtensionCallbackErrorResponse
                {
                    Error = "Bad Request",
                    Message = "Log message cannot be null or empty"
                });
            }

            try
            {
                // Get extension info for context
                var extInfo = m_ExtensionTracker.GetCommandInfo(commandId ?? string.Empty);
                var extensionName = extInfo?.ExtensionName ?? "unknown";

                // Write log message with commandId prefix for tracking multiple concurrent executions
                var logMessage = $"[{extensionName}] [{commandId}] {request.Message}";

                switch (request.Level?.ToLowerInvariant())
                {
                    case "debug":
                        m_Logger.LogDebug("{LogMessage}", logMessage);
                        break;
                    case "information":
                    case "info":
                        m_Logger.LogInformation("{LogMessage}", logMessage);
                        break;
                    case "warning":
                    case "warn":
                        m_Logger.LogWarning("{LogMessage}", logMessage);
                        break;
                    case "error":
                        m_Logger.LogError("{LogMessage}", logMessage);
                        break;
                    default:
                        m_Logger.LogInformation("{LogMessage}", logMessage);
                        break;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to process extension log request");
                return StatusCode(500, new ExtensionCallbackErrorResponse
                {
                    Error = "Internal Server Error",
                    Message = $"Failed to write log: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Checks if the request is from localhost.
        /// </summary>
        /// <returns>True if the request is from localhost, false otherwise.</returns>
        private bool IsLocalhost()
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp == null)
                return false;

            return IPAddress.IsLoopback(remoteIp) ||
                   remoteIp.ToString() == "::1" ||
                   remoteIp.ToString() == "127.0.0.1";
        }

        /// <summary>
        /// Extracts the bearer token from the Authorization header.
        /// </summary>
        /// <returns>The token string, or null if not found.</returns>
        private string? ExtractBearerToken()
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
                return null;

            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader[7..].Trim();

            return authHeader;
        }
    }
}

