using System.Text.Json;

using Nexus.Config;
using Nexus.Engine.Extensions.Models;
using Nexus.External.Apis.FileSystem;

using NLog;

namespace Nexus.Engine.Extensions.Core;

/// <summary>
/// Manages discovery, loading, and validation of extension scripts.
/// </summary>
internal class Manager : IDisposable
{
    private readonly Logger m_Logger;
    private readonly ISettings m_Settings;
    private readonly IFileSystem m_FileSystem;
    private readonly string m_ExtensionsPath;
    private readonly Dictionary<string, ExtensionMetadata> m_Extensions = new();
    private readonly object m_Lock = new();
    private readonly FileSystemWatcher? m_Watcher;
    private int m_ReloadPending = 0;
    private int m_Version = 0;
    private bool m_Disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Manager"/> class with specified dependencies.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="settings">The product settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when fileSystem is null.</exception>
    /// <exception cref="ArgumentException">Thrown when extensionsPath is null or empty.</exception>
    public Manager(IFileSystem fileSystem, ISettings settings)
    {
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_Logger = LogManager.GetCurrentClassLogger();

        m_Settings = settings;

        m_ExtensionsPath = m_Settings.Get().McpNexus.Extensions.ExtensionsPath;
        if (!Path.IsPathRooted(m_ExtensionsPath))
        {
            m_ExtensionsPath = Path.Combine(AppContext.BaseDirectory, m_ExtensionsPath);
        }

        if (!m_FileSystem.DirectoryExists(m_ExtensionsPath))
        {
            m_FileSystem.CreateDirectory(m_ExtensionsPath);

            if (!m_FileSystem.DirectoryExists(m_ExtensionsPath))
            {
                m_Logger.Warn("Extensions directory does not exist: \"{ExtensionsPath}\" and failed to create it. No extensions will be loaded.", m_ExtensionsPath);
                return;
            }
        }

        try
        {
            m_Watcher = new FileSystemWatcher(m_ExtensionsPath)
            {
                IncludeSubdirectories = true,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
            };
            m_Watcher.Changed += OnExtensionsChanged;
            m_Watcher.Created += OnExtensionsChanged;
            m_Watcher.Deleted += OnExtensionsChanged;
            m_Watcher.Renamed += OnExtensionsChanged;
            m_Watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            m_Logger.Warn(ex, "Failed to initialize extensions FileSystemWatcher");
        }

        // Load extensions at startup
        LoadExtensionsAsync().Wait();
    }

    /// <summary>
    /// Discovers and loads all available extensions from the extensions directory.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LoadExtensionsAsync()
    {
        m_Logger.Info("Loading extensions from: {ExtensionsPath}", m_ExtensionsPath);

        if (!m_FileSystem.DirectoryExists(m_ExtensionsPath))
        {
            m_Logger.Warn("Extensions directory does not exist: {ExtensionsPath}", m_ExtensionsPath);
            m_FileSystem.CreateDirectory(m_ExtensionsPath);
            m_Logger.Info("Created extensions directory: {ExtensionsPath}", m_ExtensionsPath);
            return;
        }

        var metadataFiles = m_FileSystem.GetFiles(m_ExtensionsPath, "*.json", SearchOption.AllDirectories);
        m_Logger.Info("Found {Count} metadata files", metadataFiles.Length);

        // Build a new map to replace atomically
        var newMap = new Dictionary<string, ExtensionMetadata>(StringComparer.OrdinalIgnoreCase);

        foreach (var metadataFile in metadataFiles)
        {
            try
            {
                var meta = await LoadExtensionMetadataAsync(metadataFile);
                if (meta != null)
                {
                    if (!newMap.ContainsKey(meta.Name))
                    {
                        newMap[meta.Name] = meta;
                    }
                    else
                    {
                        m_Logger.Warn("Duplicate extension name '{Name}' encountered; keeping first", meta.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Failed to load extension from: {MetadataFile}", metadataFile);
            }
        }

        lock (m_Lock)
        {
            m_Extensions.Clear();
            foreach (var kv in newMap)
            {
                m_Extensions[kv.Key] = kv.Value;
            }
        }

        _ = Interlocked.Increment(ref m_Version);
        m_Logger.Info("Loaded {Count} extensions successfully", m_Extensions.Count);
    }

    /// <summary>
    /// Loads a single extension from its metadata file.
    /// </summary>
    /// <param name="metadataFile">The path to the metadata JSON file.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task<ExtensionMetadata?> LoadExtensionMetadataAsync(string metadataFile)
    {
        m_Logger.Debug("Loading extension metadata from: {MetadataFile}", metadataFile);

        var json = await File.ReadAllTextAsync(metadataFile);
        var metadata = JsonSerializer.Deserialize<ExtensionMetadata>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (metadata == null)
        {
            m_Logger.Warn("Failed to deserialize metadata from: {MetadataFile}", metadataFile);
            return null;
        }

        if (string.IsNullOrWhiteSpace(metadata.Name))
        {
            m_Logger.Warn("Extension metadata missing name: {MetadataFile}", metadataFile);
            return null;
        }

        if (string.IsNullOrWhiteSpace(metadata.ScriptFile))
        {
            m_Logger.Warn("Extension {Name} missing script file reference", metadata.Name);
            return null;
        }

        // Set extension path (directory containing the metadata file)
        metadata.ExtensionPath = Path.GetDirectoryName(metadataFile) ?? m_ExtensionsPath;

        // Set full script path
        metadata.FullScriptPath = Path.Combine(metadata.ExtensionPath, metadata.ScriptFile);

        // Validate script file exists
        if (!m_FileSystem.FileExists(metadata.FullScriptPath))
        {
            m_Logger.Warn("Extension {Name} script file not found: {ScriptPath}", metadata.Name, metadata.FullScriptPath);
            return null;
        }

        return metadata;
    }

    /// <summary>
    /// Gets metadata for a specific extension by name.
    /// </summary>
    /// <param name="extensionName">The name of the extension to retrieve.</param>
    /// <returns>The extension metadata, or null if not found.</returns>
    public ExtensionMetadata? GetExtension(string extensionName)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            return null;
        }

        lock (m_Lock)
        {
            return m_Extensions.TryGetValue(extensionName, out var metadata) ? metadata : null;
        }
    }

    /// <summary>
    /// Gets all available extensions.
    /// </summary>
    /// <returns>A collection of all extension metadata.</returns>
    public IEnumerable<ExtensionMetadata> GetAllExtensions()
    {
        lock (m_Lock)
        {
            return m_Extensions.Values.ToList();
        }
    }

    /// <summary>
    /// Gets a monotonically increasing version of the extensions set for cache invalidation.
    /// </summary>
    /// <returns>Version number that increments each time extensions are reloaded.</returns>
    public int GetExtensionsVersion()
    {
        return Volatile.Read(ref m_Version);
    }

    /// <summary>
    /// Handles file system changes in the extensions directory.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void OnExtensionsChanged(object sender, FileSystemEventArgs e)
    {
        // Atomic check-and-set to prevent multiple concurrent reloads
        if (Interlocked.CompareExchange(ref m_ReloadPending, 1, 0) == 1)
        {
            // Already reloading, skip this event
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                m_Logger.Info("Detected extensions change: {Change} on {Path}. Reloading...", e.ChangeType, e.FullPath);
                await LoadExtensionsAsync();
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Error reloading extensions after change");
            }
            finally
            {
                // Reset the flag using atomic operation
                _ = Interlocked.Exchange(ref m_ReloadPending, 0);
            }
        });
    }

    /// <summary>
    /// Checks if an extension with the given name exists.
    /// </summary>
    /// <param name="extensionName">The name of the extension to check.</param>
    /// <returns>True if the extension exists, false otherwise.</returns>
    public bool ExtensionExists(string extensionName)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            return false;
        }

        lock (m_Lock)
        {
            return m_Extensions.ContainsKey(extensionName);
        }
    }

    /// <summary>
    /// Validates an extension's metadata and script file.
    /// </summary>
    /// <param name="extensionName">The name of the extension to validate.</param>
    /// <returns>A tuple containing validation result and error message if validation fails.</returns>
    public (bool IsValid, string? ErrorMessage) ValidateExtension(string extensionName)
    {
        var metadata = GetExtension(extensionName);
        if (metadata == null)
        {
            return (false, $"Extension '{extensionName}' not found");
        }

        if (string.IsNullOrWhiteSpace(metadata.ScriptFile))
        {
            return (false, $"Extension '{extensionName}' has no script file specified");
        }

        if (!m_FileSystem.FileExists(metadata.FullScriptPath))
        {
            return (false, $"Extension '{extensionName}' script file not found: {metadata.FullScriptPath}");
        }

        if (string.IsNullOrWhiteSpace(metadata.ScriptType))
        {
            return (false, $"Extension '{extensionName}' has no script type specified");
        }

        var supportedScriptTypes = new[] { "powershell" };
        return !supportedScriptTypes.Contains(metadata.ScriptType.ToLowerInvariant())
            ? (false, $"Extension '{extensionName}' has unsupported script type: {metadata.ScriptType}")
            : (true, (string?)null);
    }

    /// <summary>
    /// Disposes of the extension manager and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        if (m_Watcher != null)
        {
            try
            {
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Changed -= OnExtensionsChanged;
                m_Watcher.Created -= OnExtensionsChanged;
                m_Watcher.Deleted -= OnExtensionsChanged;
                m_Watcher.Renamed -= OnExtensionsChanged;
                m_Watcher.Dispose();
                m_Logger.Debug("FileSystemWatcher disposed successfully");
            }
            catch (Exception ex)
            {
                m_Logger.Warn(ex, "Error disposing FileSystemWatcher");
            }
        }

        GC.SuppressFinalize(this);
    }
}
