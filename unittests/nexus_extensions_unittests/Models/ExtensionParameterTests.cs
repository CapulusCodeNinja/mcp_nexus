using FluentAssertions;

using Nexus.Extensions.Models;

namespace Nexus.Extensions_unittests.Models;

/// <summary>
/// Unit tests for the ExtensionParameter class.
/// </summary>
public class ExtensionParameterTests
{
    /// <summary>
    /// Verifies that ExtensionParameter can be instantiated with default values.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var parameter = new ExtensionParameter();

        // Assert
        _ = parameter.Name.Should().Be(string.Empty);
        _ = parameter.Type.Should().Be("string");
        _ = parameter.Description.Should().Be(string.Empty);
        _ = parameter.Required.Should().BeFalse();
        _ = parameter.Default.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Name property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Name_ShouldSetAndGetValue()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        const string name = "testParameter";

        // Act
        parameter.Name = name;

        // Assert
        _ = parameter.Name.Should().Be(name);
    }

    /// <summary>
    /// Verifies that Type property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Type_ShouldSetAndGetValue()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        const string type = "int";

        // Act
        parameter.Type = type;

        // Assert
        _ = parameter.Type.Should().Be(type);
    }

    /// <summary>
    /// Verifies that Description property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Description_ShouldSetAndGetValue()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        const string description = "Test description";

        // Act
        parameter.Description = description;

        // Assert
        _ = parameter.Description.Should().Be(description);
    }

    /// <summary>
    /// Verifies that Required property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Required_ShouldSetAndGetValue()
    {
        // Arrange
        var parameter = new ExtensionParameter
        {
            // Act
            Required = true
        };

        // Assert
        _ = parameter.Required.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Default property can be set and retrieved with string value.
    /// </summary>
    [Fact]
    public void Default_ShouldSetAndGetStringValue()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        const string defaultValue = "defaultString";

        // Act
        parameter.Default = defaultValue;

        // Assert
        _ = parameter.Default.Should().Be(defaultValue);
    }

    /// <summary>
    /// Verifies that Default property can be set and retrieved with numeric value.
    /// </summary>
    [Fact]
    public void Default_ShouldSetAndGetNumericValue()
    {
        // Arrange
        var parameter = new ExtensionParameter();
        const int defaultValue = 42;

        // Act
        parameter.Default = defaultValue;

        // Assert
        _ = parameter.Default.Should().Be(defaultValue);
    }

    /// <summary>
    /// Verifies that Default property can be null.
    /// </summary>
    [Fact]
    public void Default_ShouldAllowNullValue()
    {
        // Arrange
        var parameter = new ExtensionParameter
        {
            Default = "test"
        };

        // Act
        parameter.Default = null;

        // Assert
        _ = parameter.Default.Should().BeNull();
    }

    /// <summary>
    /// Verifies that all properties can be set via object initializer.
    /// </summary>
    [Fact]
    public void ObjectInitializer_ShouldSetAllProperties()
    {
        // Act
        var parameter = new ExtensionParameter
        {
            Name = "sessionId",
            Type = "string",
            Description = "The session identifier",
            Required = true,
            Default = "default-session"
        };

        // Assert
        _ = parameter.Name.Should().Be("sessionId");
        _ = parameter.Type.Should().Be("string");
        _ = parameter.Description.Should().Be("The session identifier");
        _ = parameter.Required.Should().BeTrue();
        _ = parameter.Default.Should().Be("default-session");
    }
}

