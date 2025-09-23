using System.Collections.Concurrent;

namespace mcp_nexus.Services
{
    public record Job(
        string Id,
        string Command,
        DateTime StartTime,
        Task<string> Task,
        CancellationTokenSource Cts,
        JobStatus Status = JobStatus.Running,
        string? Result = null,
        string? Error = null,
        DateTime? EndTime = null
    );

    public enum JobStatus
    {
        Running,
        Completed,
        Cancelled,
        Failed
    }

    public class BackgroundJobService : IDisposable
    {
        private readonly ConcurrentDictionary<string, Job> m_jobs = new();
        private readonly ILogger<BackgroundJobService> m_logger;

        // FIX: Prevent multiple cleanup tasks with a single cleanup timer
        private readonly Timer m_cleanupTimer;
        private readonly object m_cleanupLock = new();

        public BackgroundJobService(ILogger<BackgroundJobService> logger)
        {
            m_logger = logger;

            // FIX: Use a single timer for cleanup instead of spawning tasks for each job
            m_cleanupTimer = new Timer(CleanupOldJobsCallback, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public string StartJob(string command, Func<CancellationToken, Task<string>> jobFunction)
        {
            var jobId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource();
            var startTime = DateTime.UtcNow;

            m_logger.LogInformation("Starting background job {JobId} for command: {Command}", jobId, command);

            var jobTask = Task.Run(async () =>
            {
                try
                {
                    var result = await jobFunction(cts.Token);
                    UpdateJobStatus(jobId, JobStatus.Completed, result: result);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    m_logger.LogWarning("Background job {JobId} for command '{Command}' was cancelled.", jobId, command);
                    UpdateJobStatus(jobId, JobStatus.Cancelled, error: "Job cancelled.");
                    return "Job cancelled.";
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Background job {JobId} for command '{Command}' failed.", jobId, command);
                    UpdateJobStatus(jobId, JobStatus.Failed, error: ex.Message);
                    return $"Job failed: {ex.Message}";
                }
            }, cts.Token);

            var job = new Job(jobId, command, startTime, jobTask, cts);
            m_jobs[jobId] = job;

            // FIX: Cleanup is now handled by the timer, no need to spawn individual tasks

            return jobId;
        }

        public Job? GetJob(string jobId)
        {
            return m_jobs.GetValueOrDefault(jobId);
        }

        public IEnumerable<Job> GetAllJobs()
        {
            return m_jobs.Values;
        }

        public void CancelJob(string jobId)
        {
            if (m_jobs.TryGetValue(jobId, out var job))
            {
                m_logger.LogInformation("Attempting to cancel background job {JobId} for command: {Command}", jobId, job.Command);
                job.Cts.Cancel();
                UpdateJobStatus(jobId, JobStatus.Cancelled, error: "Cancelled by user.");
            }
            else
            {
                m_logger.LogWarning("Attempted to cancel non-existent job: {JobId}", jobId);
            }
        }

        private void UpdateJobStatus(string jobId, JobStatus status, string? result = null, string? error = null)
        {
            // FIX: Thread-safe job status update
            m_jobs.AddOrUpdate(jobId,
                id => throw new InvalidOperationException($"Job {id} not found during update."),
                (_, existingJob) => existingJob with
                {
                    Status = status,
                    Result = result ?? existingJob.Result,
                    Error = error ?? existingJob.Error,
                    EndTime = DateTime.UtcNow
                });
        }

        private void CleanupOldJobsCallback(object? state)
        {
            // FIX: Prevent concurrent cleanup operations
            if (!Monitor.TryEnter(m_cleanupLock))
            {
                m_logger.LogDebug("Cleanup already in progress, skipping");
                return;
            }

            try
            {
                CleanupOldJobs();
            }
            finally
            {
                Monitor.Exit(m_cleanupLock);
            }
        }

        private void CleanupOldJobs()
        {
            var cutoff = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));
            var jobsToRemove = m_jobs.Where(kvp => kvp.Value.EndTime.HasValue && kvp.Value.EndTime.Value < cutoff).ToList();

            foreach (var jobEntry in jobsToRemove)
            {
                if (m_jobs.TryRemove(jobEntry.Key, out var removedJob))
                {
                    m_logger.LogDebug("Cleaned up old background job: {JobId}", removedJob.Id);
                    removedJob.Cts.Dispose();
                }
            }
        }

        public void Dispose()
        {
            m_cleanupTimer.Dispose();

            // Dispose all remaining cancellation tokens
            foreach (var job in m_jobs.Values)
            {
                job.Cts.Dispose();
            }
            m_jobs.Clear();
        }
    }
}
