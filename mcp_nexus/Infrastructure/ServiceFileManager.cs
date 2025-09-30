using System.Diagnostics;
using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages file operations for service installation, including building, copying, and backup operations
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ServiceFileManager
    {
        /// <summary>
        /// Builds the project in Release mode for deployment
        /// </summary>
        /// <param name="logger">Optional logger for build operations</param>
        /// <returns>True if build was successful, false otherwise</returns>
        public static async Task<bool> BuildProjectForDeploymentAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Build, "Building project in Release mode for deployment");

                // Find the project directory
                var projectDir = FindProjectDirectory(Environment.CurrentDirectory);
                if (string.IsNullOrEmpty(projectDir))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Build, "Could not find project directory containing mcp_nexus.csproj");
                    return false;
                }

                OperationLogger.LogDebug(logger, OperationLogger.Operations.Build, "Found project directory: {ProjectDir}", projectDir);

                // Build the project
                var buildArgs = "build --configuration Release --no-restore";
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = buildArgs,
                    WorkingDirectory = projectDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Build, "Failed to start dotnet build process");
                    return false;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Build, "Project built successfully");
                    OperationLogger.LogDebug(logger, OperationLogger.Operations.Build, "Build output: {Output}", output.Trim());
                    return true;
                }
                else
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Build, 
                        "Build failed with exit code {ExitCode}. Output: {Output}. Error: {Error}", 
                        process.ExitCode, output.Trim(), error.Trim());
                    return false;
                }
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Build, ex, "Exception during project build: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Finds the project directory containing the .csproj file
        /// </summary>
        /// <param name="startPath">The starting path to search from</param>
        /// <returns>The project directory path, or null if not found</returns>
        public static string? FindProjectDirectory(string startPath)
        {
            var currentDir = new DirectoryInfo(startPath);
            
            while (currentDir != null)
            {
                if (currentDir.GetFiles("mcp_nexus.csproj").Length > 0)
                {
                    return currentDir.FullName;
                }
                currentDir = currentDir.Parent;
            }
            
            return null;
        }

        /// <summary>
        /// Copies application files from the build output to the installation directory
        /// </summary>
        /// <param name="logger">Optional logger for copy operations</param>
        /// <returns>Task representing the copy operation</returns>
        public static async Task CopyApplicationFilesAsync(ILogger? logger = null)
        {
            try
            {
                var projectDir = FindProjectDirectory(Environment.CurrentDirectory);
                if (string.IsNullOrEmpty(projectDir))
                {
                    throw new InvalidOperationException("Could not find project directory");
                }

                var sourceDir = Path.Combine(projectDir, "bin", "Release", "net8.0");
                var targetDir = ServiceConfiguration.InstallFolder;

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Copy, 
                    "Copying files from {SourceDir} to {TargetDir}", sourceDir, targetDir);

                if (!Directory.Exists(sourceDir))
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
                }

                await CopyDirectoryAsync(sourceDir, targetDir, logger);
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Copy, "Application files copied successfully");
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Copy, ex, "Error copying application files: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Recursively copies a directory and all its contents
        /// </summary>
        /// <param name="sourceDir">Source directory path</param>
        /// <param name="targetDir">Target directory path</param>
        /// <param name="logger">Optional logger for copy operations</param>
        /// <returns>Task representing the copy operation</returns>
        public static async Task CopyDirectoryAsync(string sourceDir, string targetDir, ILogger? logger = null)
        {
            try
            {
                // Create target directory if it doesn't exist
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Copy all files
                var files = Directory.GetFiles(sourceDir);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var targetFile = Path.Combine(targetDir, fileName);
                    
                    File.Copy(file, targetFile, true);
                    OperationLogger.LogDebug(logger, OperationLogger.Operations.Copy, "Copied file: {FileName}", fileName);
                }

                // Copy all subdirectories
                var directories = Directory.GetDirectories(sourceDir);
                foreach (var directory in directories)
                {
                    var dirName = Path.GetFileName(directory);
                    var targetSubDir = Path.Combine(targetDir, dirName);
                    
                    await CopyDirectoryAsync(directory, targetSubDir, logger);
                }
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Copy, ex, 
                    "Error copying directory from {SourceDir} to {TargetDir}: {Error}", sourceDir, targetDir, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates a backup of the current installation
        /// </summary>
        /// <param name="logger">Optional logger for backup operations</param>
        /// <returns>The backup directory path if successful, null otherwise</returns>
        public static async Task<string?> CreateBackupAsync(ILogger? logger = null)
        {
            try
            {
                if (!Directory.Exists(ServiceConfiguration.InstallFolder))
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "No existing installation to backup");
                    return null;
                }

                var backupFolder = ServiceConfiguration.GetTimestampedBackupFolder();
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Creating backup: {BackupFolder}", backupFolder);

                // Create backups directory if it doesn't exist
                if (!Directory.Exists(ServiceConfiguration.BackupsFolder))
                {
                    Directory.CreateDirectory(ServiceConfiguration.BackupsFolder);
                }

                // Create the specific backup folder
                Directory.CreateDirectory(backupFolder);

                // Copy all files except the backups folder itself
                var sourceFiles = Directory.GetFiles(ServiceConfiguration.InstallFolder, "*", SearchOption.TopDirectoryOnly);
                foreach (var sourceFile in sourceFiles)
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var targetFile = Path.Combine(backupFolder, fileName);
                    File.Copy(sourceFile, targetFile, true);
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Backup created successfully: {BackupFolder}", backupFolder);
                return backupFolder;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Update, ex, "Error creating backup: {Error}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Cleans up old backup folders, keeping only the most recent ones
        /// </summary>
        /// <param name="maxBackupsToKeep">Maximum number of backups to retain</param>
        /// <param name="logger">Optional logger for cleanup operations</param>
        /// <returns>Task representing the cleanup operation</returns>
        public static async Task CleanupOldBackupsAsync(int maxBackupsToKeep = 5, ILogger? logger = null)
        {
            try
            {
                if (!Directory.Exists(ServiceConfiguration.BackupsFolder))
                    return;

                var backupDirs = Directory.GetDirectories(ServiceConfiguration.BackupsFolder)
                    .Select(d => new DirectoryInfo(d))
                    .OrderByDescending(d => d.CreationTime)
                    .ToList();

                if (backupDirs.Count <= maxBackupsToKeep)
                    return;

                var dirsToDelete = backupDirs.Skip(maxBackupsToKeep);
                foreach (var dir in dirsToDelete)
                {
                    try
                    {
                        dir.Delete(true);
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Cleanup, "Deleted old backup: {BackupDir}", dir.Name);
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Cleanup, 
                            "Could not delete backup directory {BackupDir}: {Error}", dir.Name, ex.Message);
                    }
                }

                // Method is async for consistency with other file operations
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Cleanup, ex, "Error during backup cleanup: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Validates that all required files exist in the installation directory
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if all required files exist, false otherwise</returns>
        public static bool ValidateInstallationFiles(ILogger? logger = null)
        {
            try
            {
                var requiredFiles = new[]
                {
                    ServiceConfiguration.ExecutableName,
                    "mcp_nexus.dll",
                    "mcp_nexus.runtimeconfig.json"
                };

                var missingFiles = new List<string>();

                foreach (var file in requiredFiles)
                {
                    var filePath = Path.Combine(ServiceConfiguration.InstallFolder, file);
                    if (!File.Exists(filePath))
                    {
                        missingFiles.Add(file);
                    }
                }

                if (missingFiles.Any())
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, 
                        "Missing required files: {MissingFiles}", string.Join(", ", missingFiles));
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "All required installation files are present");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, ex, 
                    "Error validating installation files: {Error}", ex.Message);
                return false;
            }
        }
    }
}
