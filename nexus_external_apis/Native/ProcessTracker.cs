using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Nexus.External.Apis.Native;

/// <summary>
/// Tracks child processes by assigning them to a Windows Job Object so that they are terminated
/// automatically when the host process exits (job handle is closed).
/// </summary>
internal static class ProcessTracker
{
    /// <summary>
    /// Windows Job Object limit flag that terminates all processes in the job when the job handle is closed.
    /// </summary>
    private const uint JobObjectLimitKillOnJobClose = 0x2000;

    /// <summary>
    /// Creates (or opens) a Windows Job Object.
    /// </summary>
    /// <param name="lpJobAttributes">Reserved; must be <see cref="IntPtr.Zero" />.</param>
    /// <param name="lpName">Optional name; <see langword="null" /> for an unnamed job.</param>
    /// <returns>A handle to the job object.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern JobObjectHandle CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    /// <summary>
    /// Sets information on a Windows Job Object.
    /// </summary>
    /// <param name="hJob">The job handle.</param>
    /// <param name="infoType">The information class being set.</param>
    /// <param name="lpJobObjectInfo">Pointer to the info structure.</param>
    /// <param name="cbJobObjectInfoLength">Size (in bytes) of the structure.</param>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(JobObjectHandle hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    /// <summary>
    /// Assigns a process to a Windows Job Object.
    /// </summary>
    /// <param name="hJob">The job handle.</param>
    /// <param name="hProcess">Handle to the process.</param>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(JobObjectHandle hJob, IntPtr hProcess);

    /// <summary>
    /// The shared job object handle used to track processes for the lifetime of the application.
    /// </summary>
    private static readonly JobObjectHandle m_JobHandle;

    /// <summary>
    /// Initializes static members of the <see cref="ProcessTracker"/> class.
    /// Initializes the tracker by creating and configuring a Job Object with the
    /// "kill on job close" limit.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the job object cannot be created or configured.</exception>
    static ProcessTracker()
    {
        m_JobHandle = CreateJobObject(IntPtr.Zero, null);
        if (m_JobHandle.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to create the Windows job object. Win32Error={Marshal.GetLastWin32Error()}.");
        }

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                PerProcessUserTimeLimit = 0,
                PerJobUserTimeLimit = 0,
                LimitFlags = JobObjectLimitKillOnJobClose,
                MinimumWorkingSetSize = UIntPtr.Zero,
                MaximumWorkingSetSize = UIntPtr.Zero,
                ActiveProcessLimit = 0,
                Affinity = UIntPtr.Zero,
                PriorityClass = 0,
                SchedulingClass = 0,
            },
            IoCounters = new IO_COUNTERS
            {
                ReadOperationCount = 0,
                WriteOperationCount = 0,
                OtherOperationCount = 0,
                ReadTransferCount = 0,
                WriteTransferCount = 0,
                OtherTransferCount = 0,
            },
            ProcessMemoryLimit = UIntPtr.Zero,
            JobMemoryLimit = UIntPtr.Zero,
            PeakProcessMemoryUsed = UIntPtr.Zero,
            PeakJobMemoryUsed = UIntPtr.Zero,
        };

        var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        var extendedInfoPtr = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(info, extendedInfoPtr, false);
            if (!SetInformationJobObject(m_JobHandle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
            {
                throw new InvalidOperationException($"Failed to set job object limits. Win32Error={Marshal.GetLastWin32Error()}.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }
    }

    /// <summary>
    /// Adds a process to the tracker by assigning it to the shared Windows Job Object.
    /// </summary>
    /// <param name="process">The process to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="process" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be assigned to the job object.</exception>
    public static void AddProcess(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (process.HasExited)
        {
            throw new InvalidOperationException("Cannot assign a process that has already exited to the job object.");
        }

        try
        {
            var result = AssignProcessToJobObject(m_JobHandle, process.Handle);
            if (!result)
            {
                throw new InvalidOperationException($"Failed to assign process to job object. Win32Error={Marshal.GetLastWin32Error()}.");
            }
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException("Failed to assign process to job object because the process handle is no longer valid (the process may have already exited).", ex);
        }
    }

    /// <summary>
    /// Identifies the type of job object information being set or queried.
    /// </summary>
    private enum JobObjectInfoType
    {
        /// <summary>
        /// Extended limit information.
        /// </summary>
        ExtendedLimitInformation = 9,
    }

    /// <summary>
    /// Basic limit information for a job object.
    /// </summary>
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        /// <summary>
        /// Per-process user-mode execution time limit, in 100-nanosecond ticks.
        /// </summary>
        public long PerProcessUserTimeLimit;

        /// <summary>
        /// Per-job user-mode execution time limit, in 100-nanosecond ticks.
        /// </summary>
        public long PerJobUserTimeLimit;

        /// <summary>
        /// Flags that control the limits in effect for the job.
        /// </summary>
        public uint LimitFlags;

        /// <summary>
        /// Minimum working set size, in bytes.
        /// </summary>
        public UIntPtr MinimumWorkingSetSize;

        /// <summary>
        /// Maximum working set size, in bytes.
        /// </summary>
        public UIntPtr MaximumWorkingSetSize;

        /// <summary>
        /// Maximum number of active processes for the job.
        /// </summary>
        public uint ActiveProcessLimit;

        /// <summary>
        /// Processor affinity mask.
        /// </summary>
        public UIntPtr Affinity;

        /// <summary>
        /// Process priority class.
        /// </summary>
        public uint PriorityClass;

        /// <summary>
        /// Scheduling class.
        /// </summary>
        public uint SchedulingClass;
    }

    /// <summary>
    /// I/O accounting counters for a job object.
    /// </summary>
    private struct IO_COUNTERS
    {
        /// <summary>
        /// Number of read operations performed.
        /// </summary>
        public ulong ReadOperationCount;

        /// <summary>
        /// Number of write operations performed.
        /// </summary>
        public ulong WriteOperationCount;

        /// <summary>
        /// Number of other (non-read/non-write) operations performed.
        /// </summary>
        public ulong OtherOperationCount;

        /// <summary>
        /// Total number of bytes read.
        /// </summary>
        public ulong ReadTransferCount;

        /// <summary>
        /// Total number of bytes written.
        /// </summary>
        public ulong WriteTransferCount;

        /// <summary>
        /// Total number of bytes transferred by other operations.
        /// </summary>
        public ulong OtherTransferCount;
    }

    /// <summary>
    /// Extended limit information for a job object.
    /// </summary>
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        /// <summary>
        /// Basic limit information.
        /// </summary>
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;

        /// <summary>
        /// I/O accounting information.
        /// </summary>
        public IO_COUNTERS IoCounters;

        /// <summary>
        /// Process memory limit, in bytes.
        /// </summary>
        public UIntPtr ProcessMemoryLimit;

        /// <summary>
        /// Job memory limit, in bytes.
        /// </summary>
        public UIntPtr JobMemoryLimit;

        /// <summary>
        /// Peak process memory usage, in bytes.
        /// </summary>
        public UIntPtr PeakProcessMemoryUsed;

        /// <summary>
        /// Peak job memory usage, in bytes.
        /// </summary>
        public UIntPtr PeakJobMemoryUsed;
    }
}
