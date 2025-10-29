using System.Diagnostics;
using System.Threading.Channels;

namespace Nexus.Engine.Internal;

/// <summary>
/// Aggregates standard output and error streams from a running process
/// into a single ordered stream of lines for consumption.
/// </summary>
internal sealed class ProcessOutputAggregator : IDisposable
{
    private readonly Channel<ProcessOutputLine> m_Channel;
    private Process? m_Process;
    private volatile bool m_Disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessOutputAggregator"/> class.
    /// </summary>
    public ProcessOutputAggregator()
    {
        // Unbounded channel is acceptable here because the producer is the process IO callbacks
        // and the consumer is the command reader loop which applies backpressure through awaits.
        m_Channel = Channel.CreateUnbounded<ProcessOutputLine>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
    }

    /// <summary>
    /// Gets the channel reader that provides merged stdout/stderr lines.
    /// </summary>
    public ChannelReader<ProcessOutputLine> Reader => m_Channel.Reader;

    /// <summary>
    /// Attaches to the specified process and begins asynchronous reading
    /// from both standard output and standard error streams.
    /// </summary>
    /// <param name="process">The process to attach.</param>
    public void Attach(Process process)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(ProcessOutputAggregator));
        }

        m_Process = process ?? throw new ArgumentNullException(nameof(process));

        m_Process.OutputDataReceived += OnOutputDataReceived;
        m_Process.ErrorDataReceived += OnErrorDataReceived;

        m_Process.BeginOutputReadLine();
        m_Process.BeginErrorReadLine();
    }

    /// <summary>
    /// Releases resources and detaches from the process streams.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        try
        {
            if (m_Process != null)
            {
                m_Process.OutputDataReceived -= OnOutputDataReceived;
                m_Process.ErrorDataReceived -= OnErrorDataReceived;
            }
        }
        finally
        {
            _ = m_Channel.Writer.TryComplete();
            m_Disposed = true;
        }
    }

    /// <summary>
    /// Handles data received on the standard output stream.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event args containing the data line.</param>
    private void OnOutputDataReceived(object? sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _ = m_Channel.Writer.TryWrite(new ProcessOutputLine(e.Data, false));
        }
    }

    /// <summary>
    /// Handles data received on the standard error stream.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event args containing the data line.</param>
    private void OnErrorDataReceived(object? sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _ = m_Channel.Writer.TryWrite(new ProcessOutputLine(e.Data, true));
        }
    }
}
