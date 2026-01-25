using System.Runtime.Versioning;

using Microsoft.Win32;

namespace WinAiDbg.External.Apis.Registry;

/// <summary>
/// Interface for Windows Registry operations to enable mocking in tests.
/// </summary>
[SupportedOSPlatform("windows")]
public interface IRegistryService
{
    /// <summary>
    /// Reads a string value from the registry.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="keyPath">The registry key path.</param>
    /// <param name="valueName">The value name to read.</param>
    /// <returns>The string value, or null if not found.</returns>
    string? ReadString(RegistryHive hive, string keyPath, string valueName);

    /// <summary>
    /// Reads an integer value from the registry.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="keyPath">The registry key path.</param>
    /// <param name="valueName">The value name to read.</param>
    /// <returns>The integer value, or null if not found.</returns>
    int? ReadInt32(RegistryHive hive, string keyPath, string valueName);

    /// <summary>
    /// Checks if a registry key exists.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="keyPath">The registry key path to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    bool KeyExists(RegistryHive hive, string keyPath);
}
