using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Provides Win32 API access to the Service Control Manager
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class Win32ServiceManager
    {
        private const uint SC_MANAGER_ALL_ACCESS = 0xF003F;

        /// <summary>
        /// Opens a handle to the Service Control Manager
        /// </summary>
        /// <returns>Handle to the Service Control Manager, or IntPtr.Zero if failed</returns>
        public static IntPtr OpenServiceControlManager()
        {
            try
            {
                return OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenSCManager(
            string? lpMachineName,
            string? lpDatabaseName,
            uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        /// <summary>
        /// Disposable wrapper for Service Control Manager handle
        /// </summary>
        public class ServiceControlManagerHandle : IDisposable
        {
            private IntPtr m_handle;
            private bool m_disposed;

            internal ServiceControlManagerHandle(IntPtr handle)
            {
                m_handle = handle;
            }

            public static implicit operator IntPtr(ServiceControlManagerHandle handle)
            {
                return handle.m_handle;
            }

            public void Dispose()
            {
                if (!m_disposed && m_handle != IntPtr.Zero)
                {
                    CloseServiceHandle(m_handle);
                    m_handle = IntPtr.Zero;
                    m_disposed = true;
                }
            }
        }
    }
}
