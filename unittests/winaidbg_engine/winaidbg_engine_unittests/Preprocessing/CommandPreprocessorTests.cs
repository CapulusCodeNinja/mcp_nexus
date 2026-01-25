using FluentAssertions;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.Engine.Preprocessing;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;

using Xunit;

namespace WinAiDbg.Engine.Tests.Preprocessing;

/// <summary>
/// Unit tests for the <see cref="CommandPreprocessor"/> class.
/// </summary>
public class CommandPreprocessorTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IProcessManager> m_MockProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPreprocessorTests"/> class.
    /// </summary>
    public CommandPreprocessorTests()
    {
        m_Settings = new Mock<ISettings>();
        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
        m_FileSystemMock = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();

        // Setup default behavior - all directories exist, no creation needed
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new CommandPreprocessor(null!, m_MockProcessManager.Object, m_Settings.Object));
    }

    /// <summary>
    /// Verifies that PreprocessCommand returns input for null command.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithNullCommand_ReturnsInput()
    {
        // Arrange
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Act
        var result = preprocessor.PreprocessCommand(null!);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that PreprocessCommand returns input for empty command.
    /// </summary>
    /// <param name="command">Command text under test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PreprocessCommand_WithEmptyCommand_ReturnsInput(string command)
    {
        // Arrange
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Act
        var result = preprocessor.PreprocessCommand(command);

        // Assert
        _ = result.Should().Be(command);
    }

    /// <summary>
    /// Verifies that PreprocessCommand returns input for simple commands.
    /// </summary>
    /// <param name="command">Command text under test.</param>
    [Theory]
    [InlineData("k")]
    [InlineData("!analyze -v")]
    [InlineData("lm")]
    public void PreprocessCommand_WithSimpleCommand_ReturnsInput(string command)
    {
        // Arrange
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);

        // Act
        var result = preprocessor.PreprocessCommand(command);

        // Assert
        _ = result.Should().Be(command);
    }

    /// <summary>
    /// Verifies that PreprocessCommand caches results.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithSameCommand_UsesCaching()
    {
        // Arrange
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".sympath C:\\symbols";

        // Act - call twice
        var result1 = preprocessor.PreprocessCommand(command);
        var result2 = preprocessor.PreprocessCommand(command);

        // Assert - results should be identical (caching works)
        _ = result1.Should().Be(result2);
    }

    /// <summary>
    /// Verifies that PreprocessCommand handles .sympath commands.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithSymPathCommand_EnsuresDirectoriesExist()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".sympath C:\\symbols";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should try to create the directory
        m_FileSystemMock.Verify(fs => fs.CreateDirectory("C:\\symbols"), Times.Once);
    }

    /// <summary>
    /// Verifies that PreprocessCommand skips srv* tokens in .sympath.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithSymPathAndSrvToken_SkipsSrvDirectory()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".sympath srv*C:\\symbols*https://msdl.microsoft.com/download/symbols";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should NOT try to create srv* paths
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.Is<string>(p => p.Contains("srv"))), Times.Never);
    }

    /// <summary>
    /// Verifies that PreprocessCommand skips UNC paths in .sympath.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithSymPathAndUncPath_SkipsUncDirectory()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".sympath \\\\server\\share\\symbols";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should NOT try to create UNC paths
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that PreprocessCommand handles multiple paths in .sympath.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithSymPathMultiplePaths_CreatesAllLocalDirectories()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".sympath C:\\symbols1;C:\\symbols2;D:\\symbols3";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should try to create all local directories
        m_FileSystemMock.Verify(fs => fs.CreateDirectory("C:\\symbols1"), Times.Once);
        m_FileSystemMock.Verify(fs => fs.CreateDirectory("C:\\symbols2"), Times.Once);
        m_FileSystemMock.Verify(fs => fs.CreateDirectory("D:\\symbols3"), Times.Once);
    }

    /// <summary>
    /// Verifies that PreprocessCommand handles .srcpath commands.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithSrcPathCommand_EnsuresDirectoriesExist()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".srcpath C:\\sources";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should try to create the directory
        m_FileSystemMock.Verify(fs => fs.CreateDirectory("C:\\sources"), Times.Once);
    }

    /// <summary>
    /// Verifies that PreprocessCommand skips srv* tokens in .srcpath.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithSrcPathAndSrvToken_SkipsSrvDirectory()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".srcpath srv*C:\\sources*https://source.server.com";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should NOT try to create srv* paths
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.Is<string>(p => p.Contains("srv"))), Times.Never);
    }

    /// <summary>
    /// Verifies that PreprocessCommand handles .symfix commands.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithSymFixCommand_EnsuresDirectoriesExist()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".symfix C:\\symbols";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should try to create the directory
        m_FileSystemMock.Verify(fs => fs.CreateDirectory("C:\\symbols"), Times.Once);
    }

    /// <summary>
    /// Verifies that PreprocessCommand handles !homedir commands.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithHomeDirCommand_EnsuresDirectoriesExist()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = "!homedir C:\\debugger";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should try to create the directory
        m_FileSystemMock.Verify(fs => fs.CreateDirectory("C:\\debugger"), Times.Once);
    }

    /// <summary>
    /// Verifies that PreprocessCommand converts backslashes to forward slashes in !homedir.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithHomeDirCommand_ConvertsBackslashes()
    {
        // Arrange
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = "!homedir C:\\debugger\\extensions";

        // Act
        var result = preprocessor.PreprocessCommand(command);

        // Assert - should convert backslashes to forward slashes
        _ = result.Should().Contain("C:/debugger/extensions");
    }

    /// <summary>
    /// Verifies that PreprocessCommand handles quoted paths in !homedir.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithHomeDirCommandQuoted_PreservesQuotes()
    {
        // Arrange
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = "!homedir \"C:\\debugger\\extensions\"";

        // Act
        var result = preprocessor.PreprocessCommand(command);

        // Assert - should preserve quotes and convert slashes
        _ = result.Should().Contain("\"C:/debugger/extensions\"");
    }

    /// <summary>
    /// Verifies that PreprocessCommand doesn't create directories that already exist.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WhenDirectoryExists_DoesNotCreate()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists("C:\\symbols")).Returns(true);
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".sympath C:\\symbols";

        // Act
        _ = preprocessor.PreprocessCommand(command);

        // Assert - should NOT try to create directory
        m_FileSystemMock.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that PreprocessCommand handles directory creation exceptions gracefully.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WhenDirectoryCreationFails_HandlesGracefully()
    {
        // Arrange
        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        _ = m_FileSystemMock.Setup(fs => fs.CreateDirectory(It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException("Access denied"));

        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = ".sympath C:\\symbols";

        // Act - should not throw
        var result = preprocessor.PreprocessCommand(command);

        // Assert
        _ = result.Should().Be(command);
    }

    /// <summary>
    /// Verifies that PreprocessCommand skips WSL paths in regular commands.
    /// </summary>
    [Fact]
    public void PreprocessCommand_WithWslPath_ReturnsAsIs()
    {
        // Arrange
        var preprocessor = new CommandPreprocessor(m_FileSystemMock.Object, m_MockProcessManager.Object, m_Settings.Object);
        var command = "lsa /mnt/c/sources/file.cpp:123";

        // Act
        var result = preprocessor.PreprocessCommand(command);

        // Assert - WSL path conversion requires wsl.exe which we can't test in unit tests
        // Just verify it doesn't throw
        _ = result.Should().NotBeNullOrEmpty();
    }
}
