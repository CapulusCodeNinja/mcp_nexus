using System.Runtime.InteropServices;

using FluentAssertions;

using Nexus.Engine.Share.WindowsDebugging;
using Nexus.External.Apis.FileSystem;

using Xunit;

namespace Nexus.Engine.Share.Tests;

/// <summary>
/// Unit tests for <see cref="WindowsDebuggerToolLocator"/>.
/// </summary>
public sealed class WindowsDebuggerToolLocatorTests
{
    /// <summary>
    /// Verifies that the locator prefers the OS-matching debugger architecture folder when multiple are present.
    /// </summary>
    [Fact]
    public void FindToolExecutablePath_WhenArm64AndArm64Exists_PrefersArm64()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "McpNexusDebuggerLocatorTests", Guid.NewGuid().ToString("N"));
        var kitsBase = Path.Combine(tempRoot, "Windows Kits");
        var debuggersRoot = Path.Combine(kitsBase, "10", "Debuggers");
        var arm64Dir = Path.Combine(debuggersRoot, "arm64");
        var x86Dir = Path.Combine(debuggersRoot, "x86");

        _ = Directory.CreateDirectory(arm64Dir);
        _ = Directory.CreateDirectory(x86Dir);

        var arm64Tool = Path.Combine(arm64Dir, "cdb.exe");
        var x86Tool = Path.Combine(x86Dir, "cdb.exe");

        File.WriteAllText(arm64Tool, "arm64");
        File.WriteAllText(x86Tool, "x86");

        try
        {
            var fileSystem = new FileSystem();
            var locator = new WindowsDebuggerToolLocator(fileSystem, new[] { kitsBase });

            var resolved = locator.FindToolExecutablePath("cdb.exe", null, Architecture.Arm64);

            _ = resolved.Should().Be(arm64Tool);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    /// <summary>
    /// Verifies that the locator falls back to x64 on ARM64 when arm64 tools are not available.
    /// </summary>
    [Fact]
    public void FindToolExecutablePath_WhenArm64AndOnlyX64Exists_FallsBackToX64()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "McpNexusDebuggerLocatorTests", Guid.NewGuid().ToString("N"));
        var kitsBase = Path.Combine(tempRoot, "Windows Kits");
        var debuggersRoot = Path.Combine(kitsBase, "10", "Debuggers");
        var x64Dir = Path.Combine(debuggersRoot, "x64");
        var x86Dir = Path.Combine(debuggersRoot, "x86");

        _ = Directory.CreateDirectory(x64Dir);
        _ = Directory.CreateDirectory(x86Dir);

        var x64Tool = Path.Combine(x64Dir, "dumpchk.exe");
        var x86Tool = Path.Combine(x86Dir, "dumpchk.exe");

        File.WriteAllText(x64Tool, "x64");
        File.WriteAllText(x86Tool, "x86");

        try
        {
            var fileSystem = new FileSystem();
            var locator = new WindowsDebuggerToolLocator(fileSystem, new[] { kitsBase });

            var resolved = locator.FindToolExecutablePath("dumpchk.exe", null, Architecture.Arm64);

            _ = resolved.Should().Be(x64Tool);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    /// <summary>
    /// Verifies that the configured path is always used when it exists.
    /// </summary>
    [Fact]
    public void FindToolExecutablePath_ConfiguredPathExists_ReturnsConfiguredPath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "McpNexusDebuggerLocatorTests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(tempRoot);
        var configured = Path.Combine(tempRoot, "custom-cdb.exe");
        File.WriteAllText(configured, "custom");

        try
        {
            var fileSystem = new FileSystem();
            var locator = new WindowsDebuggerToolLocator(fileSystem);

            var resolved = locator.FindToolExecutablePath("cdb.exe", configured, Architecture.Arm64);

            _ = resolved.Should().Be(configured);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
}

