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
        Initialize-McpNexusExtension
    }

    $commandIds = @()
    
    foreach ($cmd in $Command) {
        $script:RequestCounter++
        Write-Verbose "Queueing command #$script:RequestCounter: $cmd"

        # Output marker for MCP Nexus to count callbacks
        Write-Output "[CALLBACK] Queueing: $cmd"

        try {
            $body = @{
                command = $cmd
                timeoutSeconds = $TimeoutSeconds
            } | ConvertTo-Json

            $headers = @{
                "Authorization" = "Bearer $script:CallbackToken"
                "Content-Type" = "application/json"
            }

            $response = Invoke-RestMethod `
                -Uri "$script:CallbackUrl/queue" `
                -Method POST `
                -Headers $headers `
                -Body $body `
                -TimeoutSec 30

            Write-Verbose "Command queued with ID: $($response.commandId)"
            $commandIds += $response.commandId
        }
        catch {
            Write-Error "Failed to queue command '$cmd': $_"
            throw
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
        Initialize-McpNexusExtension
    }

    try {
        $body = @{ commandId = $CommandId } | ConvertTo-Json
        $headers = @{
            "Authorization" = "Bearer $script:CallbackToken"
            "Content-Type" = "application/json"
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
        Write-Error "Failed to get result for command '$CommandId': $_"
        throw
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
#>
function Wait-NexusCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]$CommandId,

        [Parameter(Mandatory=$false)]
        [int]$TimeoutSeconds = 300,

        [Parameter(Mandatory=$false)]
        [int]$PollIntervalMs = 1000,

        [Parameter(Mandatory=$false)]
        [bool]$ReturnResults = $false
    )

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        Initialize-McpNexusExtension
    }

    if ($CommandId.Count -eq 0) {
        return if ($ReturnResults) { @{} } else { @() }
    }

    $isSingleCommand = $CommandId.Count -eq 1
    $commandDisplay = if ($isSingleCommand) { $CommandId[0] } else { "$($CommandId.Count) commands" }
    Write-Verbose "Waiting for $commandDisplay to complete (timeout: ${TimeoutSeconds}s)..."

    $startTime = Get-Date
    $timeout = [TimeSpan]::FromSeconds($TimeoutSeconds)
    $completedCommands = @{}
    $results = if ($ReturnResults) { @{} } else { @() }

    do {
        $allCompleted = $true
        $remainingCommands = @()

        try {
            # Use bulk status endpoint for efficient polling
            $bulkResults = Get-NexusCommandStatus -CommandIds $CommandId

            foreach ($cmdId in $CommandId) {
                if ($completedCommands.ContainsKey($cmdId)) {
                    continue
                }

                if ($bulkResults.ContainsKey($cmdId)) {
                    $result = $bulkResults[$cmdId]

                    if ($result.isCompleted) {
                        $completedCommands[$cmdId] = $true
                        
                        if ($result.status -eq "Success" -or $result.status -eq "Completed") {
                            Write-Verbose "Command $cmdId completed successfully"
                            if ($ReturnResults) {
                                $results[$cmdId] = $result.output
                            } else {
                                $results += $result.output
                            }
                        }
                        else {
                            $errorMsg = if ($result.error) { $result.error } else { "Command failed with status: $($result.status)" }
                            Write-Error "Command $cmdId failed: $errorMsg"
                            throw "Command $cmdId failed: $errorMsg"
                        }
                    }
                    else {
                        $allCompleted = $false
                        $remainingCommands += $cmdId
                    }
                }
                else {
                    Write-Warning "Command $cmdId not found in status response"
                    $allCompleted = $false
                    $remainingCommands += $cmdId
                }
            }
        }
        catch {
            Write-Error "Error while checking bulk command status: $_"
            throw
        }

        if ($allCompleted) {
            Write-Verbose "All $($CommandId.Count) commands completed successfully"
            # For single command, return just the output; for multiple, return array/hashtable
            if ($isSingleCommand) {
                return if ($ReturnResults) { $results[$CommandId[0]] } else { $results[0] }
            }
            return $results
        }

        $completedCount = $completedCommands.Count
        Write-Verbose "Commands completed: $completedCount/$($CommandId.Count), remaining: $($remainingCommands -join ', ')"
        Start-Sleep -Milliseconds $PollIntervalMs

    } while ((Get-Date) - $startTime -lt $timeout)

    $completedCount = $completedCommands.Count
    $remainingCount = $CommandId.Count - $completedCount
    throw "Timeout: Only $completedCount of $($CommandId.Count) commands completed within $TimeoutSeconds seconds. $remainingCount commands still running."
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
        Initialize-McpNexusExtension
    }

    if ($CommandIds.Count -eq 0) {
        return @{}
    }

    try {
        $body = @{ commandIds = $CommandIds } | ConvertTo-Json
        $headers = @{
            "Authorization" = "Bearer $script:CallbackToken"
            "Content-Type" = "application/json"
        }

        $response = Invoke-RestMethod `
            -Uri "$script:CallbackUrl/status" `
            -Method POST `
            -Headers $headers `
            -Body $body `
            -TimeoutSec 10

        # Convert PSCustomObject results to a Hashtable so callers can use ContainsKey/indexing reliably
        $resultsTable = @{}
        foreach ($prop in $response.results.PSObject.Properties) {
            $resultsTable[$prop.Name] = $prop.Value
        }

        return $resultsTable
    }
    catch {
        Write-Error "Failed to get bulk status for commands: $_"
        throw
    }
}

<#
.SYNOPSIS
Waits for multiple queued commands to complete and returns their results.

.DESCRIPTION
Efficiently polls multiple queued commands until they all complete or timeout.
This function optimizes the waiting process by checking all commands in a single
polling loop rather than waiting for each command individually.

.PARAMETER CommandIds
Array of command IDs returned by Start-NexusCommand or Start-NexusCommands.

.PARAMETER TimeoutSeconds
Maximum time to wait for all commands to complete (default: 300 seconds).

.PARAMETER PollIntervalMs
Milliseconds to wait between status checks (default: 1000ms).

.PARAMETER ReturnResults
If true, returns a hashtable with command IDs as keys and outputs as values.
If false, returns an array of outputs in the same order as input command IDs.

.EXAMPLE
$commandIds = Start-NexusCommands -Commands @("lm", "!threads", "!peb")
$results = Wait-NexusCommands -CommandIds $commandIds -ReturnResults $true
Write-Output "Modules: $($results['cmd-123'])"
Write-Output "Threads: $($results['cmd-124'])"

.EXAMPLE
$commandIds = Start-NexusCommands -Commands @("lm", "!threads", "!peb")
$outputs = Wait-NexusCommands -CommandIds $commandIds -ReturnResults $false
$modules = $outputs[0]
$threads = $outputs[1]
$peb = $outputs[2]

.OUTPUTS
Hashtable or Array - Command results based on ReturnResults parameter.

.NOTES
This function is much more efficient than calling Wait-NexusCommand in a loop
because it polls all commands together rather than waiting for each individually.
#>
function Wait-NexusCommands {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]$CommandIds,

        [Parameter(Mandatory=$false)]
        [int]$TimeoutSeconds = 300,

        [Parameter(Mandatory=$false)]
        [int]$PollIntervalMs = 1000,

        [Parameter(Mandatory=$false)]
        [bool]$ReturnResults = $false
    )

    if ([string]::IsNullOrWhiteSpace($script:CallbackUrl)) {
        Initialize-McpNexusExtension
    }

    if ($CommandIds.Count -eq 0) {
        return if ($ReturnResults) { @{} } else { @() }
    }

    Write-Verbose "Waiting for $($CommandIds.Count) commands to complete (timeout: ${TimeoutSeconds}s)..."

    $startTime = Get-Date
    $timeout = [TimeSpan]::FromSeconds($TimeoutSeconds)
    $completedCommands = @{}
    $results = if ($ReturnResults) { @{} } else { @() }

    do {
        $allCompleted = $true
        $remainingCommands = @()

        try {
            # Use bulk status endpoint for efficient polling
            $bulkResults = Get-NexusCommandStatus -CommandIds $CommandIds

            foreach ($cmdId in $CommandIds) {
                if ($completedCommands.ContainsKey($cmdId)) {
                    continue
                }

                if ($bulkResults.ContainsKey($cmdId)) {
                    $result = $bulkResults[$cmdId]

                    if ($result.isCompleted) {
                        $completedCommands[$cmdId] = $true
                        
                        if ($result.status -eq "Success" -or $result.status -eq "Completed") {
                            Write-Verbose "Command $cmdId completed successfully"
                            if ($ReturnResults) {
                                $results[$cmdId] = $result.output
                            } else {
                                $results += $result.output
                            }
                        }
                        else {
                            $errorMsg = if ($result.error) { $result.error } else { "Command failed with status: $($result.status)" }
                            Write-Error "Command $cmdId failed: $errorMsg"
                            throw "Command $cmdId failed: $errorMsg"
                        }
                    }
                    else {
                        $allCompleted = $false
                        $remainingCommands += $cmdId
                    }
                }
                else {
                    Write-Warning "Command $cmdId not found in bulk status response"
                    $allCompleted = $false
                    $remainingCommands += $cmdId
                }
            }
        }
        catch {
            Write-Error "Error while checking bulk command status: $_"
            throw
        }

        if ($allCompleted) {
            Write-Verbose "All $($CommandIds.Count) commands completed successfully"
            return $results
        }

        $completedCount = $completedCommands.Count
        Write-Verbose "Commands completed: $completedCount/$($CommandIds.Count), remaining: $($remainingCommands -join ', ')"
        Start-Sleep -Milliseconds $PollIntervalMs

    } while ((Get-Date) - $startTime -lt $timeout)

    $completedCount = $completedCommands.Count
    $remainingCount = $CommandIds.Count - $completedCount
    throw "Timeout: Only $completedCount of $($CommandIds.Count) commands completed within $TimeoutSeconds seconds. $remainingCount commands still running."
}

# Export functions
Export-ModuleMember -Function @(
    'Invoke-NexusCommand',
    'Start-NexusCommand',
    'Get-NexusCommandResult',
    'Get-NexusCommandStatus',
    'Wait-NexusCommand',
    'Write-NexusProgress',
    'Write-NexusLog',
    'Get-NexusSessionId',
    'Get-NexusCommandId'
)

