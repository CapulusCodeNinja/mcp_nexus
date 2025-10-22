using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using mcp_nexus.Engine.Configuration;
using mcp_nexus.Utilities.FileSystem;
using mcp_nexus.Utilities.ProcessManagement;

namespace mcp_nexus.Engine.Internal;

/// <summary>
/// Internal CDB session that manages a single CDB process and command execution.
/// </summary>
internal class CdbSession : ICdbSession
{
    private readonly ILogger<CdbSession> m_Logger;
    private readonly DebugEngineConfiguration m_Configuration;
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
    private string m_DumpFilePath = string.Empty;
    private string? m_SymbolPath;

    /// <summary>
    /// Gets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive => m_Initialized && !m_Disposed && m_CdbProcess?.HasExited == false;

    /// <summary>
    /// Gets the dump file path.
    /// </summary>
    public string DumpFilePath => m_DumpFilePath;

    /// <summary>
    /// Gets the symbol path.
    /// </summary>
    public string? SymbolPath => m_SymbolPath;

    /// <summary>
    /// Gets a value indicating whether the session is initialized.
    /// </summary>
    public bool IsInitialized => m_Initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdbSession"/> class.
    /// </summary>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fileSystem">The file system interface.</param>
    /// <param name="processManager">The process manager interface.</param>
    public CdbSession(
        DebugEngineConfiguration configuration, 
        ILogger<CdbSession> logger,
        IFileSystem fileSystem,
        IProcessManager processManager)
    {
        m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            throw new ArgumentException("Dump file path cannot be null or empty", nameof(dumpFilePath));

        if (!m_FileSystem.FileExists(dumpFilePath))
            throw new FileNotFoundException($"Dump file not found: {dumpFilePath}", dumpFilePath);

        // Store the paths
        m_DumpFilePath = dumpFilePath;
        m_SymbolPath = symbolPath;

        m_Logger.LogInformation("Initializing CDB session for dump file: {DumpFilePath}", dumpFilePath);

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
                m_Logger.LogInformation("CDB session initialized successfully");
            }
            finally
            {
                m_ExecutionSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to initialize CDB session");
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
            throw new ArgumentException("Command cannot be null or empty", nameof(command));

        m_Logger.LogDebug("Executing CDB command: {Command}", command);

        try
        {
            await m_ExecutionSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                // Create command with sentinels
                var wrappedCommand = CreateCommandWithSentinels(command);
                
                // Send command to CDB
                await SendCommandToCdbAsync(wrappedCommand, cancellationToken);
                
                // Read output until completion
                var output = await ReadCommandOutputAsync(cancellationToken);
                
                m_Logger.LogDebug("CDB command completed with {OutputLength} characters of output", output.Length);
                return output;
            }
            finally
            {
                m_ExecutionSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            m_Logger.LogDebug("CDB command cancelled");
            throw;
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error executing CDB command: {Command}", command);
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
            return;

        m_Logger.LogDebug("Disposing CDB session");

        try
        {
            // Close input stream to signal CDB to exit
            if (m_InputWriter != null)
            {
                try
                {
                    await m_InputWriter.WriteLineAsync("q");
                    await m_InputWriter.FlushAsync();
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Error sending quit command to CDB");
                }
            }

            // Wait for process to exit
            if (m_CdbProcess != null && !m_CdbProcess.HasExited)
            {
                try
                {
                    if (!m_CdbProcess.WaitForExit((int)m_Configuration.SessionCleanupTimeout.TotalMilliseconds))
                    {
                        m_Logger.LogWarning("CDB process did not exit within timeout, killing process");
                        m_CdbProcess.Kill();
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Error waiting for CDB process to exit");
                }
            }

            // Dispose streams
            m_InputWriter?.Dispose();
            m_OutputReader?.Dispose();
            m_ErrorReader?.Dispose();

            // Dispose process
            m_CdbProcess?.Dispose();
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error disposing CDB session");
        }
        finally
        {
            m_Disposed = true;
            m_Initialized = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (m_Disposed)
            return;

        try
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error during synchronous disposal of CDB session");
        }
    }

    public Task<string> FindCdbExecutableAsync()
    {
        // If configured path exists, use it
        if (!string.IsNullOrEmpty(m_Configuration.CdbPath) && m_FileSystem.FileExists(m_Configuration.CdbPath))
        {
            return Task.FromResult(m_Configuration.CdbPath);
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
                m_Logger.LogDebug("Found CDB at: {CdbPath}", path);
                return Task.FromResult(path);
            }
        }

        throw new InvalidOperationException("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    public Task StartCdbProcessAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        if (string.IsNullOrEmpty(m_DumpFilePath))
            throw new InvalidOperationException("Session not initialized with dump file");

        var cdbPath = FindCdbExecutableAsync().Result;
        var arguments = BuildCommandLineArguments(m_DumpFilePath, m_SymbolPath);
        
        return StartCdbProcessAsync(cdbPath, arguments);
    }

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

        m_Logger.LogDebug("CDB process started with PID: {ProcessId}", m_CdbProcess.Id);
        return Task.CompletedTask;
    }

    private async Task WaitForCdbInitializationAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMilliseconds(5000);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            if (m_CdbProcess?.HasExited == true)
            {
                throw new InvalidOperationException("CDB process exited during initialization");
            }

            await Task.Delay(100, cancellationToken);
        }

        if (m_CdbProcess?.HasExited == true)
        {
            throw new InvalidOperationException("CDB process exited during initialization");
        }
    }

    private static string CreateCommandWithSentinels(string command)
    {
        return $".echo {CdbSentinels.StartMarker}; {command}; .echo {CdbSentinels.EndMarker}";
    }

    private async Task SendCommandToCdbAsync(string command, CancellationToken cancellationToken)
    {
        if (m_InputWriter == null)
            throw new InvalidOperationException("CDB input stream is not available");

        await m_InputWriter.WriteLineAsync(command);
        await m_InputWriter.FlushAsync();
    }

    private async Task<string> ReadCommandOutputAsync(CancellationToken cancellationToken)
    {
        if (m_OutputReader == null)
            throw new InvalidOperationException("CDB output stream is not available");

        var output = new StringBuilder();
        var startMarkerFound = false;
        var timeout = m_Configuration.DefaultCommandTimeout;

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!combinedCts.Token.IsCancellationRequested)
            {
                var line = await m_OutputReader.ReadLineAsync();
                if (line == null)
                    break;

                if (line.Contains(CdbSentinels.StartMarker))
                {
                    startMarkerFound = true;
                    continue;
                }

                if (startMarkerFound)
                {
                    if (line.Contains(CdbSentinels.EndMarker))
                        break;

                    output.AppendLine(line);
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Command execution timed out after {timeout.TotalMinutes:F1} minutes");
        }

        return output.ToString().TrimEnd();
    }

    private void ThrowIfDisposed()
    {
        if (m_Disposed)
            throw new ObjectDisposedException(nameof(CdbSession));
    }

    private void ThrowIfNotInitialized()
    {
        if (!m_Initialized)
            throw new InvalidOperationException("CDB session is not initialized");
    }

    public Task<string> ExecuteBatchCommandAsync(IEnumerable<string> commands, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        if (commands == null)
            throw new ArgumentNullException(nameof(commands));

        var commandList = commands.ToList();
        if (commandList.Count == 0)
            throw new ArgumentException("Commands list cannot be empty", nameof(commands));

        m_Logger.LogDebug("Executing batch of {CommandCount} CDB commands", commandList.Count);

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
            m_Logger.LogError(ex, "Error executing batch command");
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
                    m_Logger.LogDebug("Stopping CDB process with PID: {ProcessId}", m_CdbProcess.Id);
                    m_ProcessManager.KillProcess(m_CdbProcess);
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Error stopping CDB process");
                }
            }
        }
    }
}
