using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace WinAiDbg.External.Apis.Native;

/// <summary>
/// Safe handle wrapper for a Windows Job Object handle.
/// </summary>
internal sealed class JobObjectHandle : SafeHandleMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobObjectHandle" /> class.
    /// </summary>
    public JobObjectHandle()
        : base(true)
    {
    }

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    /// <param name="hObject">The handle to close.</param>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Releases the underlying handle by calling <c>CloseHandle</c>.
    /// </summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    protected override bool ReleaseHandle()
    {
        return CloseHandle(handle);
    }
}
