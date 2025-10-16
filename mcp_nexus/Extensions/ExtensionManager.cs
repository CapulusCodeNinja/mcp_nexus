using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Manages discovery, loading, and validation of extension scripts.
    /// </summary>
    public class ExtensionManager : IExtensionManager
    {
        private readonly ILogger<ExtensionManager> m_Logger;
        private readonly string m_ExtensionsPath;
        private readonly Dictionary<string, ExtensionMetadata> m_Extensions = [];
        private readonly object m_Lock = new();
        private readonly FileSystemWatcher? m_Watcher;
        private volatile bool m_ReloadPending = false;
        private int m_Version = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionManager"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording extension operations.</param>
        /// <param name="extensionsPath">The path to the extensions directory.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        /// <exception cref="ArgumentException">Thrown when extensionsPath is null or empty.</exception>
        public ExtensionManager(ILogger<ExtensionManager> logger, string extensionsPath)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(extensionsPath))
                throw new ArgumentException("Extensions path cannot be null or empty", nameof(extensionsPath));

            m_ExtensionsPath = extensionsPath;

            try
            {
                if (Directory.Exists(m_ExtensionsPath))
                {
                    m_Watcher = new FileSystemWatcher(m_ExtensionsPath)
                    {
                        IncludeSubdirectories = true,
                        Filter = "*.*",
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size
                    };
                    m_Watcher.Changed += OnExtensionsChanged;
                    m_Watcher.Created += OnExtensionsChanged;
                    m_Watcher.Deleted += OnExtensionsChanged;
                    m_Watcher.Renamed += OnExtensionsChanged;
                    m_Watcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Failed to initialize extensions FileSystemWatcher");
            }
        }

        /// <summary>
        /// Discovers and loads all available extensions from the extensions directory.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task LoadExtensionsAsync()
        {
            m_Logger.LogInformation("Loading extensions from: {ExtensionsPath}", m_ExtensionsPath);

            if (!Directory.Exists(m_ExtensionsPath))
            {
                m_Logger.LogWarning("Extensions directory does not exist: {ExtensionsPath}", m_ExtensionsPath);
                Directory.CreateDirectory(m_ExtensionsPath);
                m_Logger.LogInformation("Created extensions directory: {ExtensionsPath}", m_ExtensionsPath);
                return;
            }

            var metadataFiles = Directory.GetFiles(m_ExtensionsPath, "*.json", SearchOption.AllDirectories);
            m_Logger.LogInformation("Found {Count} metadata files", metadataFiles.Length);

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
                            m_Logger.LogWarning("Duplicate extension name '{Name}' encountered; keeping first", meta.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Failed to load extension from: {MetadataFile}", metadataFile);
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

            Interlocked.Increment(ref m_Version);
            m_Logger.LogInformation("Loaded {Count} extensions successfully", m_Extensions.Count);
        }

        /// <summary>
        /// Loads a single extension from its metadata file.
        /// </summary>
        /// <param name="metadataFile">The path to the metadata JSON file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task<ExtensionMetadata?> LoadExtensionMetadataAsync(string metadataFile)
        {
            m_Logger.LogDebug("Loading extension metadata from: {MetadataFile}", metadataFile);

            var json = await File.ReadAllTextAsync(metadataFile);
            var metadata = JsonSerializer.Deserialize<ExtensionMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (metadata == null)
            {
                m_Logger.LogWarning("Failed to deserialize metadata from: {MetadataFile}", metadataFile);
                return null;
            }

            if (string.IsNullOrWhiteSpace(metadata.Name))
            {
                m_Logger.LogWarning("Extension metadata missing name: {MetadataFile}", metadataFile);
                return null;
            }

            if (string.IsNullOrWhiteSpace(metadata.ScriptFile))
            {
                m_Logger.LogWarning("Extension {Name} missing script file reference", metadata.Name);
                return null;
            }

            // Set extension path (directory containing the metadata file)
            metadata.ExtensionPath = Path.GetDirectoryName(metadataFile) ?? m_ExtensionsPath;

            // Validate script file exists
            if (!File.Exists(metadata.FullScriptPath))
            {
                m_Logger.LogWarning("Extension {Name} script file not found: {ScriptPath}",
                    metadata.Name, metadata.FullScriptPath);
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
                return null;

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
        public int GetExtensionsVersion() => Volatile.Read(ref m_Version);

        private void OnExtensionsChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce: if a reload is already pending, ignore
            if (m_ReloadPending)
                return;
            m_ReloadPending = true;
            _ = Task.Run(async () =>
            {
                try
                {
                    m_Logger.LogInformation("🔁 Detected extensions change: {Change} on {Path}. Reloading...", e.ChangeType, e.FullPath);
                    await LoadExtensionsAsync();
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error reloading extensions after change");
                }
                finally
                {
                    m_ReloadPending = false;
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
                return false;

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
        public (bool isValid, string? errorMessage) ValidateExtension(string extensionName)
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

            if (!File.Exists(metadata.FullScriptPath))
            {
                return (false, $"Extension '{extensionName}' script file not found: {metadata.FullScriptPath}");
            }

            if (string.IsNullOrWhiteSpace(metadata.ScriptType))
            {
                return (false, $"Extension '{extensionName}' has no script type specified");
            }

            var supportedScriptTypes = new[] { "powershell" };
            if (!supportedScriptTypes.Contains(metadata.ScriptType.ToLowerInvariant()))
            {
                return (false, $"Extension '{extensionName}' has unsupported script type: {metadata.ScriptType}");
            }

            return (true, null);
        }
    }
}

