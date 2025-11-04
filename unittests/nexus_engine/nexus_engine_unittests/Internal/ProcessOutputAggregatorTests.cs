using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;

using Nexus.Engine.Internal;

using Xunit;

namespace Nexus.Engine.Tests.Internal;

/// <summary>
/// Tests for <see cref="ProcessOutputAggregator"/>.
/// </summary>
public sealed class ProcessOutputAggregatorTests
{
    /// <summary>
    /// Tests that constructor initializes the aggregator correctly.
    /// </summary>
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        using var aggregator = new ProcessOutputAggregator();

        // Assert
        Assert.NotNull(aggregator);
        Assert.NotNull(aggregator.Reader);
    }

    /// <summary>
    /// Tests that Reader property returns a valid ChannelReader.
    /// </summary>
    [Fact]
    public void Reader_ReturnsValidChannelReader()
    {
        // Arrange
        using var aggregator = new ProcessOutputAggregator();

        // Act
        var reader = aggregator.Reader;

        // Assert
        Assert.NotNull(reader);
        _ = Assert.IsAssignableFrom<ChannelReader<ProcessOutputLine>>(reader);
    }

    /// <summary>
    /// Tests that Attach throws ObjectDisposedException when disposed.
    /// </summary>
    [Fact]
    public void Attach_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var aggregator = new ProcessOutputAggregator();
        aggregator.Dispose();

        using var process = CreateTestProcess();

        // Act & Assert
        var ex = Assert.Throws<ObjectDisposedException>(() => aggregator.Attach(process));
        Assert.Equal(nameof(ProcessOutputAggregator), ex.ObjectName);
    }

    /// <summary>
    /// Tests that Attach throws ArgumentNullException when process is null.
    /// </summary>
    [Fact]
    public void Attach_WithNullProcess_ThrowsArgumentNullException()
    {
        // Arrange
        using var aggregator = new ProcessOutputAggregator();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => aggregator.Attach(null!));
        Assert.Equal("process", ex.ParamName);
    }

    /// <summary>
    /// Tests that Attach successfully attaches to a valid started process.
    /// </summary>
    [Fact]
    public void Attach_WithValidStartedProcess_AttachesSuccessfully()
    {
        // Arrange
        using var aggregator = new ProcessOutputAggregator();
        using var process = CreateEchoProcess("test");
        _ = process.Start();

        // Act - should not throw
        aggregator.Attach(process);

        // Assert
        Assert.True(true);

        // Cleanup
        if (!process.HasExited)
        {
            process.Kill();
        }
    }

    /// <summary>
    /// Tests that output data from a real process is captured correctly.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RealProcess_CapturesOutput_Successfully()
    {
        // Arrange
        using var aggregator = new ProcessOutputAggregator();
        using var process = CreateEchoProcess("Test output line");

        // Act
        _ = process.Start();
        aggregator.Attach(process);

        // Wait for process to complete
        await process.WaitForExitAsync(CancellationToken.None);

        // Give channel time to process
        await Task.Delay(100);

        // Assert
        var lines = new List<ProcessOutputLine>();
        while (aggregator.Reader.TryRead(out var line))
        {
            lines.Add(line);
        }

        Assert.NotEmpty(lines);
        Assert.Contains(lines, l => l.Text.Contains("Test output line"));
        Assert.All(lines, line => Assert.NotNull(line.Text));
    }

    /// <summary>
    /// Tests that Dispose can be called multiple times safely.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var aggregator = new ProcessOutputAggregator();
        using var process = CreateEchoProcess("test");
        _ = process.Start();
        aggregator.Attach(process);

        // Act & Assert - should not throw
        aggregator.Dispose();
        aggregator.Dispose();
        aggregator.Dispose();

        // Cleanup
        if (!process.HasExited)
        {
            process.Kill();
        }
    }

    /// <summary>
    /// Tests that Dispose completes the channel.
    /// </summary>
    [Fact]
    public void Dispose_CompletesChannel()
    {
        // Arrange
        var aggregator = new ProcessOutputAggregator();
        using var process = CreateEchoProcess("test");
        _ = process.Start();
        aggregator.Attach(process);

        // Act
        aggregator.Dispose();

        // Assert
        Assert.True(aggregator.Reader.Completion.IsCompleted);

        // Cleanup
        if (!process.HasExited)
        {
            process.Kill();
        }
    }

    /// <summary>
    /// Tests that Dispose without attaching to process works correctly.
    /// </summary>
    [Fact]
    public void Dispose_WithoutAttach_WorksCorrectly()
    {
        // Arrange
        var aggregator = new ProcessOutputAggregator();

        // Act & Assert - should not throw
        aggregator.Dispose();
        Assert.True(aggregator.Reader.Completion.IsCompleted);
    }

    /// <summary>
    /// Tests that output from multiple processes can be captured independently.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task MultipleProcesses_CaptureOutputIndependently()
    {
        // Arrange
        using var aggregator1 = new ProcessOutputAggregator();
        using var aggregator2 = new ProcessOutputAggregator();
        using var process1 = CreateEchoProcess("Process 1");
        using var process2 = CreateEchoProcess("Process 2");

        // Act
        _ = process1.Start();
        aggregator1.Attach(process1);

        _ = process2.Start();
        aggregator2.Attach(process2);

        await process1.WaitForExitAsync(CancellationToken.None);
        await process2.WaitForExitAsync(CancellationToken.None);

        await Task.Delay(100);

        // Assert
        var lines1 = new List<ProcessOutputLine>();
        while (aggregator1.Reader.TryRead(out var line))
        {
            lines1.Add(line);
        }

        var lines2 = new List<ProcessOutputLine>();
        while (aggregator2.Reader.TryRead(out var line))
        {
            lines2.Add(line);
        }

        Assert.NotEmpty(lines1);
        Assert.NotEmpty(lines2);
        Assert.Contains(lines1, l => l.Text.Contains("Process 1"));
        Assert.Contains(lines2, l => l.Text.Contains("Process 2"));
    }

    /// <summary>
    /// Tests that channel reader works asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ChannelReader_SupportsAsyncIteration()
    {
        // Arrange
        using var aggregator = new ProcessOutputAggregator();
        using var process = CreateEchoProcess("Async test");

        // Act
        _ = process.Start();
        aggregator.Attach(process);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var lines = new List<ProcessOutputLine>();

        // Read asynchronously
        var readTask = Task.Run(
            async () =>
            {
                await foreach (var line in aggregator.Reader.ReadAllAsync(cts.Token))
                {
                    lines.Add(line);
                    if (process.HasExited)
                    {
                        break;
                    }
                }
            },
            cts.Token);

        await process.WaitForExitAsync(CancellationToken.None);
        aggregator.Dispose(); // Complete the channel

        // Wait for the read task to complete
        try
        {
            await readTask;
        }
        catch (OperationCanceledException)
        {
            // Expected if the cancellation token fires
        }

        // Assert
        Assert.NotEmpty(lines);
    }

    /// <summary>
    /// Tests that disposed aggregator doesn't accept new attachments.
    /// </summary>
    [Fact]
    public void DisposedAggregator_RejectsNewAttachments()
    {
        // Arrange
        var aggregator = new ProcessOutputAggregator();
        aggregator.Dispose();

        using var process1 = CreateTestProcess();
        using var process2 = CreateTestProcess();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => aggregator.Attach(process1));
        _ = Assert.Throws<ObjectDisposedException>(() => aggregator.Attach(process2));
    }

    /// <summary>
    /// Creates a test process instance for testing.
    /// </summary>
    /// <returns>A configured Process instance.</returns>
    private static Process CreateTestProcess()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GetShellCommand(),
                Arguments = GetEchoArguments("test"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };

        return process;
    }

    /// <summary>
    /// Creates a process that echoes a specific message.
    /// </summary>
    /// <param name="message">The message to echo.</param>
    /// <returns>A configured Process instance.</returns>
    private static Process CreateEchoProcess(string message)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GetShellCommand(),
                Arguments = GetEchoArguments(message),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };

        return process;
    }

    /// <summary>
    /// Gets the appropriate shell command for the current platform.
    /// </summary>
    /// <returns>The shell command path.</returns>
    private static string GetShellCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/sh";
    }

    /// <summary>
    /// Gets the appropriate echo command arguments for the current platform.
    /// </summary>
    /// <param name="message">The message to echo.</param>
    /// <returns>The command arguments.</returns>
    private static string GetEchoArguments(string message)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"/c echo {message}"
            : $"-c \"echo {message}\"";
    }
}
