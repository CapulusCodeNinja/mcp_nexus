using System.Diagnostics;

using FluentAssertions;

using Moq;

using Nexus.Config;
using Nexus.Config.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using Xunit;

namespace Nexus.Engine.DumpCheck.Tests;

/// <summary>
/// Unit tests for the <see cref="DumpValidator"/> class.
/// </summary>
public class DumpValidatorTests
{
    private readonly Mock<IFileSystem> m_FileSystem;

    private readonly Mock<ISettings> m_Settings;

    private readonly Mock<IProcessManager> m_ProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DumpValidatorTests"/> class.
    /// </summary>
    public DumpValidatorTests()
    {
        m_FileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        m_Settings = new Mock<ISettings>(MockBehavior.Strict);
        m_ProcessManager = new Mock<IProcessManager>(MockBehavior.Strict);
    }

    /// <summary>
    /// Creates a new <see cref="DumpValidator"/> instance for testing.
    /// </summary>
    /// <returns>A configured <see cref="DumpValidator"/> instance.</returns>
    private DumpValidator CreateValidator()
    {
        return new DumpValidator(m_FileSystem.Object, m_Settings.Object, m_ProcessManager.Object);
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_NullFileSystem_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DumpValidator(null!, m_Settings.Object, m_ProcessManager.Object);

        // Assert
        _ = act.Should().Throw<ArgumentNullException>().WithParameterName("fileSystem");
    }

    /// <summary>
    /// Verifies that <see cref="DumpValidator.RunDumpChkAsync(string, CancellationToken)"/> short-circuits
    /// when dumpchk integration is disabled and does not start any process.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunDumpChkAsync_DumpChkDisabled_ReturnsDisabledMessageAndDoesNotStartProcess()
    {
        // Arrange
        var sharedConfiguration = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Validation = new ValidationSettings
                {
                    DumpChkEnabled = false,
                },
            },
        };

        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfiguration);

        var dumpPath = @"C:\dumps\valid.dmp";
        var validator = CreateValidator();

        // Act
        var result = await validator.RunDumpChkAsync(dumpPath, CancellationToken.None);

        // Assert
        _ = result.IsEnabled.Should().BeFalse();
        _ = result.WasExecuted.Should().BeFalse();
        _ = result.ExitCode.Should().Be(-1);
        _ = result.Message.Should().Be("Dumpchk is disabled in configuration.");
        m_ProcessManager.Verify(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()), Times.Never);
    }

    /// <summary>
    /// Verifies that <see cref="DumpValidator.RunDumpChkAsync(string, CancellationToken)"/> throws
    /// <see cref="InvalidOperationException"/> when dumpchk is enabled but cannot be located.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunDumpChkAsync_DumpChkEnabledAndNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var sharedConfiguration = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Validation = new ValidationSettings
                {
                    DumpChkEnabled = true,
                    DumpChkPath = null,
                },
            },
        };

        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfiguration);

        var dumpPath = @"C:\dumps\valid.dmp";

        _ = m_FileSystem.Setup(fs => fs.FileExists(It.Is<string>(p => p == dumpPath))).Returns(true);
        _ = m_FileSystem.Setup(fs => fs.FileExists(It.Is<string>(p => p != dumpPath))).Returns(false);
        _ = m_FileSystem.Setup(fs => fs.ProbeRead(dumpPath));

        var validator = CreateValidator();

        // Act
        var act = async () => await validator.RunDumpChkAsync(dumpPath, CancellationToken.None);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Dumpchk executable not found*");

        m_FileSystem.Verify(fs => fs.FileExists(dumpPath), Times.Once);
        m_ProcessManager.Verify(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()), Times.Never);
    }

    /// <summary>
    /// Verifies that <see cref="DumpValidator.Validate(string)"/> throws <see cref="FileNotFoundException"/> when the dump file does not exist.
    /// </summary>
    [Fact]
    public void Validate_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var dumpPath = @"C:\dumps\missing.dmp";
        _ = m_FileSystem.Setup(fs => fs.FileExists(dumpPath)).Returns(false);
        var validator = CreateValidator();

        // Act
        var act = () => validator.Validate(dumpPath);

        // Assert
        _ = act.Should().Throw<FileNotFoundException>()
            .Where(ex => ex.FileName == dumpPath);
        m_FileSystem.Verify(fs => fs.FileExists(dumpPath), Times.Once);
        m_FileSystem.Verify(fs => fs.ProbeRead(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that <see cref="DumpValidator.Validate(string)"/> calls <see cref="IFileSystem.ProbeRead(string)"/> when the file exists.
    /// </summary>
    [Fact]
    public void Validate_FileExists_ProbesReadOnce()
    {
        // Arrange
        var dumpPath = @"C:\dumps\valid.dmp";
        _ = m_FileSystem.Setup(fs => fs.FileExists(dumpPath)).Returns(true);
        _ = m_FileSystem.Setup(fs => fs.ProbeRead(dumpPath));
        var validator = CreateValidator();

        // Act
        validator.Validate(dumpPath);

        // Assert
        m_FileSystem.Verify(fs => fs.FileExists(dumpPath), Times.Once);
        m_FileSystem.Verify(fs => fs.ProbeRead(dumpPath), Times.Once);
    }

    /// <summary>
    /// Verifies that <see cref="DumpValidator.Validate(string)"/> propagates <see cref="UnauthorizedAccessException"/> from <see cref="IFileSystem.ProbeRead(string)"/>.
    /// </summary>
    [Fact]
    public void Validate_ProbeReadThrowsUnauthorizedAccessException_PropagatesException()
    {
        // Arrange
        var dumpPath = @"C:\dumps\protected.dmp";
        _ = m_FileSystem.Setup(fs => fs.FileExists(dumpPath)).Returns(true);
        _ = m_FileSystem.Setup(fs => fs.ProbeRead(dumpPath))
            .Throws(new UnauthorizedAccessException("Access denied"));
        var validator = CreateValidator();

        // Act
        var act = () => validator.Validate(dumpPath);

        // Assert
        _ = act.Should().Throw<UnauthorizedAccessException>();
        m_FileSystem.Verify(fs => fs.FileExists(dumpPath), Times.Once);
        m_FileSystem.Verify(fs => fs.ProbeRead(dumpPath), Times.Once);
    }

    /// <summary>
    /// Verifies that <see cref="DumpValidator.Validate(string)"/> propagates <see cref="IOException"/> from <see cref="IFileSystem.ProbeRead(string)"/>.
    /// </summary>
    [Fact]
    public void Validate_ProbeReadThrowsIOException_PropagatesException()
    {
        // Arrange
        var dumpPath = @"C:\dumps\corrupt.dmp";
        _ = m_FileSystem.Setup(fs => fs.FileExists(dumpPath)).Returns(true);
        _ = m_FileSystem.Setup(fs => fs.ProbeRead(dumpPath))
            .Throws(new IOException("I/O error"));
        var validator = CreateValidator();

        // Act
        var act = () => validator.Validate(dumpPath);

        // Assert
        _ = act.Should().Throw<IOException>();
        m_FileSystem.Verify(fs => fs.FileExists(dumpPath), Times.Once);
        m_FileSystem.Verify(fs => fs.ProbeRead(dumpPath), Times.Once);
    }
}


