using System.Threading;

namespace mcp_nexus.Infrastructure
{
    public static class ThreadPoolTuning
    {
        public static void Apply()
        {
            ThreadPool.GetMinThreads(out var currWorker, out var currIo);
            var target = Math.Max(Environment.ProcessorCount * 2, currWorker);
            var targetIo = Math.Max(Environment.ProcessorCount * 2, currIo);
            ThreadPool.SetMinThreads(target, targetIo);
        }
    }
}


