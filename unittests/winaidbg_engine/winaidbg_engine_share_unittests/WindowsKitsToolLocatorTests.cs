using System.Runtime.InteropServices;

using FluentAssertions;

using Moq;

using WinAiDbg.Engine.Share.WindowsKits;
using WinAiDbg.External.Apis.FileSystem;

using Xunit;

namespace WinAiDbg.Engine.Share.Unittests;

/// <summary>
/// Unit tests for <see cref="WindowsKitsToolLocator"/>.
/// </summary>
public sealed class WindowsKitsToolLocatorTests
{
    /// <summary>
    /// Verifies that the locator prefers the OS-matching debugger architecture folder when multiple are present.
    /// </summary>
    [Fact]
    public void FindToolExecutablePath_WhenArm64AndArm64Exists_PrefersArm64()
    {
        const string toolFileName = "cdb.exe";
        var kitsBase = @"C:\Mock\Windows Kits";
        var versionDirectory = Path.Combine(kitsBase, "10");
        var debuggersRoot = Path.Combine(versionDirectory, "Debuggers");
        var arm64Tool = Path.Combine(debuggersRoot, "arm64", toolFileName);
        var x86Tool = Path.Combine(debuggersRoot, "x86", toolFileName);

        var fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);
        _ = fileSystemMock
            .Setup(m => m.CombinePaths(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        _ = fileSystemMock
            .Setup(m => m.DirectoryExists(kitsBase))
            .Returns(true);
        _ = fileSystemMock
            .Setup(m => m.GetDirectories(kitsBase))
            .Returns(new[] { versionDirectory });
        _ = fileSystemMock
            .Setup(m => m.DirectoryExists(debuggersRoot))
            .Returns(true);

        _ = fileSystemMock
            .Setup(m => m.FileExists(arm64Tool))
            .Returns(true);
        _ = fileSystemMock
            .Setup(m => m.FileExists(x86Tool))
            .Returns(true);

        var locator = new WindowsKitsToolLocator(fileSystemMock.Object, new[] { kitsBase });

        var resolved = locator.FindToolExecutablePath(toolFileName, null, Architecture.Arm64);

        _ = resolved.Should().Be(arm64Tool);
    }

    /// <summary>
    /// Verifies that the locator falls back to x64 on ARM64 when arm64 tools are not available.
    /// </summary>
    [Fact]
    public void FindToolExecutablePath_WhenArm64AndOnlyX64Exists_FallsBackToX64()
    {
        const string toolFileName = "dumpchk.exe";
        var kitsBase = @"C:\Mock\Windows Kits";
        var versionDirectory = Path.Combine(kitsBase, "10");
        var debuggersRoot = Path.Combine(versionDirectory, "Debuggers");
        var arm64Tool = Path.Combine(debuggersRoot, "arm64", toolFileName);
        var x64Tool = Path.Combine(debuggersRoot, "x64", toolFileName);
        var x86Tool = Path.Combine(debuggersRoot, "x86", toolFileName);

        var fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);
        _ = fileSystemMock
            .Setup(m => m.CombinePaths(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        _ = fileSystemMock
            .Setup(m => m.DirectoryExists(kitsBase))
            .Returns(true);
        _ = fileSystemMock
            .Setup(m => m.GetDirectories(kitsBase))
            .Returns(new[] { versionDirectory });
        _ = fileSystemMock
            .Setup(m => m.DirectoryExists(debuggersRoot))
            .Returns(true);

        _ = fileSystemMock
            .Setup(m => m.FileExists(arm64Tool))
            .Returns(false);
        _ = fileSystemMock
            .Setup(m => m.FileExists(x64Tool))
            .Returns(true);
        _ = fileSystemMock
            .Setup(m => m.FileExists(x86Tool))
            .Returns(true);

        var locator = new WindowsKitsToolLocator(fileSystemMock.Object, new[] { kitsBase });

        var resolved = locator.FindToolExecutablePath(toolFileName, null, Architecture.Arm64);

        _ = resolved.Should().Be(x64Tool);
    }

    /// <summary>
    /// Verifies that the configured path is always used when it exists.
    /// </summary>
    [Fact]
    public void FindToolExecutablePath_ConfiguredPathExists_ReturnsConfiguredPath()
    {
        const string toolFileName = "cdb.exe";
        var configured = @"C:\Mock\custom-cdb.exe";

        var fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);
        _ = fileSystemMock
            .Setup(m => m.FileExists(configured))
            .Returns(true);

        var locator = new WindowsKitsToolLocator(fileSystemMock.Object);

        var resolved = locator.FindToolExecutablePath(toolFileName, configured, Architecture.Arm64);

        _ = resolved.Should().Be(configured);
    }

    /// <summary>
    /// Verifies that the locator falls back to any-architecture enumeration when the best-match probing finds no candidates.
    /// </summary>
    [Fact]
    public void FindToolExecutablePath_WhenNoBestMatchButAnyArchitectureHasTool_ReturnsToolFromEnumeratedArchitectureDirectory()
    {
        const string toolFileName = "cdb.exe";
        var kitsBase = @"C:\Mock\Windows Kits";
        var versionDirectory = Path.Combine(kitsBase, "10");
        var debuggersRoot = Path.Combine(versionDirectory, "Debuggers");
        var enumeratedArchitectureDirectory = Path.Combine(debuggersRoot, "custom-arch");

        var expected = Path.Combine(enumeratedArchitectureDirectory, toolFileName);

        var fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);
        _ = fileSystemMock
            .Setup(m => m.CombinePaths(It.IsAny<string[]>()))
            .Returns((string[] paths) => Path.Combine(paths));

        _ = fileSystemMock
            .Setup(m => m.DirectoryExists(kitsBase))
            .Returns(true);
        _ = fileSystemMock
            .Setup(m => m.GetDirectories(kitsBase))
            .Returns(new[] { versionDirectory });
        _ = fileSystemMock
            .Setup(m => m.DirectoryExists(debuggersRoot))
            .Returns(true);

        _ = fileSystemMock
            .Setup(m => m.GetDirectories(debuggersRoot))
            .Returns(new[] { enumeratedArchitectureDirectory });

        _ = fileSystemMock
            .Setup(m => m.FileExists(It.IsAny<string>()))
            .Returns(false);
        _ = fileSystemMock
            .Setup(m => m.FileExists(expected))
            .Returns(true);

        var locator = new WindowsKitsToolLocator(fileSystemMock.Object, new[] { kitsBase });

        var resolved = locator.FindToolExecutablePath(toolFileName, null, Architecture.X64);

        _ = resolved.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that the locator probes legacy known paths when no Windows Kits directories can be enumerated.
    /// </summary>
    [Fact]
    public void FindToolExecutablePath_WhenNoDebuggerRootsAndLegacyKnownPathExists_ReturnsLegacyPath()
    {
        const string toolFileName = "cdb.exe";
        const string kitsBase = @"C:\Mock\NonExistentWindowsKitsBase";

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).Trim().TrimEnd('\\');
        var expectedLegacyRoot = Path.Combine(programFilesX86, @"Windows Kits\10\Debuggers");
        var expected = Path.Combine(expectedLegacyRoot, "x64", toolFileName);

        var fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);
        _ = fileSystemMock
            .Setup(m => m.DirectoryExists(kitsBase))
            .Returns(false);

        _ = fileSystemMock
            .Setup(m => m.FileExists(It.IsAny<string>()))
            .Returns(false);
        _ = fileSystemMock
            .Setup(m => m.FileExists(expected))
            .Returns(true);

        var locator = new WindowsKitsToolLocator(fileSystemMock.Object, new[] { kitsBase });

        var resolved = locator.FindToolExecutablePath(toolFileName, null, Architecture.X64);

        _ = resolved.Should().Be(expected);
    }
}

