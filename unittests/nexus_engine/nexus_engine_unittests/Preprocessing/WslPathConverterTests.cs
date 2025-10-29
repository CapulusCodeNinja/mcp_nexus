using System.Diagnostics;

using FluentAssertions;

using Moq;

using Nexus.Engine.Preprocessing;
using Nexus.External.Apis.ProcessManagement;

using Xunit;

namespace Nexus.Engine.Tests.Preprocessing;

/// <summary>
/// Unit tests for the <see cref="WslPathConverter"/> class.
/// </summary>
public class WslPathConverterTests
{
    private readonly Mock<IProcessManager> m_ProcessManagerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="WslPathConverterTests"/> class.
    /// </summary>
    public WslPathConverterTests()
    {
        m_ProcessManagerMock = new Mock<IProcessManager>();
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when processManager is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProcessManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new WslPathConverter(null!));
    }

    /// <summary>
    /// Verifies that TryConvertToWindowsPath returns false when process fails to start.
    /// </summary>
    [Fact]
    public void TryConvertToWindowsPath_WhenProcessFailsToStart_ReturnsFalse()
    {
        // Arrange
        _ = m_ProcessManagerMock.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns<Process?>(null!);

        var converter = new WslPathConverter(m_ProcessManagerMock.Object);

        // Act
        var result = converter.TryConvertToWindowsPath("/mnt/c/test", out var windowsPath);

        // Assert
        _ = result.Should().BeFalse();
        _ = windowsPath.Should().Be("/mnt/c/test");
    }

    /// <summary>
    /// Verifies that TryConvertToWindowsPath returns false when process times out.
    /// </summary>
    [Fact]
    public void TryConvertToWindowsPath_WhenProcessTimesOut_ReturnsFalse()
    {
        // Arrange
        var mockProcess = new Mock<Process>();
        _ = m_ProcessManagerMock.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns(mockProcess.Object);
        _ = m_ProcessManagerMock.Setup(pm => pm.WaitForProcessExit(mockProcess.Object, It.IsAny<int>()))
            .Returns(false);

        var converter = new WslPathConverter(m_ProcessManagerMock.Object);

        // Act
        var result = converter.TryConvertToWindowsPath("/mnt/c/test", out var windowsPath);

        // Assert
        _ = result.Should().BeFalse();
        _ = windowsPath.Should().Be("/mnt/c/test");
        m_ProcessManagerMock.Verify(pm => pm.KillProcess(mockProcess.Object), Times.Once);
    }

    /// <summary>
    /// Verifies that TryConvertToWindowsPath handles kill exceptions gracefully.
    /// </summary>
    [Fact]
    public void TryConvertToWindowsPath_WhenKillThrows_HandlesGracefully()
    {
        // Arrange
        var mockProcess = new Mock<Process>();
        _ = m_ProcessManagerMock.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns(mockProcess.Object);
        _ = m_ProcessManagerMock.Setup(pm => pm.WaitForProcessExit(mockProcess.Object, It.IsAny<int>()))
            .Returns(false);
        _ = m_ProcessManagerMock.Setup(pm => pm.KillProcess(mockProcess.Object))
            .Throws(new InvalidOperationException("Process already exited"));

        var converter = new WslPathConverter(m_ProcessManagerMock.Object);

        // Act
        var result = converter.TryConvertToWindowsPath("/mnt/c/test", out var windowsPath);

        // Assert - should not throw
        _ = result.Should().BeFalse();
        _ = windowsPath.Should().Be("/mnt/c/test");
    }

    /// <summary>
    /// Verifies that TryConvertToWindowsPath handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void TryConvertToWindowsPath_WhenExceptionThrown_ReturnsFalse()
    {
        // Arrange
        _ = m_ProcessManagerMock.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Throws(new InvalidOperationException("Process error"));

        var converter = new WslPathConverter(m_ProcessManagerMock.Object);

        // Act
        var result = converter.TryConvertToWindowsPath("/mnt/c/test", out var windowsPath);

        // Assert
        _ = result.Should().BeFalse();
        _ = windowsPath.Should().Be("/mnt/c/test");
    }

    /// <summary>
    /// Verifies that LoadFstabMappings returns empty dictionary when process fails to start.
    /// </summary>
    [Fact]
    public void LoadFstabMappings_WhenProcessFailsToStart_ReturnsEmptyDictionary()
    {
        // Arrange
        _ = m_ProcessManagerMock.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns<Process?>(null!);

        var converter = new WslPathConverter(m_ProcessManagerMock.Object);

        // Act
        var result = converter.LoadFstabMappings();

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that LoadFstabMappings returns empty dictionary when process times out.
    /// </summary>
    [Fact]
    public void LoadFstabMappings_WhenProcessTimesOut_ReturnsEmptyDictionary()
    {
        // Arrange
        var mockProcess = new Mock<Process>();
        _ = m_ProcessManagerMock.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Returns(mockProcess.Object);
        _ = m_ProcessManagerMock.Setup(pm => pm.WaitForProcessExit(mockProcess.Object, It.IsAny<int>()))
            .Returns(false);

        var converter = new WslPathConverter(m_ProcessManagerMock.Object);

        // Act
        var result = converter.LoadFstabMappings();

        // Assert
        _ = result.Should().BeEmpty();
        m_ProcessManagerMock.Verify(pm => pm.KillProcess(mockProcess.Object), Times.Once);
    }

    /// <summary>
    /// Verifies that LoadFstabMappings handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void LoadFstabMappings_WhenExceptionThrown_ReturnsEmptyDictionary()
    {
        // Arrange
        _ = m_ProcessManagerMock.Setup(pm => pm.StartProcess(It.IsAny<ProcessStartInfo>()))
            .Throws(new InvalidOperationException("Process error"));

        var converter = new WslPathConverter(m_ProcessManagerMock.Object);

        // Act
        var result = converter.LoadFstabMappings();

        // Assert
        _ = result.Should().BeEmpty();
    }
}
