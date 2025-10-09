<#
.SYNOPSIS
PowerShell helper module for MCP Nexus extensions.

.DESCRIPTION
Provides simple helper functions for extension scripts to interact with the MCP Nexus server.
This module handles all the complexity of HTTP callbacks, token management, and command execution.

.NOTES
Version: 1.0.0
Author: MCP Nexus Team
#>

# Module-level variables
$script:CallbackUrl = $null
$script:CallbackToken = $null
$script:SessionId = $null
$script:CommandId = $null
$script:RequestCounter = 0

<#
.SYNOPSIS
Initializes the MCP Nexus extension environment.

.DESCRIPTION
Reads environment variables set by the MCP Nexus server and initializes the module.
This function is automatically called when the module is imported.

.NOTES
This function is called automatically and usually doesn't need to be called manually.
#>
function Initialize-McpNexusExtension {
    $script:CallbackUrl = $env:MCP_NEXUS_CALLBACK_URL
    $script:CallbackToken = $env:MCP_NEXUS_CALLBACK_TOKEN
    $script:SessionId = $env:MCP_NEXUS_SESSION_ID
    $script:CommandId = $env:MCP_NEXUS_COMMAND_ID

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        throw "MCP_NEXUS_CALLBACK_URL environment variable not set. This script must be run by MCP Nexus."
    }

    if ([string]::IsNullOrWhiteSpace($script:CallbackToken)) {
        throw "MCP_NEXUS_CALLBACK_TOKEN environment variable not set. This script must be run by MCP Nexus."
    }

    Write-Verbose "MCP Nexus Extension initialized: SessionId=$script:SessionId, CommandId=$script:CommandId"
}

<#
.SYNOPSIS
Executes a WinDBG command in the current debugging session.

.DESCRIPTION
Sends a command to the MCP Nexus server for execution in the debugger.
This function blocks until the command completes and returns the output.

.PARAMETER Command
The WinDBG command to execute (e.g., "~8kL", "!analyze -v", "lsa 00007ff8`12345678").

.PARAMETER TimeoutSeconds
Maximum time to wait for command completion (default: 300 seconds).

.EXAMPLE
$stack = Invoke-NexusCommand "~8kL"

.EXAMPLE
$analysis = Invoke-NexusCommand "!analyze -v" -TimeoutSeconds 600

.OUTPUTS
String - The output from the WinDBG command.

.NOTES
This function automatically handles authentication and error reporting.
#>
function Invoke-NexusCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [ValidateNotNullOrEmpty()]
        [string]$Command,

        [Parameter(Mandatory=$false)]
        [int]$TimeoutSeconds = 300
    )

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        Initialize-McpNexusExtension
    }

    $script:RequestCounter++
    Write-Verbose "Executing command #$script:RequestCounter`: $Command"

    # Output marker for MCP Nexus to count callbacks
    Write-Output "[CALLBACK] Executing: $Command"

    try {
        $body = @{
            command = $Command
            timeoutSeconds = $TimeoutSeconds
        } | ConvertTo-Json

        $headers = @{
            "Authorization" = "Bearer $script:CallbackToken"
            "Content-Type" = "application/json"
        }

        $response = Invoke-RestMethod `
            -Uri "$script:CallbackUrl/execute" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -TimeoutSec ($TimeoutSeconds + 10)

        if ($response.status -eq "Completed" -or $response.status -eq "Success") {
            Write-Verbose "Command completed successfully"
            return $response.output
        }
        else {
            $errorMsg = if ($response.error) { $response.error } else { "Command failed with status: $($response.status)" }
            Write-Error "Command execution failed: $errorMsg"
            throw $errorMsg
        }
    }
    catch {
        Write-Error "Failed to execute command '$Command': $_"
        throw
    }
}

<#
.SYNOPSIS
Writes a progress message that will be tracked by MCP Nexus.

.DESCRIPTION
Outputs a progress message in a format that MCP Nexus recognizes and reports to the AI.

.PARAMETER Message
The progress message to report.

.EXAMPLE
Write-NexusProgress "Downloading sources: 5 of 40 completed"

.NOTES
Progress messages are visible in the extension command status.
#>
function Write-NexusProgress {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$Message
    )

    Write-Output "[PROGRESS] $Message"
}

<#
.SYNOPSIS
Writes a log message to the MCP Nexus server log file.

.DESCRIPTION
Sends a log message to the MCP Nexus server where it will be written to the server's log file.
This allows extensions to provide diagnostic information that can be reviewed in the server logs.

.PARAMETER Message
The log message to write.

.PARAMETER Level
The log level: Debug, Information, Warning, or Error. Defaults to Information.

.EXAMPLE
Write-NexusLog "Processing address 0x12345678"

.EXAMPLE
Write-NexusLog "Failed to download source file" -Level Error

.EXAMPLE
Write-NexusLog "Starting memory corruption analysis" -Level Information

.NOTES
Use appropriate log levels:
- Debug: Detailed diagnostic information (verbose, not normally needed)
- Information: General informational messages about extension progress
- Warning: Recoverable issues or unexpected situations
- Error: Errors that prevent part of the extension from completing

Avoid log spam - log only significant events, not every iteration of a loop.
#>
function Write-NexusLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [ValidateNotNullOrEmpty()]
        [string]$Message,

        [Parameter(Mandatory=$false)]
        [ValidateSet('Debug', 'Information', 'Warning', 'Error')]
        [string]$Level = 'Information'
    )

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        Initialize-McpNexusExtension
    }

    try {
        $body = @{
            message = $Message
            level = $Level
        } | ConvertTo-Json

        $headers = @{
            "Authorization" = "Bearer $script:CallbackToken"
            "Content-Type" = "application/json"
        }

        # Use fire-and-forget pattern for logging (don't wait for response)
        $null = Invoke-RestMethod `
            -Uri "$script:CallbackUrl/log" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -TimeoutSec 5 `
            -ErrorAction SilentlyContinue
    }
    catch {
        # Silently ignore logging errors to avoid breaking extension execution
        Write-Verbose "Failed to send log to server: $_"
    }
}

<#
.SYNOPSIS
Gets the session ID for the current extension execution.

.OUTPUTS
String - The session ID.
#>
function Get-NexusSessionId {
    if ([string]::IsNullOrWhiteSpace($script:SessionId)) {
        Initialize-McpNexusExtension
    }
    return $script:SessionId
}

<#
.SYNOPSIS
Gets the command ID for the current extension execution.

.OUTPUTS
String - The command ID.
#>
function Get-NexusCommandId {
    if ([string]::IsNullOrWhiteSpace($script:CommandId)) {
        Initialize-McpNexusExtension
    }
    return $script:CommandId
}

# Auto-initialize when module is imported
try {
    Initialize-McpNexusExtension
}
catch {
    # Ignore errors during module import for non-extension contexts
    Write-Warning "McpNexusExtensions module: Not running in MCP Nexus extension context: $_"
}

# Export functions
Export-ModuleMember -Function @(
    'Invoke-NexusCommand',
    'Write-NexusProgress',
    'Write-NexusLog',
    'Get-NexusSessionId',
    'Get-NexusCommandId'
)

