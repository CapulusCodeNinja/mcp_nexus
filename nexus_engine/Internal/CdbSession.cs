using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

namespace Nexus.Engine.Internal;

using Nexus.Config;

using NLog;

/// <summary>
/// Internal CDB session that manages a single CDB process and command execution.
/// </summary>
internal class CdbSession : ICdbSession
{
    private readonly Logger m_Logger;

    private readonly IFileSystem m_FileSystem;
    private readonly IProcessManager m_ProcessManager;
    private readonly SemaphoreSlim m_ExecutionSemaphore = new(1, 1);
    private readonly object m_ProcessLock = new();

    private Process? m_CdbProcess;
    private StreamWriter? m_InputWriter;
    private StreamReader? m_OutputReader;
    private StreamReader? m_ErrorReader;
    private volatile bool m_Disposed = false;
    private volatile bool m_Initialized = false;

    /// <summary>
    /// Gets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive => m_Initialized && !m_Disposed && m_CdbProcess?.HasExited == false;

    /// <summary>
    /// Gets the dump file path.
    /// </summary>
    public string DumpFilePath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the symbol path.
    /// </summary>
    public string? SymbolPath => SymbolPath1;

    /// <summary>
    /// Gets a value indicating whether the session is initialized.
    /// </summary>
    public bool IsInitialized => m_Initialized;

    public string? SymbolPath1
    {
        get;
        set;
    }

    public CdbSession(
        IFileSystem fileSystem,
        IProcessManager processManager)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
    }

    /// <summary>
    /// Initializes the CDB session with a dump file.
    /// </summary>
    /// <param name="dumpFilePath">The path to the dump file.</param>
    /// <param name="symbolPath">Optional symbol path.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InitializeAsync(string dumpFilePath, string? symbolPath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(dumpFilePath))
        {
            throw new ArgumentException("Dump file path cannot be null or empty", nameof(dumpFilePath));
        }

        if (!m_FileSystem.FileExists(dumpFilePath))
        {
            throw new FileNotFoundException($"Dump file not found: {dumpFilePath}", dumpFilePath);
        }

        // Store the paths
        DumpFilePath = dumpFilePath;
        SymbolPath1 = symbolPath;

        m_Logger.Info("Initializing CDB session for dump file: {DumpFilePath}", dumpFilePath);

        try
        {
            await m_ExecutionSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Find CDB executable
                var cdbPath = await FindCdbExecutableAsync();

                // Build command line arguments
                var arguments = BuildCommandLineArguments(dumpFilePath, symbolPath);

                // Start CDB process
                await StartCdbProcessAsync(cdbPath, arguments);

                // Wait for CDB to initialize
                await WaitForCdbInitializationAsync(cancellationToken);

                m_Initialized = true;
                m_Logger.Info("CDB session initialized successfully");
            }
            finally
            {
                m_ExecutionSemaphore.Release();
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

        try
        {
            await m_ExecutionSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Create command with sentinels
                var wrappedCommand = CreateCommandWithSentinels(command);

                // Send command to CDB
                await SendCommandToCdbAsync(wrappedCommand);

                // Read output until completion
                var output = await ReadCommandOutputAsync(cancellationToken);

                m_Logger.Debug("CDB command completed with {OutputLength} characters of output", output.Length);
                return output;
            }
            finally
            {
                m_ExecutionSemaphore.Release();
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
                if (!m_CdbProcess.WaitForExit((int)Settings.GetInstance().Get().McpNexus.SessionManagement.GetCleanupInterval().TotalMilliseconds))
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
        m_OutputReader?.Dispose();
        m_ErrorReader?.Dispose();
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

    public Task<string> FindCdbExecutableAsync()
    {
        // If configured path exists, use it
        if (!string.IsNullOrEmpty(Settings.GetInstance().Get().McpNexus.Debugging.CdbPath) &&
            m_FileSystem.FileExists(Settings.GetInstance().Get().McpNexus.Debugging.CdbPath ?? string.Empty))
        {
            return Task.FromResult(Settings.GetInstance().Get().McpNexus.Debugging.CdbPath)!;
        }

        // Try common CDB locations
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
            @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe",
            @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
            @"C:\Program Files\Windows Kits\10\Debuggers\x86\cdb.exe"
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

    public Task StartCdbProcessAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        if (string.IsNullOrEmpty(DumpFilePath))
        {
            throw new InvalidOperationException("Session not initialized with dump file");
        }

        var cdbPath = FindCdbExecutableAsync().Result;
        var arguments = BuildCommandLineArguments(DumpFilePath, SymbolPath1);

        return StartCdbProcessAsync(cdbPath, arguments);
    }

    /// <summary>
    /// Builds the command line arguments for starting the CDB process.
    /// </summary>
    /// <param name="dumpFilePath">Path to the dump file.</param>
    /// <param name="symbolPath">Optional symbol path.</param>
    /// <returns>The formatted command line arguments string.</returns>
    private static string BuildCommandLineArguments(string dumpFilePath, string? symbolPath)
    {
        var arguments = new List<string>
        {
            "-z", // Analyze dump file
            $"\"{dumpFilePath}\""
        };

        if (!string.IsNullOrWhiteSpace(symbolPath))
        {
            arguments.Add("-y");
            arguments.Add($"\"{symbolPath}\"");
        }

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
            WindowStyle = ProcessWindowStyle.Hidden
        };

        m_CdbProcess = m_ProcessManager.StartProcess(startInfo);

        if (m_CdbProcess == null)
        {
            throw new InvalidOperationException("Failed to start CDB process");
        }

        m_InputWriter = m_CdbProcess.StandardInput;
        m_OutputReader = m_CdbProcess.StandardOutput;
        m_ErrorReader = m_CdbProcess.StandardError;

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
        var timeout = TimeSpan.FromMilliseconds(5000);
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
        if (m_OutputReader == null)
        {
            throw new InvalidOperationException("CDB output stream is not available");
        }

        var output = new StringBuilder();
        var startMarkerFound = false;
        var timeout = Settings.GetInstance().Get().McpNexus.AutomatedRecovery.GetDefaultCommandTimeout();

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!combinedCts.Token.IsCancellationRequested)
            {
                var line = await ReadLineFromOutputAsync();
                if (line == null)
                {
                    break;
                }

                var (shouldContinue, shouldBreak) = ProcessOutputLine(line, ref startMarkerFound, output);
                if (shouldBreak)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Command execution timed out after {timeout.TotalMinutes:F1} minutes");
        }

        return output.ToString().TrimEnd();
    }

    /// <summary>
    /// Reads a line from the CDB output stream.
    /// </summary>
    /// <returns>The line read from the output stream, or null if no more lines.</returns>
    protected virtual async Task<string?> ReadLineFromOutputAsync()
    {
        return await m_OutputReader!.ReadLineAsync();
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

            output.AppendLine(line);
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

    public Task<string> ExecuteBatchCommandAsync(IEnumerable<string> commands, CancellationToken cancellationToken = default)
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
            return m_ExecutionSemaphore.WaitAsync(cancellationToken).ContinueWith(async _ =>
            {
                try
                {
                    var batchCommand = string.Join("; ", commandList);
                    return await ExecuteCommandAsync(batchCommand, cancellationToken);
                }
                finally
                {
                    m_ExecutionSemaphore.Release();
                }
            }, cancellationToken).Unwrap();
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error executing batch command");
            throw;
        }
    }

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
