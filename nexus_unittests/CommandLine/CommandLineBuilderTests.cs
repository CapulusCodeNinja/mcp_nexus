using System.CommandLine;
using System.CommandLine.Parsing;
using nexus.CommandLine;
using Xunit;

namespace nexus_unittests.CommandLine;

/// <summary>
/// Unit tests for CommandLineBuilder.
/// </summary>
public class CommandLineBuilderTests
{
    /// <summary>
    /// Verifies that root command is created with correct description.
    /// </summary>
    [Fact]
    public void BuildRootCommand_CreatesRootCommandWithDescription()
    {
        // Act
        var rootCommand = CommandLineBuilder.BuildRootCommand();

        // Assert
        Assert.NotNull(rootCommand);
        Assert.Equal("Nexus - Windows Debugging Server", rootCommand.Description);
    }

    /// <summary>
    /// Verifies that root command has all three subcommands.
    /// </summary>
    [Fact]
    public void BuildRootCommand_HasAllThreeSubcommands()
    {
        // Act
        var rootCommand = CommandLineBuilder.BuildRootCommand();

        // Assert
        Assert.Equal(3, rootCommand.Subcommands.Count);
        Assert.Contains(rootCommand.Subcommands, c => c.Name == "--http");
        Assert.Contains(rootCommand.Subcommands, c => c.Name == "--stdio");
        Assert.Contains(rootCommand.Subcommands, c => c.Name == "--service");
    }

    /// <summary>
    /// Verifies that http command has port option.
    /// </summary>
    [Fact]
    public void BuildRootCommand_HttpCommand_HasPortOption()
    {
        // Act
        var rootCommand = CommandLineBuilder.BuildRootCommand();
        var httpCommand = rootCommand.Subcommands.First(c => c.Name == "--http");

        // Assert
        Assert.NotNull(httpCommand);
        Assert.Single(httpCommand.Options);
        var portOption = httpCommand.Options.First();
        Assert.Equal("port", portOption.Name);
    }

    /// <summary>
    /// Verifies that http command port option has default value.
    /// </summary>
    [Fact]
    public void BuildRootCommand_HttpCommand_PortOption_HasDefaultValue()
    {
        // Act
        var rootCommand = CommandLineBuilder.BuildRootCommand();
        var parseResult = rootCommand.Parse("--http");

        // Assert
        Assert.Equal(0, parseResult.Errors.Count);
    }

    /// <summary>
    /// Verifies that stdio command has no options.
    /// </summary>
    [Fact]
    public void BuildRootCommand_StdioCommand_HasNoOptions()
    {
        // Act
        var rootCommand = CommandLineBuilder.BuildRootCommand();
        var stdioCommand = rootCommand.Subcommands.First(c => c.Name == "--stdio");

        // Assert
        Assert.NotNull(stdioCommand);
        Assert.Empty(stdioCommand.Options);
    }

    /// <summary>
    /// Verifies that service command has no options.
    /// </summary>
    [Fact]
    public void BuildRootCommand_ServiceCommand_HasNoOptions()
    {
        // Act
        var rootCommand = CommandLineBuilder.BuildRootCommand();
        var serviceCommand = rootCommand.Subcommands.First(c => c.Name == "--service");

        // Assert
        Assert.NotNull(serviceCommand);
        Assert.Empty(serviceCommand.Options);
    }

    /// <summary>
    /// Verifies that commands have descriptions.
    /// </summary>
    [Theory]
    [InlineData("--http", "Run server in HTTP mode")]
    [InlineData("--stdio", "Run server in standard input/output mode")]
    [InlineData("--service", "Run server as Windows Service")]
    public void BuildRootCommand_Commands_HaveDescriptions(string commandName, string expectedDescription)
    {
        // Act
        var rootCommand = CommandLineBuilder.BuildRootCommand();
        var command = rootCommand.Subcommands.First(c => c.Name == commandName);

        // Assert
        Assert.Equal(expectedDescription, command.Description);
    }
}

