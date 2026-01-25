using FluentAssertions;

using WinAiDbg.Engine.Extensions.Models;

using Xunit;

namespace WinAiDbg.Engine.Extensions.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="ExtensionMetadata"/> class.
/// </summary>
public class ExtensionMetadataTests
{
    /// <summary>
    /// Verifies that default constructor initializes properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Act
        var metadata = new ExtensionMetadata();

        // Assert
        _ = metadata.Name.Should().BeEmpty();
        _ = metadata.Description.Should().BeEmpty();
        _ = metadata.Version.Should().BeEmpty();
        _ = metadata.Author.Should().BeEmpty();
        _ = metadata.ScriptFile.Should().BeEmpty();
        _ = metadata.FullScriptPath.Should().BeEmpty();
        _ = metadata.ScriptType.Should().BeEmpty();
        _ = metadata.TimeoutMs.Should().Be(300000); // 5 minutes
        _ = metadata.RequiredParameters.Should().NotBeNull().And.BeEmpty();
        _ = metadata.OptionalParameters.Should().NotBeNull().And.BeEmpty();
        _ = metadata.ExtensionPath.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that Name property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Name_CanBeSetAndRetrieved()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        var expectedName = "TestExtension";

        // Act
        metadata.Name = expectedName;

        // Assert
        _ = metadata.Name.Should().Be(expectedName);
    }

    /// <summary>
    /// Verifies that Description property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Description_CanBeSetAndRetrieved()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        var expectedDescription = "Test Description";

        // Act
        metadata.Description = expectedDescription;

        // Assert
        _ = metadata.Description.Should().Be(expectedDescription);
    }

    /// <summary>
    /// Verifies that Version property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Version_CanBeSetAndRetrieved()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        var expectedVersion = "1.0.0";

        // Act
        metadata.Version = expectedVersion;

        // Assert
        _ = metadata.Version.Should().Be(expectedVersion);
    }

    /// <summary>
    /// Verifies that TimeoutMs property can be set and retrieved.
    /// </summary>
    [Fact]
    public void TimeoutMs_CanBeSetAndRetrieved()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        var expectedTimeout = 60000; // 1 minute

        // Act
        metadata.TimeoutMs = expectedTimeout;

        // Assert
        _ = metadata.TimeoutMs.Should().Be(expectedTimeout);
    }

    /// <summary>
    /// Verifies that RequiredParameters collection can be modified.
    /// </summary>
    [Fact]
    public void RequiredParameters_CanBeModified()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        var parameter = new ExtensionParameter { Name = "TestParam", Required = true };

        // Act
        metadata.RequiredParameters.Add(parameter);

        // Assert
        _ = metadata.RequiredParameters.Should().HaveCount(1);
        _ = metadata.RequiredParameters[0].Name.Should().Be("TestParam");
    }

    /// <summary>
    /// Verifies that OptionalParameters collection can be modified.
    /// </summary>
    [Fact]
    public void OptionalParameters_CanBeModified()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        var parameter = new ExtensionParameter { Name = "OptionalParam", Required = false };

        // Act
        metadata.OptionalParameters.Add(parameter);

        // Assert
        _ = metadata.OptionalParameters.Should().HaveCount(1);
        _ = metadata.OptionalParameters[0].Name.Should().Be("OptionalParam");
    }
}
