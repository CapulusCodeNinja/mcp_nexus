using System.Diagnostics;
using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Handles building .NET projects for deployment
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ProjectBuilder
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
                    OperationLogger.LogDebug(logger, OperationLogger.Operations.Build, "Build output: {Output}", output);
                    return true;
                }
                else
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Build, "Build failed with exit code: {ExitCode}", process.ExitCode);
                    OperationLogger.LogError(logger, OperationLogger.Operations.Build, "Build error: {Error}", error);
                    OperationLogger.LogDebug(logger, OperationLogger.Operations.Build, "Build output: {Output}", output);
                    return false;
                }
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Build, ex, "Exception during project build");
                return false;
            }
        }

        /// <summary>
        /// Finds the project directory containing the .csproj file
        /// </summary>
        /// <param name="startDirectory">Directory to start searching from</param>
        /// <returns>Path to project directory, or null if not found</returns>
        public static string? FindProjectDirectory(string startDirectory)
        {
            var currentDir = new DirectoryInfo(startDirectory);

            while (currentDir != null)
            {
                var projectFile = Path.Combine(currentDir.FullName, ServiceConfiguration.ProjectFileName);
                if (File.Exists(projectFile))
                {
                    return currentDir.FullName;
                }
                currentDir = currentDir.Parent;
            }

            return null;
        }
    }
}
