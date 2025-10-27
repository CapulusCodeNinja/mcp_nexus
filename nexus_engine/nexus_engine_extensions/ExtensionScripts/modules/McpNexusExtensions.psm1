<#
.SYNOPSIS
PowerShell helper module for MCP Nexus extensions.

.DESCRIPTION
Provides simple helper functions for extension scripts to interact with the MCP Nexus server.
This module handles all the complexity of HTTP callbacks, token management, and command execution.

.NOTES
Version: 1.1.0
Author: MCP Nexus Team
#>

# Module-level variables
$script:CallbackUrl = $null
$script:CallbackToken = $null
$script:SessionId = $null
$script:RequestCounter = 0
$script:RollingCallbackCounter = 0
$script:ExtensionName = $null

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
    param(
        [Parameter(Mandatory=$true)]
        [string]$SessionId,
        
        [Parameter(Mandatory=$true)]
        [string]$Token,
        
        [Parameter(Mandatory=$true)]
        [string]$CallbackUrl,
        
        [Parameter(Mandatory=$true)]
        [string]$ScriptName
    )
    
    $script:CallbackUrl = $CallbackUrl
    $script:CallbackToken = $Token
    $script:SessionId = $SessionId
    $script:RollingCallbackCounter = 1
    $script:ExtensionName = $ScriptName
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
        throw "MCP Nexus environment not initialized. Call Initialize-McpNexusExtension first."
    }

    $script:RequestCounter++
    Write-NexusLog "Executing command #$script:RequestCounter`: $Command" -Level Debug

    try {
        $body = @{
            command = $Command
            timeoutSeconds = $TimeoutSeconds
        } | ConvertTo-Json

        $headers = @{
            "Authorization" = "Bearer $script:CallbackToken"
            "Content-Type" = "application/json"
            "X-Extension-PID" = $PID
            "X-Script-Extension-Name" = $script:ExtensionName
            "X-Callback-Counter" = $script:RollingCallbackCounter
        }

        $response = Invoke-RestMethod `
            -Uri "$script:CallbackUrl/execute" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -TimeoutSec ($TimeoutSeconds + 10)

        if ($response.state -eq "Completed" -or $response.state -eq "Success") {
            Write-NexusLog "Command completed successfully" -Level Debug
            return $response.output
        }
        else {
            $errorMsg = if ($response.error) { $response.error } else { "Command failed with state: $($response.state)" }
            Write-NexusLog "Command execution failed: $errorMsg" -Level Error
            throw $errorMsg
        }
    }
    catch {
        Write-NexusLog "Failed to execute command '$Command': $_" -Level Error
        throw
    } finally {
        ++$script:RollingCallbackCounter
    }
}

<#
.SYNOPSIS
Queues a WinDBG command for asynchronous execution.

.DESCRIPTION
Queues a command without waiting for it to complete. This enables command batching
for improved throughput. Use Wait-NexusCommand or Get-NexusCommandResult to retrieve results.

.PARAMETER Command
The WinDBG command to execute (e.g., "lm", "!threads", "lsa 00007ff8`12345678").

.PARAMETER TimeoutSeconds
Maximum time to wait for command completion when later calling Wait-NexusCommand (default: 300 seconds).

.EXAMPLE
$cmdId = Start-NexusCommand "lm"
$result = Wait-NexusCommand -CommandId $cmdId

.EXAMPLE
$ids = @()
$ids += Start-NexusCommand "lm"
$ids += Start-NexusCommand "!threads"
$ids += Start-NexusCommand "!peb"
foreach ($id in $ids) {
    $result = Wait-NexusCommand -CommandId $id
    Write-Output $result
}

.OUTPUTS
String - The command ID that can be used to retrieve results.

.NOTES
This function enables command batching by queuing multiple commands before they execute.
#>
function Start-NexusCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [ValidateNotNullOrEmpty()]
        [string[]]$Command,

        [Parameter(Mandatory=$false)]
        [int]$TimeoutSeconds = 300
    )

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        throw "MCP Nexus environment not initialized. Call Initialize-McpNexusExtension first."
    }

    $commandIds = @()
    
    foreach ($cmd in $Command) {
        $script:RequestCounter++

        try {
            $body = @{
                command = $cmd
                timeoutSeconds = $TimeoutSeconds
            } | ConvertTo-Json

            $headers = @{
                "Authorization" = "Bearer $script:CallbackToken"
                "Content-Type" = "application/json"
                "X-Extension-PID" = $PID
                "X-Script-Extension-Name" = $script:ExtensionName
                "X-Callback-Counter" = $script:RollingCallbackCounter
            }
        
            $response = Invoke-RestMethod `
                -Uri "$script:CallbackUrl/queue" `
                -Method POST `
                -Headers $headers `
                -Body $body `
                -TimeoutSec 30

            Write-NexusLog "Command '$cmd' queued with ID: $($response.commandId)" -Level Debug
            $commandIds += $response.commandId
        }
        catch {
            Write-NexusLog "Failed to queue command '$cmd': $_" -Level Error
            throw
        } finally {
            ++$script:RollingCallbackCounter
        }
    }

    # Return single ID if only one command, array if multiple
    if ($commandIds.Count -eq 1) {
        return $commandIds[0]
    }
    return $commandIds
}

<#
.SYNOPSIS
Gets the status and result of a previously queued command.

.DESCRIPTION
Retrieves the current status and result of a command queued with Start-NexusCommand.
Returns immediately with the current state without waiting.

.PARAMETER CommandId
The command ID returned by Start-NexusCommand.

.EXAMPLE
$cmdId = Start-NexusCommand "lm"
Start-Sleep -Seconds 2
$status = Get-NexusCommandResult -CommandId $cmdId
if ($status.isCompleted) {
    Write-Output $status.output
}

.OUTPUTS
PSCustomObject - An object with properties: commandId, status, isCompleted, output, error

.NOTES
This function does not wait for command completion. Use Wait-NexusCommand for blocking behavior.
#>
function Get-NexusCommandResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$CommandId
    )

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        throw "MCP Nexus environment not initialized. Call Initialize-McpNexusExtension first."
    }

    try {
        $body = @{ commandId = $CommandId } | ConvertTo-Json
        $headers = @{
            "Authorization" = "Bearer $script:CallbackToken"
            "Content-Type" = "application/json"
            "X-Extension-PID" = $PID
            "X-Script-Extension-Name" = $script:ExtensionName
            "X-Callback-Counter" = $script:RollingCallbackCounter
        }

        $response = Invoke-RestMethod `
            -Uri "$script:CallbackUrl/read" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -TimeoutSec 10

        return $response
    }
    catch {
        Write-NexusLog "Failed to get result for command '$CommandId': $_" -Level Error
        throw
    } finally {
        ++$script:RollingCallbackCounter
    }
}

<#
.SYNOPSIS
Waits for a queued command to complete and returns the result.

.DESCRIPTION
Polls a queued command until it completes or times out. This is a blocking operation
that waits for the command to finish executing.

.PARAMETER CommandId
The command ID returned by Start-NexusCommand.

.PARAMETER TimeoutSeconds
Maximum time to wait for command completion (default: 300 seconds).

.PARAMETER PollIntervalMs
Milliseconds to wait between status checks (default: 1000ms).

.EXAMPLE
$cmdId = Start-NexusCommand "lm"
$result = Wait-NexusCommand -CommandId $cmdId

.EXAMPLE
$cmdId = Start-NexusCommand "!analyze -v"
$result = Wait-NexusCommand -CommandId $cmdId -TimeoutSeconds 600 -PollIntervalMs 2000

.OUTPUTS
String - The output from the WinDBG command.

.NOTES
This function blocks until the command completes. For non-blocking checks, use Get-NexusCommandResult.
#>function Wait-NexusCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]$CommandId,

        [Parameter(Mandatory=$false)]
        [int]$TimeoutSeconds = 300,

        [Parameter(Mandatory=$false)]
        [int]$PollIntervalMs = 1000
    )

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        throw "MCP Nexus environment not initialized. Call Initialize-McpNexusExtension first."
    }

    if ($CommandId.Count -eq 0) {
        return @()  # Always return array
    }

    $startTime = Get-Date
    $timeout = [TimeSpan]::FromSeconds($TimeoutSeconds)
    $completedCommands = @{}
    $results = @()  # Always array

    do {
        $allCompleted = $true
        $remainingCommands = @()

        try {
            $bulkResults = Get-NexusCommandStatus -CommandIds $CommandId

            foreach ($cmdId in $CommandId) {
                if ($completedCommands.ContainsKey($cmdId)) { continue }

                if ($bulkResults.ContainsKey($cmdId)) {
                    $result = $bulkResults[$cmdId]
                    if ($result.isCompleted) {
                        $completedCommands[$cmdId] = $true

                        if ($result.state -eq "Success" -or $result.state -eq "Completed") {
                            $results += $result.output
                        }
                        else {
                            $errorMsg = if ($result.error) { $result.error } else { "Command failed with state: $($result.state)" }
                            throw "Command $cmdId failed: $errorMsg"
                        }
                    } else {
                        $allCompleted = $false
                        $remainingCommands += $cmdId
                    }
                } else {
                    $allCompleted = $false
                    $remainingCommands += $cmdId
                }
            }
        }
        catch {
            Write-NexusLog "Error while checking bulk command state: $_" -Level Error
            throw
        }

        if ($allCompleted) {
            return ,$results  # <— COMMA forces array even if 1 element
        }

        Start-Sleep -Milliseconds $PollIntervalMs
    } while ((Get-Date) - $startTime -lt $timeout)

    throw "Timeout waiting for commands."
}


<#
.SYNOPSIS
Writes a log message to the MCP Nexus server log file.

.DESCRIPTION
Sends a log message to the MCP Nexus server where it will be written to the server's log file.
This allows extensions to provide diagnostic information that can be reviewed in the server logs.

The server automatically prefixes all log messages with the extension name and command ID for 
tracking purposes, which is essential when multiple extension scripts are running concurrently.

Log format in server: [Extension: extension_name | ext-xxxxx] Your message

.PARAMETER Message
The log message to write.

.PARAMETER Level
The log level: Debug, Information, Warning, or Error. Defaults to Information.

.EXAMPLE
Write-NexusLog "Processing address 0x12345678"
# Server logs: [Extension: stack_with_sources | ext-abc123] Processing address 0x12345678

.EXAMPLE
Write-NexusLog "Failed to download source file" -Level Error
# Server logs: [Extension: stack_with_sources | ext-abc123] Failed to download source file

.EXAMPLE
Write-NexusLog "Starting memory corruption analysis" -Level Information
# Server logs: [Extension: memory_corruption_analysis | ext-xyz789] Starting memory corruption analysis

.NOTES
Use appropriate log levels:
- Debug: Detailed diagnostic information (verbose, not normally needed)
- Information: General informational messages about extension progress
- Warning: Recoverable issues or unexpected situations
- Error: Errors that prevent part of the extension from completing

Avoid log spam - log only significant events, not every iteration of a loop.

The command ID is automatically included by the server, so you don't need to add it manually.
This ensures log messages can be correlated when multiple instances of the same extension 
are running simultaneously for different sessions.
#>
function Write-NexusLog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [ValidateNotNullOrEmpty()]
        [string]$Message,

        [Parameter(Mandatory=$true)]
        [ValidateSet('Trace', 'Debug', 'Information', 'Warning', 'Error', 'Fatal')]
        [string]$Level
    )

    try {
        $body = @{
            message = $Message
            level = $Level
        } | ConvertTo-Json

        $headers = @{
            "Authorization" = "Bearer $script:CallbackToken"
            "Content-Type" = "application/json"
            "X-Extension-PID" = $PID
            "X-Script-Extension-Name" = $script:ExtensionName
            "X-Callback-Counter" = $script:RollingCallbackCounter
        }

        # Use fire-and-forget pattern for logging (don't wait for response)
        $null = Invoke-RestMethod `
            -Uri "$script:CallbackUrl/log" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -TimeoutSec 5 `
            -ErrorAction SilentlyContinue

        switch ($level) {
            'Trace' {
                Write-Debug $Message
            }
            'Debug' {
                Write-Debug $Message
            }
            'Information' {
                Write-Information $Message
            }
            'Warning' {
                Write-Warning $Message
            }
            'Error' {
                Write-Error $Message
            }
            'Fatal' {
                Write-Error $Message
            }
        }
    }
    catch {
        # Silently ignore logging errors to avoid breaking extension execution
        Write-Error "Failed to send log to server: $_"
    } finally {
        ++$script:RollingCallbackCounter
    }
}

# Auto-initialize when module is imported
# Note: Scripts must now call Initialize-McpNexusExtension explicitly with parameters

<#
.SYNOPSIS
Gets the status of multiple commands in a single request.

.DESCRIPTION
Efficiently retrieves the status and results of multiple commands using the bulk status endpoint.
This is much more efficient than calling Get-NexusCommandResult multiple times.

.PARAMETER CommandIds
Array of command IDs to check.

.EXAMPLE
$commandIds = @("cmd-123", "cmd-124", "cmd-125")
$results = Get-NexusCommandStatus -CommandIds $commandIds
foreach ($cmdId in $commandIds) {
    if ($results[$cmdId].isCompleted) {
        Write-Output "Command $cmdId completed: $($results[$cmdId].output)"
    }
}

.OUTPUTS
Hashtable - Dictionary with command IDs as keys and status objects as values.

.NOTES
This function uses the bulk status endpoint for optimal performance.
#>
function Get-NexusCommandStatus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]$CommandIds
    )

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        throw "MCP Nexus environment not initialized. Call Initialize-McpNexusExtension first."
    }

    if ($CommandIds.Count -eq 0) {
        return @{}
    }

    try {
        $body = @{ commandIds = $CommandIds } | ConvertTo-Json
        $headers = @{
            "Authorization" = "Bearer $script:CallbackToken"
            "Content-Type" = "application/json"
            "X-Extension-PID" = $PID
            "X-Script-Extension-Name" = $script:ExtensionName
            "X-Callback-Counter" = $script:RollingCallbackCounter
        }

        $response = Invoke-RestMethod `
            -Uri "$script:CallbackUrl/status" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -TimeoutSec 10

        if (-not $response.results) {
            throw "Bulk status response missing 'results' property"
        }

        # Convert PSCustomObject results to a Hashtable so callers can use ContainsKey/indexing reliably
        $resultsTable = @{}
        foreach ($prop in $response.results.PSObject.Properties) {
            $resultsTable[$prop.Name] = $prop.Value
        }

        return $resultsTable
    }
    catch {
        Write-NexusLog "Failed to get bulk status for commands: $_" -Level Error
        throw
    } finally {
        ++$script:RollingCallbackCounter
    }
}

# Export functions
Export-ModuleMember -Function @(
    'Initialize-McpNexusExtension',
    'Invoke-NexusCommand',
    'Start-NexusCommand',
    'Get-NexusCommandResult',
    'Get-NexusCommandStatus',
    'Wait-NexusCommand',
    'Write-NexusLog'
)

