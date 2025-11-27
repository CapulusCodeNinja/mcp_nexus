using System.Diagnostics;
using System.Text;

using Nexus.Config;
using Nexus.Engine.Preprocessing;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using NLog;

namespace Nexus.Engine.Internal;

/// <summary>
/// Internal CDB session that manages a single CDB process and command execution.
/// </summary>
internal class CdbSession : ICdbSession
{
    private readonly Logger m_Logger;
    private readonly ISettings m_Settings;

    private readonly IFileSystem m_FileSystem;
    private readonly IProcessManager m_ProcessManager;
    private readonly CommandPreprocessor m_CommandPreprocessor;
    private readonly SemaphoreSlim m_ExecutionSemaphore = new(1, 1);
    private readonly object m_ProcessLock = new();

    private Process? m_CdbProcess;
    private StreamWriter? m_InputWriter;
    private ProcessOutputAggregator? m_OutputAggregator;
    private volatile bool m_Disposed = false;

    /// <summary>
    /// Backing field indicating whether the session has completed initialization.
    /// </summary>
    private volatile bool m_Initialized = false;

    /// <summary>
    /// Gets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive => m_Initialized && !m_Disposed && m_CdbProcess?.HasExited == false;

    /// <summary>
    /// Gets the session ID.
    /// </summary>
    public string SessionId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the dump file path.
    /// </summary>
    public string DumpFilePath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the symbol path.
    /// </summary>
    public string? SymbolPath
    {
        get; private set;
    }

    /// <summary>
    /// Gets the process identifier.
    /// </summary>
    public int? ProcessId => m_CdbProcess?.Id ?? null;

    /// <summary>
    /// Gets a value indicating whether the session is initialized.
    /// </summary>
    public bool IsInitialized => m_Initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdbSession"/> class.
    /// </summary>
    /// <param name="settings">The product settings.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    /// <param name="commandPreprocessor">Command preprocessor for WSL path conversion and directory creation.</param>
    /// <exception cref="ArgumentNullException">Thrown when fileSystem, processManager, or commandPreprocessor is null.</exception>
    public CdbSession(
        ISettings settings,
        IFileSystem fileSystem,
        IProcessManager processManager,
        CommandPreprocessor commandPreprocessor)
    {
        m_Settings = settings;
        m_Logger = LogManager.GetCurrentClassLogger();
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        m_CommandPreprocessor = commandPreprocessor ?? throw new ArgumentNullException(nameof(commandPreprocessor));
    }

    /// <summary>
    /// Initializes the CDB session with a dump file.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="dumpFilePath">The path to the dump file.</param>
    /// <param name="symbolPath">Optional symbol path.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InitializeAsync(string sessionId, string dumpFilePath, string? symbolPath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(dumpFilePath))
        {
            throw new ArgumentException("Dump file path cannot be null or empty", nameof(dumpFilePath));
        }

        if (!m_FileSystem.FileExists(dumpFilePath))
        {
            throw new FileNotFoundException($"Dump file not found: {dumpFilePath}", dumpFilePath);
        }

        // Store the session ID and paths
        SessionId = sessionId;
        DumpFilePath = dumpFilePath;
        SymbolPath = symbolPath;

        m_Logger.Info("Initializing CDB session {SessionId} for dump file: {DumpFilePath}", sessionId, dumpFilePath);

        try
        {
            await m_ExecutionSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Find CDB executable
                var cdbPath = await FindCdbExecutableAsync();

                // Build command line arguments
                var arguments = BuildCommandLineArguments();

                // Start CDB process
                await StartCdbProcessAsync(cdbPath, arguments);

                // Wait for CDB to initialize
                await WaitForCdbInitializationAsync(cancellationToken);

                m_Initialized = true;
                m_Logger.Info("CDB session initialized successfully");
            }
            finally
            {
                _ = m_ExecutionSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to initialize CDB session");
            await DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Executes a command in the CDB session.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command output.</returns>
    public async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        m_Logger.Debug("Executing CDB command: {Command}", command);

        // Apply command preprocessing
        var processedCommand = m_CommandPreprocessor.PreprocessCommand(command);
        if (processedCommand != command)
        {
            m_Logger.Debug("Command preprocessed: {Command}", processedCommand);
        }

        try
        {
            await m_ExecutionSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Create command with sentinels
                var wrappedCommand = CreateCommandWithSentinels(processedCommand);

                // Send command to CDB
                await SendCommandToCdbAsync(wrappedCommand);

                // Read output until completion
                var output = await ReadCommandOutputAsync(cancellationToken);

                m_Logger.Debug("CDB command \"{Command}\" completed with {OutputLength} characters of output", command, output.Length);
                return output;
            }
            finally
            {
                _ = m_ExecutionSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            m_Logger.Debug("CDB command cancelled");
            throw;
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error executing CDB command: {Command}", command);
            throw;
        }
    }

    /// <summary>
    /// Disposes the CDB session.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Logger.Debug("Disposing CDB session");

        try
        {
            await SendQuitCommandAsync();
            await WaitForProcessExitAsync();
            await CompressCdbLogIfAvailableAsync();
            DisposeResources();
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error disposing CDB session");
        }
        finally
        {
            SetDisposedState();
        }
    }

    /// <summary>
    /// Sends the quit command to CDB.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual async Task SendQuitCommandAsync()
    {
        if (IsProcessExited())
        {
            m_Logger.Debug("Skipping quit command because CDB process has already exited");
            return;
        }

        if (m_InputWriter != null)
        {
            try
            {
                await WriteQuitCommandAsync();
                await FlushInputAsync();
            }
            catch (Exception ex)
            {
                m_Logger.Warn(ex, "Error sending quit command to CDB");
            }
        }
    }

    /// <summary>
    /// Writes the quit command to the input stream.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual async Task WriteQuitCommandAsync()
    {
        await m_InputWriter!.WriteLineAsync("q");
    }

    /// <summary>
    /// Flushes the input stream.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual async Task FlushInputAsync()
    {
        await m_InputWriter!.FlushAsync();
    }

    /// <summary>
    /// Waits for the CDB process to exit.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual Task WaitForProcessExitAsync()
    {
        if (m_CdbProcess != null && !m_CdbProcess.HasExited)
        {
            try
            {
                if (!m_CdbProcess.WaitForExit((int)m_Settings.Get().McpNexus.SessionManagement.GetCleanupInterval().TotalMilliseconds))
                {
                    m_Logger.Warn("CDB process did not exit within timeout, killing process");
                    KillProcess();
                }
            }
            catch (Exception ex)
            {
                m_Logger.Warn(ex, "Error waiting for CDB process to exit");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Kills the CDB process.
    /// </summary>
    protected virtual void KillProcess()
    {
        m_CdbProcess?.Kill();
    }

    /// <summary>
    /// Disposes all resources.
    /// </summary>
    protected virtual void DisposeResources()
    {
        m_InputWriter?.Dispose();
        m_OutputAggregator?.Dispose();
        m_CdbProcess?.Dispose();
    }

    /// <summary>
    /// Sets the disposed state.
    /// </summary>
    protected virtual void SetDisposedState()
    {
        m_Disposed = true;
        m_Initialized = false;
    }

    /// <summary>
    /// Sets the initialized state flag. Intended for use by test accessors.
    /// </summary>
    /// <param name="initialized">The initialized state to set.</param>
    protected void SetInitializedForTesting(bool initialized)
    {
        m_Initialized = initialized;
    }

    /// <summary>
    /// Disposes of the CDB session and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        try
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error during synchronous disposal of CDB session");
        }
    }

    /// <summary>
    /// Finds the CDB executable path by checking configured path and common installation locations.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the CDB executable path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CDB executable cannot be found.</exception>
    public Task<string> FindCdbExecutableAsync()
    {
        // If configured path exists, use it
        if (!string.IsNullOrEmpty(m_Settings.Get().McpNexus.Debugging.CdbPath) &&
            m_FileSystem.FileExists(m_Settings.Get().McpNexus.Debugging.CdbPath ?? string.Empty))
        {
            return Task.FromResult(m_Settings.Get().McpNexus.Debugging.CdbPath)!;
        }

        // Try common CDB locations
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
            @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe",
            @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
            @"C:\Program Files\Windows Kits\10\Debuggers\x86\cdb.exe",
        };

        foreach (var path in commonPaths)
        {
            if (m_FileSystem.FileExists(path))
            {
                m_Logger.Debug("Found CDB at: {CdbPath}", path);
                return Task.FromResult(path);
            }
        }

        throw new InvalidOperationException("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    /// <summary>
    /// Starts the CDB process asynchronously using the initialized dump file path and symbol path.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized with a dump file.</exception>
    public async Task StartCdbProcessAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        if (string.IsNullOrEmpty(DumpFilePath))
        {
            throw new InvalidOperationException("Session not initialized with dump file");
        }

        var cdbPath = await FindCdbExecutableAsync();
        var arguments = BuildCommandLineArguments();

        await StartCdbProcessAsync(cdbPath, arguments);
        return;
    }

    /// <summary>
    /// Generates a session-specific CDB log file path based on the current log configuration and service mode.
    /// </summary>
    /// <param name="sessionId">The unique session identifier for the CDB log file.</param>
    /// <returns>The full path to the session-specific CDB log file.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the NLog file target is not found, the log directory is not set, or the session ID is not provided.
    /// </exception>
    private string GetCdbSessionBasedLogPath(string sessionId)
    {
        var fileTarget = LogManager.Configuration?.FindTargetByName("mainFile") as NLog.Targets.FileTarget ?? throw new InvalidOperationException("File target not found in NLog configuration");
        var logEventInfo = new LogEventInfo(NLog.LogLevel.Info, string.Empty, string.Empty);
        var originalPath = fileTarget.FileName.Render(logEventInfo);

        var directory = Path.GetDirectoryName(originalPath);
        _ = Path.GetFileNameWithoutExtension(originalPath);

        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("Directory is not set");
        }

        if (string.IsNullOrEmpty(sessionId))
        {
            throw new InvalidOperationException("Session ID is not set");
        }

        string sessionsDirectory;
        if (m_Settings.Get().McpNexus.Transport.ServiceMode)
        {
            // Service mode: C:\ProgramData\MCP-Nexus\Sessions\
            sessionsDirectory = Path.Combine(Path.GetDirectoryName(directory)!, "Sessions");
        }
        else
        {
            // Other modes: C:\ProgramData\MCP-Nexus\Logs\Sessions\
            sessionsDirectory = Path.Combine(directory, "Sessions");
        }

        // Ensure the Sessions directory exists
        _ = Directory.CreateDirectory(sessionsDirectory);

        var retentionDays = m_Settings.Get().Logging.RetentionDays;
        if (retentionDays > 0)
        {
            CleanupOldCdbLogs(sessionsDirectory, retentionDays);
        }

        var newFileNameWithoutExtension = $"cdb_{sessionId}";
        var newFileName = Path.ChangeExtension(newFileNameWithoutExtension, ".log");
        var newPath = Path.Combine(sessionsDirectory, newFileName);

        return newPath;
    }

    /// <summary>
    /// Cleans up CDB log files older than the configured retention period in the specified Sessions directory.
    /// </summary>
    /// <param name="sessionsDirectory">The directory containing the CDB log files.</param>
    /// <param name="retentionDays">The retention period in days.</param>
    protected void CleanupOldCdbLogs(string sessionsDirectory, int retentionDays)
    {
        if (retentionDays <= 0)
        {
            return;
        }

        try
        {
            var directoryInfo = m_FileSystem.GetDirectoryInfo(sessionsDirectory);
            var files = directoryInfo.GetFiles("cdb_*.log*");

            if (files.Length == 0)
            {
                return;
            }

            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var deletedCount = 0;

            foreach (var file in files)
            {
                if (file.CreationTime >= cutoff)
                {
                    continue;
                }

                try
                {
                    m_FileSystem.DeleteFile(file.FullName);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    m_Logger.Warn(ex, "Failed to delete old CDB log file {CdbLogFile}", file.FullName);
                }
            }

            if (deletedCount > 0)
            {
                m_Logger.Info(
                    "Deleted {DeletedCount} CDB log files older than {RetentionDays} days in {SessionsDirectory}",
                    deletedCount,
                    retentionDays,
                    sessionsDirectory);
            }
        }
        catch (Exception ex)
        {
            m_Logger.Warn(ex, "Error while cleaning up old CDB log files in {SessionsDirectory}", sessionsDirectory);
        }
    }

    /// <summary>
    /// Attempts to compress the current session's CDB log file using GZip in a best-effort manner.
    /// </summary>
    /// <returns>A task that represents the asynchronous compression operation.</returns>
    private async Task CompressCdbLogIfAvailableAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SessionId))
            {
                return;
            }

            var cdbLogFilePath = GetCdbSessionBasedLogPath(SessionId);

            if (!m_FileSystem.FileExists(cdbLogFilePath))
            {
                return;
            }

            var compressedPath = $"{cdbLogFilePath}.gz";

            if (m_FileSystem.FileExists(compressedPath))
            {
                return;
            }

            await m_FileSystem.CompressToGZipAsync(cdbLogFilePath, compressedPath).ConfigureAwait(false);

            m_FileSystem.DeleteFile(cdbLogFilePath);

            m_Logger.Info("Compressed CDB log for session {SessionId} to {CompressedPath}", SessionId, compressedPath);
        }
        catch (Exception ex)
        {
            m_Logger.Info(ex, "Failed to compress CDB log for session {SessionId}. Keeping uncompressed log file.", SessionId);
        }
    }

    /// <summary>
    /// Builds the command line arguments for starting the CDB process.
    /// </summary>
    /// <returns>The formatted command line arguments string.</returns>
    private string BuildCommandLineArguments()
    {
        var arguments = new List<string>
        {
            "-z", // Analyze dump file
            $"\"{DumpFilePath}\"",
        };

        if (!string.IsNullOrWhiteSpace(SymbolPath))
        {
            arguments.Add("-y");
            arguments.Add($"\"{SymbolPath}\"");
        }

        arguments.Add("-lines");

        // Add CDB logging
        var cdbLogFilePath = GetCdbSessionBasedLogPath(SessionId);
        arguments.Add("-logau");
        arguments.Add(cdbLogFilePath);

        return string.Join(" ", arguments);
    }

    /// <summary>
    /// Starts the CDB process with the specified arguments.
    /// </summary>
    /// <param name="cdbPath">Path to the CDB executable.</param>
    /// <param name="arguments">Command line arguments for CDB.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task StartCdbProcessAsync(string cdbPath, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = cdbPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        m_CdbProcess = m_ProcessManager.StartProcess(startInfo);

        if (m_CdbProcess == null)
        {
            throw new InvalidOperationException("Failed to start CDB process");
        }

        m_InputWriter = m_CdbProcess.StandardInput;

        // Attach event-based aggregator to merge stdout and stderr into a single stream
        m_OutputAggregator = new ProcessOutputAggregator();
        m_OutputAggregator.Attach(m_CdbProcess);

        m_Logger.Debug("CDB process started with PID: {ProcessId}", m_CdbProcess.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Waits for the CDB process to initialize.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous wait operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CDB process exits during initialization.</exception>
    protected async Task WaitForCdbInitializationAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMilliseconds(m_Settings.Get().McpNexus.Debugging.StartupDelayMs);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            if (IsProcessExited())
            {
                throw new InvalidOperationException("CDB process exited during initialization");
            }

            await WaitForInitializationDelay(cancellationToken);
        }

        if (IsProcessExited())
        {
            throw new InvalidOperationException("CDB process exited during initialization");
        }
    }

    /// <summary>
    /// Checks if the CDB process has exited.
    /// </summary>
    /// <returns>True if the process has exited, false otherwise.</returns>
    protected virtual bool IsProcessExited()
    {
        return m_CdbProcess?.HasExited == true;
    }

    /// <summary>
    /// Waits for the specified delay during initialization.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual async Task WaitForInitializationDelay(CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
    }

    /// <summary>
    /// Creates a CDB command wrapped with start and end sentinel markers.
    /// </summary>
    /// <param name="command">The CDB command to wrap.</param>
    /// <returns>The command string with sentinels.</returns>
    protected static string CreateCommandWithSentinels(string command)
    {
        return $".echo {CdbSentinels.StartMarker}; {command}; .echo {CdbSentinels.EndMarker}";
    }

    /// <summary>
    /// Sends a command to the CDB process input stream.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CDB input stream is not available.</exception>
    protected async Task SendCommandToCdbAsync(string command)
    {
        if (m_InputWriter == null)
        {
            throw new InvalidOperationException("CDB input stream is not available");
        }

        await m_InputWriter.WriteLineAsync(command);
        await m_InputWriter.FlushAsync();
    }

    /// <summary>
    /// Reads the command output from the CDB process, extracting text between sentinel markers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command output as a string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CDB output stream is not available.</exception>
    /// <exception cref="TimeoutException">Thrown when command execution exceeds the configured timeout.</exception>
    protected async Task<string> ReadCommandOutputAsync(CancellationToken cancellationToken)
    {
        if (m_OutputAggregator == null)
        {
            throw new InvalidOperationException("CDB output stream is not available");
        }

        var output = new StringBuilder();
        var startMarkerFound = false;
        var timeout = m_Settings.Get().McpNexus.AutomatedRecovery.GetDefaultCommandTimeout();

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var reader = m_OutputAggregator.Reader;
            while (!combinedCts.Token.IsCancellationRequested)
            {
                var next = await reader.ReadAsync(combinedCts.Token);

                var (shouldContinue, shouldBreak) = ProcessOutputLine(next.Text, ref startMarkerFound, output);
                if (shouldBreak)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            // Include partial output and attempt to re-sync to the next end marker to avoid misalignment
            var partial = output.ToString().TrimEnd();
            await DrainUntilEndMarkerAsync(TimeSpan.FromMilliseconds(750), cancellationToken);
            throw new TimeoutException($"Command execution timed out after {timeout.TotalMinutes:F1} minutes{Environment.NewLine}{partial}");
        }

        return output.ToString().TrimEnd();
    }

    /// <summary>
    /// Reads a line from the CDB output stream.
    /// </summary>
    /// <returns>The line read from the output stream, or null if no more lines.</returns>
    protected virtual async Task<string?> ReadLineFromOutputAsync()
    {
        if (m_OutputAggregator == null)
        {
            return null;
        }

        var next = await m_OutputAggregator.Reader.ReadAsync(CancellationToken.None);
        return next.Text;
    }

    /// <summary>
    /// Drains the merged output stream until the end sentinel is observed or the maximum duration elapses.
    /// This helps re-synchronize the stream after a timeout.
    /// </summary>
    /// <param name="maxDuration">Maximum time to drain.</param>
    /// <param name="cancellationToken">External cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task DrainUntilEndMarkerAsync(TimeSpan maxDuration, CancellationToken cancellationToken)
    {
        if (m_OutputAggregator == null)
        {
            return;
        }

        using var drainCts = new CancellationTokenSource(maxDuration);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, drainCts.Token);

        try
        {
            var reader = m_OutputAggregator.Reader;
            while (!linked.Token.IsCancellationRequested)
            {
                var next = await reader.ReadAsync(linked.Token);
                if (next.Text.Contains(CdbSentinels.EndMarker))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Best-effort drain; safe to ignore
        }
    }

    /// <summary>
    /// Processes a single line of output from CDB.
    /// </summary>
    /// <param name="line">The line to process.</param>
    /// <param name="startMarkerFound">Reference to the start marker found flag.</param>
    /// <param name="output">The output string builder to append to.</param>
    /// <returns>A tuple indicating whether to continue processing and whether to break the loop.</returns>
    protected virtual (bool ShouldContinue, bool ShouldBreak) ProcessOutputLine(string line, ref bool startMarkerFound, StringBuilder output)
    {
        if (line.Contains(CdbSentinels.StartMarker))
        {
            startMarkerFound = true;
            return (true, false); // Continue, don't break
        }

        if (startMarkerFound)
        {
            if (line.Contains(CdbSentinels.EndMarker))
            {
                return (false, true); // Don't continue, break
            }

            _ = output.AppendLine(line);
        }

        return (true, false); // Continue, don't break
    }

    /// <summary>
    /// Throws an exception if the session has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the session is disposed.</exception>
    protected void ThrowIfDisposed()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(CdbSession));
        }
    }

    /// <summary>
    /// Throws an exception if the session is not initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    protected void ThrowIfNotInitialized()
    {
        if (!m_Initialized)
        {
            throw new InvalidOperationException("CDB session is not initialized");
        }
    }

    /// <summary>
    /// Executes a batch of commands in the CDB session by joining them with semicolons.
    /// </summary>
    /// <param name="commands">The commands to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the combined command output.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    /// <exception cref="ArgumentNullException">Thrown when commands is null.</exception>
    /// <exception cref="ArgumentException">Thrown when commands list is empty.</exception>
    public async Task<string> ExecuteBatchCommandAsync(IEnumerable<string> commands, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        if (commands == null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        var commandList = commands.ToList();
        if (commandList.Count == 0)
        {
            throw new ArgumentException("Commands list cannot be empty", nameof(commands));
        }

        m_Logger.Debug("Executing batch of {CommandCount} CDB commands", commandList.Count);

        try
        {
            var batchCommand = string.Join("; ", commandList);
            return await ExecuteCommandAsync(batchCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error executing batch command");
            throw;
        }
    }

    /// <summary>
    /// Stops the CDB process forcefully by killing it.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    public void StopCdbProcess()
    {
        ThrowIfDisposed();

        lock (m_ProcessLock)
        {
            if (m_CdbProcess != null && !m_CdbProcess.HasExited)
            {
                try
                {
                    m_Logger.Debug("Stopping CDB process with PID: {ProcessId}", m_CdbProcess.Id);
                    m_ProcessManager.KillProcess(m_CdbProcess);
                }
                catch (Exception ex)
                {
                    m_Logger.Warn(ex, "Error stopping CDB process");
                }
            }
        }
    }
}
