using System.Threading;

namespace mcp_nexus.Infrastructure.Core
{
    /// <summary>
    /// Provides thread pool tuning functionality for optimal performance.
    /// Configures the .NET ThreadPool with appropriate minimum thread counts based on the system's processor count.
    /// </summary>
    public static class ThreadPoolTuning
    {
        /// <summary>
        /// Applies thread pool tuning by setting optimal minimum thread counts.
        /// Sets the minimum number of worker and I/O threads to at least 2 times the processor count.
        /// </summary>
        public static void Apply()
        {
            ThreadPool.GetMinThreads(out var currWorker, out var currIo);
            var target = Math.Max(Environment.ProcessorCount * 2, currWorker);
            var targetIo = Math.Max(Environment.ProcessorCount * 2, currIo);
            ThreadPool.SetMinThreads(target, targetIo);
        }
    }
}


