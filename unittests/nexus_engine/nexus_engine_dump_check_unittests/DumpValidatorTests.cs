using FluentAssertions;

using Moq;

using Nexus.External.Apis.FileSystem;

using Xunit;

namespace Nexus.Engine.DumpCheck.Tests;

/// <summary>
/// Unit tests for the <see cref="DumpValidator"/> class.
/// </summary>
public class DumpValidatorTests
{
    private readonly Mock<IFileSystem> m_FileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="DumpValidatorTests"/> class.
    /// </summary>
    public DumpValidatorTests()
    {
        m_FileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
    }

    /// <summary>
    /// Creates a new <see cref="DumpValidator"/> instance for testing.
    /// </summary>
    /// <returns>A configured <see cref="DumpValidator"/> instance.</returns>
    private DumpValidator CreateValidator()
    {
        return new DumpValidator(m_FileSystem.Object);
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_NullFileSystem_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DumpValidator(null!);

        // Assert
        _ = act.Should().Throw<ArgumentNullException>().WithParameterName("fileSystem");
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


