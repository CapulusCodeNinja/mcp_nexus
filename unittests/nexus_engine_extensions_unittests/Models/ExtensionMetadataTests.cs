using System.Text.Json;

using FluentAssertions;

using Nexus.Engine.Extensions.Models;

namespace Nexus.Engine.Extensions.Tests.Models;

/// <summary>
/// Unit tests for the ExtensionMetadata class.
/// </summary>
public class ExtensionMetadataTests
{
    /// <summary>
    /// Verifies that ExtensionMetadata can be instantiated with default values.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var metadata = new ExtensionMetadata();

        // Assert
        _ = metadata.Name.Should().Be(string.Empty);
        _ = metadata.Description.Should().Be(string.Empty);
        _ = metadata.Version.Should().Be(string.Empty);
        _ = metadata.Author.Should().Be(string.Empty);
        _ = metadata.ScriptType.Should().Be(string.Empty);
        _ = metadata.ScriptFile.Should().Be(string.Empty);
        _ = metadata.TimeoutMs.Should().Be(300000); // 5 minutes default
        _ = metadata.RequiredParameters.Should().NotBeNull();
        _ = metadata.RequiredParameters.Should().BeEmpty();
        _ = metadata.OptionalParameters.Should().NotBeNull();
        _ = metadata.OptionalParameters.Should().BeEmpty();
        _ = metadata.ExtensionPath.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that Name property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Name_ShouldSetAndGetValue()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        const string name = "stack_with_sources";

        // Act
        metadata.Name = name;

        // Assert
        _ = metadata.Name.Should().Be(name);
    }

    /// <summary>
    /// Verifies that Description property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Description_ShouldSetAndGetValue()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        const string description = "Analyze crash dump with source information";

        // Act
        metadata.Description = description;

        // Assert
        _ = metadata.Description.Should().Be(description);
    }

    /// <summary>
    /// Verifies that Version property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Version_ShouldSetAndGetValue()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        const string version = "2.0.0";

        // Act
        metadata.Version = version;

        // Assert
        _ = metadata.Version.Should().Be(version);
    }

    /// <summary>
    /// Verifies that Author property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Author_ShouldSetAndGetValue()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        const string author = "Test Author";

        // Act
        metadata.Author = author;

        // Assert
        _ = metadata.Author.Should().Be(author);
    }

    /// <summary>
    /// Verifies that ScriptType property can be set and retrieved.
    /// </summary>
    [Fact]
    public void ScriptType_ShouldSetAndGetValue()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        const string scriptType = "python";

        // Act
        metadata.ScriptType = scriptType;

        // Assert
        _ = metadata.ScriptType.Should().Be(scriptType);
    }

    /// <summary>
    /// Verifies that ScriptFile property can be set and retrieved.
    /// </summary>
    [Fact]
    public void ScriptFile_ShouldSetAndGetValue()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        const string scriptFile = "analyze.ps1";

        // Act
        metadata.ScriptFile = scriptFile;

        // Assert
        _ = metadata.ScriptFile.Should().Be(scriptFile);
    }

    /// <summary>
    /// Verifies that Timeout property can be set and retrieved.
    /// </summary>
    [Fact]
    public void Timeout_ShouldSetAndGetValue()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        const int timeout = 600000; // 10 minutes

        // Act
        metadata.TimeoutMs = timeout;

        // Assert
        _ = metadata.TimeoutMs.Should().Be(timeout);
    }

    /// <summary>
    /// Verifies that Requires list can be populated.
    /// </summary>
    [Fact]
    public void Requires_ShouldAllowAddingItems()
    {
        // Arrange
        var metadata = new ExtensionMetadata();

        // Act
        metadata.RequiredParameters.Add(new ExtensionParameter { Name = "Module1" });
        metadata.RequiredParameters.Add(new ExtensionParameter { Name = "Module2" });

        // Assert
        _ = metadata.RequiredParameters.Should().HaveCount(2);
        _ = metadata.RequiredParameters.Should().Contain(p => p.Name == "Module1");
        _ = metadata.RequiredParameters.Should().Contain(p => p.Name == "Module2");
    }

    /// <summary>
    /// Verifies that Parameters list can be populated.
    /// </summary>
    [Fact]
    public void Parameters_ShouldAllowAddingItems()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        var param1 = new ExtensionParameter { Name = "param1", Type = "string" };
        var param2 = new ExtensionParameter { Name = "param2", Type = "int" };

        // Act
        metadata.OptionalParameters.Add(param1);
        metadata.OptionalParameters.Add(param2);

        // Assert
        _ = metadata.OptionalParameters.Should().HaveCount(2);
        _ = metadata.OptionalParameters[0].Name.Should().Be("param1");
        _ = metadata.OptionalParameters[1].Name.Should().Be("param2");
    }

    /// <summary>
    /// Verifies that ExtensionPath property can be set and retrieved.
    /// </summary>
    [Fact]
    public void ExtensionPath_ShouldSetAndGetValue()
    {
        // Arrange
        var metadata = new ExtensionMetadata();
        const string extensionPath = "C:\\Extensions\\MyExtension";

        // Act
        metadata.ExtensionPath = extensionPath;

        // Assert
        _ = metadata.ExtensionPath.Should().Be(extensionPath);
    }

    /// <summary>
    /// Verifies that FullScriptPath combines ExtensionPath and ScriptFile correctly.
    /// </summary>
    [Fact]
    public void FullScriptPath_ShouldCombinePathsCorrectly()
    {
        // Arrange
        var metadata = new ExtensionMetadata
        {
            ExtensionPath = "C:\\Extensions\\MyExtension",
            ScriptFile = "script.ps1"
        };

        // Act
        var fullPath = metadata.FullScriptPath;

        // Assert
        _ = fullPath.Should().Be("C:\\Extensions\\MyExtension\\script.ps1");
    }

    /// <summary>
    /// Verifies that all properties can be set via object initializer.
    /// </summary>
    [Fact]
    public void ObjectInitializer_ShouldSetAllProperties()
    {
        // Act
        var metadata = new ExtensionMetadata
        {
            Name = "test_extension",
            Description = "Test description",
            Version = "1.5.0",
            Author = "Test Author",
            ScriptType = "powershell",
            ScriptFile = "test.ps1",
            TimeoutMs = 300000,
            ExtensionPath = "C:\\Test"
        };
        metadata.RequiredParameters.Add(new ExtensionParameter { Name = "TestModule" });
        metadata.OptionalParameters.Add(new ExtensionParameter { Name = "param1" });

        // Assert
        _ = metadata.Name.Should().Be("test_extension");
        _ = metadata.Description.Should().Be("Test description");
        _ = metadata.Version.Should().Be("1.5.0");
        _ = metadata.Author.Should().Be("Test Author");
        _ = metadata.ScriptType.Should().Be("powershell");
        _ = metadata.ScriptFile.Should().Be("test.ps1");
        _ = metadata.TimeoutMs.Should().Be(300000);
        _ = metadata.ExtensionPath.Should().Be("C:\\Test");
        _ = metadata.RequiredParameters.Should().HaveCount(1);
        _ = metadata.OptionalParameters.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that ExtensionMetadata can be serialized to JSON.
    /// </summary>
    [Fact]
    public void Serialization_ShouldSerializeToJson()
    {
        // Arrange
        var metadata = new ExtensionMetadata
        {
            Name = "test_ext",
            Description = "Test",
            Version = "1.0.0",
            Author = "Author",
            ScriptType = "powershell",
            ScriptFile = "test.ps1",
            TimeoutMs = 600000
        };
        metadata.RequiredParameters.Add(new ExtensionParameter { Name = "Module1" });

        // Act
        var json = JsonSerializer.Serialize(metadata);

        // Assert
        _ = json.Should().Contain("\"name\":\"test_ext\"");
        _ = json.Should().Contain("\"description\":\"Test\"");
        _ = json.Should().Contain("\"version\":\"1.0.0\"");
        _ = json.Should().Contain("\"scriptType\":\"powershell\"");
        _ = json.Should().Contain("\"timeout\":600000");
        _ = json.Should().Contain("\"requires\"");
        _ = json.Should().NotContain("extensionPath"); // JsonIgnore attribute
    }

    /// <summary>
    /// Verifies that ExtensionMetadata can be deserialized from JSON.
    /// </summary>
    [Fact]
    public void Deserialization_ShouldDeserializeFromJson()
    {
        // Arrange
        const string json = @"{
            ""name"": ""test_ext"",
            ""description"": ""Test extension"",
            ""version"": ""2.0.0"",
            ""author"": ""Test Author"",
            ""scriptType"": ""powershell"",
            ""scriptFile"": ""script.ps1"",
            ""timeout"": 900000,
            ""requires"": [""Module1"", ""Module2""],
            ""parameters"": []
        }";

        // Act
        var metadata = JsonSerializer.Deserialize<ExtensionMetadata>(json);

        // Assert
        _ = metadata.Should().NotBeNull();
        _ = metadata!.Name.Should().Be("test_ext");
        _ = metadata.Description.Should().Be("Test extension");
        _ = metadata.Version.Should().Be("2.0.0");
        _ = metadata.Author.Should().Be("Test Author");
        _ = metadata.ScriptType.Should().Be("powershell");
        _ = metadata.ScriptFile.Should().Be("script.ps1");
        _ = metadata.TimeoutMs.Should().Be(900000);
        _ = metadata.RequiredParameters.Should().HaveCount(2);
        _ = metadata.OptionalParameters.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FullScriptPath handles empty ExtensionPath.
    /// </summary>
    [Fact]
    public void FullScriptPath_WithEmptyExtensionPath_ReturnsScriptFile()
    {
        // Arrange
        var metadata = new ExtensionMetadata
        {
            ExtensionPath = "",
            ScriptFile = "script.ps1"
        };

        // Act
        var fullPath = metadata.FullScriptPath;

        // Assert
        _ = fullPath.Should().Be("script.ps1");
    }

    /// <summary>
    /// Verifies that FullScriptPath handles empty ScriptFile.
    /// </summary>
    [Fact]
    public void FullScriptPath_WithEmptyScriptFile_ReturnsExtensionPath()
    {
        // Arrange
        var metadata = new ExtensionMetadata
        {
            ExtensionPath = "C:\\Extensions\\Test",
            ScriptFile = ""
        };

        // Act
        var fullPath = metadata.FullScriptPath;

        // Assert
        _ = fullPath.Should().Be("C:\\Extensions\\Test");
    }
}

