namespace mcp_nexus.Models
{
    /// <summary>
    /// Helper class to get version information
    /// </summary>
    internal static class VersionHelper
    {
        internal static string GetFileVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.FileVersion ?? "1.0.0.0";
        }
    }
}
