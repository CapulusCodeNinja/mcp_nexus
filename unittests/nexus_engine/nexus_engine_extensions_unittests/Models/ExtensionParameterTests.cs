using FluentAssertions;

using Nexus.Engine.Extensions.Models;

namespace Nexus.Engine.Extensions.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="ExtensionParameter"/> class.
/// </summary>
public class ExtensionParameterTests
{
    /// <summary>
    /// Verifies that default constructor initializes properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Act
        var parameter = new ExtensionParameter();

        // Assert
        _ = parameter.Name.Should().BeEmpty();
        _ = parameter.Type.Should().Be("string");
        _ = parameter.Description.Should().BeEmpty();
        _ = parameter.Required.Should().BeFalse();
        _ = parameter.Default.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Name property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Name_CanBeSetAndRetrieved()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        var expectedName = "TestParam";

        // Act
        parameter.Name = expectedName;

        // Assert
        _ = parameter.Name.Should().Be(expectedName);
    }

    /// <summary>
    /// Verifies that Type property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Type_CanBeSetAndRetrieved()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        var expectedType = "int";

        // Act
        parameter.Type = expectedType;

        // Assert
        _ = parameter.Type.Should().Be(expectedType);
    }

    /// <summary>
    /// Verifies that Description property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Description_CanBeSetAndRetrieved()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        var expectedDescription = "Test description";

        // Act
        parameter.Description = expectedDescription;

        // Assert
        _ = parameter.Description.Should().Be(expectedDescription);
    }

    /// <summary>
    /// Verifies that Required property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Required_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var parameter = new ExtensionParameter
        {
            Required = true
        };

        // Assert
        _ = parameter.Required.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Default property can be set and retrieved with string value.
    /// </summary>
    [Fact]
    public void Default_CanBeSetAndRetrieved_WithStringValue()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        var expectedDefault = "default value";

        // Act
        parameter.Default = expectedDefault;

        // Assert
        _ = parameter.Default.Should().Be(expectedDefault);
    }

    /// <summary>
    /// Verifies that Default property can be set and retrieved with int value.
    /// </summary>
    [Fact]
    public void Default_CanBeSetAndRetrieved_WithIntValue()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        var expectedDefault = 42;

        // Act
        parameter.Default = expectedDefault;

        // Assert
        _ = parameter.Default.Should().Be(expectedDefault);
    }

    /// <summary>
    /// Verifies that Default property can be null.
    /// </summary>
    [Fact]
    public void Default_CanBeNull()
    {
        // Arrange & Act
        var parameter = new ExtensionParameter
        {
            Default = null
        };

        // Assert
        _ = parameter.Default.Should().BeNull();
    }

    /// <summary>
    /// Verifies that all properties can be initialized together.
    /// </summary>
    [Fact]
    public void AllProperties_CanBeInitializedTogether()
    {
        // Arrange & Act
        var parameter = new ExtensionParameter
        {
            Name = "TestParam",
            Type = "bool",
            Description = "A test parameter",
            Required = true,
            Default = true
        };

        // Assert
        _ = parameter.Name.Should().Be("TestParam");
        _ = parameter.Type.Should().Be("bool");
        _ = parameter.Description.Should().Be("A test parameter");
        _ = parameter.Required.Should().BeTrue();
        _ = parameter.Default.Should().Be(true);
    }
}

