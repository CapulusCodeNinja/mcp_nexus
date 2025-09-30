using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages file and directory operations for service deployment
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class FileOperationsManager
    {
        /// <summary>
        /// Copies application files from build output to installation directory
        /// </summary>
        /// <param name="logger">Optional logger for copy operations</param>
        /// <returns>True if copy was successful, false otherwise</returns>
        public static async Task<bool> CopyApplicationFilesAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Copying application files to installation directory");

                // Find the project directory
                var projectDir = ProjectBuilder.FindProjectDirectory(Environment.CurrentDirectory);
                if (string.IsNullOrEmpty(projectDir))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Could not find project directory");
                    return false;
                }

                // Determine source directory (build output)
                var sourceDir = Path.Combine(projectDir, "bin", ServiceConfiguration.BuildConfiguration, "net8.0");
                if (!Directory.Exists(sourceDir))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Build output directory not found: {SourceDir}", sourceDir);
                    return false;
                }

                OperationLogger.LogDebug(logger, OperationLogger.Operations.Install, "Copying from: {SourceDir}", sourceDir);
                OperationLogger.LogDebug(logger, OperationLogger.Operations.Install, "Copying to: {DestDir}", ServiceConfiguration.InstallFolder);

                // Copy all files
                await CopyDirectoryAsync(sourceDir, ServiceConfiguration.InstallFolder, logger);

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Application files copied successfully");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, ex, "Exception during file copy");
                return false;
            }
        }

        /// <summary>
        /// Recursively copies a directory and all its contents
        /// </summary>
        /// <param name="sourceDir">Source directory path</param>
        /// <param name="destDir">Destination directory path</param>
        /// <param name="logger">Optional logger for copy operations</param>
        public static async Task CopyDirectoryAsync(string sourceDir, string destDir, ILogger? logger = null)
        {
            // Create destination directory if it doesn't exist
            Directory.CreateDirectory(destDir);

            // Copy all files in the current directory
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);

                try
                {
                    File.Copy(file, destFile, overwrite: true);
                    OperationLogger.LogTrace(logger, OperationLogger.Operations.Install, "Copied: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, "Failed to copy file {FileName}: {Error}", fileName, ex.Message);
                }
            }

            // Recursively copy subdirectories (excluding backup folders to prevent infinite recursion)
            var subDirs = Directory.GetDirectories(sourceDir)
                .Where(dir => !Path.GetFileName(dir).Equals("backups", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var subDir in subDirs)
            {
                var dirName = Path.GetFileName(subDir);
                var destSubDir = Path.Combine(destDir, dirName);
                await CopyDirectoryAsync(subDir, destSubDir, logger);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Validates that all required installation files are present
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if all required files exist, false otherwise</returns>
        public static bool ValidateInstallationFiles(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Validation, "Validating installation files");

                var executablePath = Path.Combine(ServiceConfiguration.InstallFolder, ServiceConfiguration.ExecutableName);

                if (!File.Exists(executablePath))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Validation, "Required executable not found: {ExecutablePath}", executablePath);
                    return false;
                }

                // Check for essential .NET runtime files
                var requiredFiles = new[]
                {
                    "mcp_nexus.dll",
                    "mcp_nexus.runtimeconfig.json",
                    "mcp_nexus.deps.json"
                };

                foreach (var requiredFile in requiredFiles)
                {
                    var filePath = Path.Combine(ServiceConfiguration.InstallFolder, requiredFile);
                    if (!File.Exists(filePath))
                    {
                        OperationLogger.LogError(logger, OperationLogger.Operations.Validation, "Required file not found: {FilePath}", filePath);
                        return false;
                    }
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Validation, "All required installation files are present");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Validation, ex, "Exception during file validation");
                return false;
            }
        }
    }
}
