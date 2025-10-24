using System.Runtime.Versioning;

using Microsoft.Win32;

namespace Nexus.External.Apis.Registry;

/// <summary>
/// Concrete implementation of IRegistryService that uses the real Windows Registry.
/// </summary>
[SupportedOSPlatform("windows")]
public class RegistryService : IRegistryService
{
    /// <summary>
    /// Reads a string value from the registry.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="keyPath">The registry key path.</param>
    /// <param name="valueName">The value name to read.</param>
    /// <returns>The string value, or null if not found.</returns>
    public string? ReadString(RegistryHive hive, string keyPath, string valueName)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var key = baseKey.OpenSubKey(keyPath);
        return key?.GetValue(valueName) as string;
    }

    /// <summary>
    /// Reads an integer value from the registry.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="keyPath">The registry key path.</param>
    /// <param name="valueName">The value name to read.</param>
    /// <returns>The integer value, or null if not found.</returns>
    public int? ReadInt32(RegistryHive hive, string keyPath, string valueName)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var key = baseKey.OpenSubKey(keyPath);
        var value = key?.GetValue(valueName);

        return value switch
        {
            int intValue => intValue,
            _ => null
        };
    }

    /// <summary>
    /// Checks if a registry key exists.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="keyPath">The registry key path to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    public bool KeyExists(RegistryHive hive, string keyPath)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var key = baseKey.OpenSubKey(keyPath);
        return key != null;
    }
}

