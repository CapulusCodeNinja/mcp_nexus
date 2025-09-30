using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for CommandState enum
    /// </summary>
    public class CommandStateTests
    {
        [Fact]
        public void CommandState_EnumValues_AreCorrectlyDefined()
        {
            // Assert
            Assert.Equal(0, (int)CommandState.Queued);
            Assert.Equal(1, (int)CommandState.Executing);
            Assert.Equal(2, (int)CommandState.Completed);
            Assert.Equal(3, (int)CommandState.Cancelled);
            Assert.Equal(4, (int)CommandState.Failed);
        }

        [Fact]
        public void CommandState_EnumValues_CanBeConvertedToString()
        {
            // Assert
            Assert.Equal("Queued", CommandState.Queued.ToString());
            Assert.Equal("Executing", CommandState.Executing.ToString());
            Assert.Equal("Completed", CommandState.Completed.ToString());
            Assert.Equal("Cancelled", CommandState.Cancelled.ToString());
            Assert.Equal("Failed", CommandState.Failed.ToString());
        }

        [Fact]
        public void CommandState_EnumValues_CanBeParsedFromString()
        {
            // Act & Assert
            Assert.Equal(CommandState.Queued, Enum.Parse<CommandState>("Queued"));
            Assert.Equal(CommandState.Executing, Enum.Parse<CommandState>("Executing"));
            Assert.Equal(CommandState.Completed, Enum.Parse<CommandState>("Completed"));
            Assert.Equal(CommandState.Cancelled, Enum.Parse<CommandState>("Cancelled"));
            Assert.Equal(CommandState.Failed, Enum.Parse<CommandState>("Failed"));
        }

        [Fact]
        public void CommandState_EnumValues_CanBeParsedFromInt()
        {
            // Act & Assert
            Assert.Equal(CommandState.Queued, (CommandState)0);
            Assert.Equal(CommandState.Executing, (CommandState)1);
            Assert.Equal(CommandState.Completed, (CommandState)2);
            Assert.Equal(CommandState.Cancelled, (CommandState)3);
            Assert.Equal(CommandState.Failed, (CommandState)4);
        }

        [Theory]
        [InlineData(CommandState.Queued, CommandState.Executing)]
        [InlineData(CommandState.Executing, CommandState.Completed)]
        [InlineData(CommandState.Executing, CommandState.Cancelled)]
        [InlineData(CommandState.Executing, CommandState.Failed)]
        public void CommandState_CanBeCompared(CommandState state1, CommandState state2)
        {
            // Act & Assert
            Assert.NotEqual(state1, state2);
            Assert.True(state1 != state2);
            Assert.False(state1 == state2);
        }

        [Fact]
        public void CommandState_AllValues_CanBeEnumerated()
        {
            // Act
            var allValues = Enum.GetValues<CommandState>();

            // Assert
            Assert.Equal(5, allValues.Length);
            Assert.Contains(CommandState.Queued, allValues);
            Assert.Contains(CommandState.Executing, allValues);
            Assert.Contains(CommandState.Completed, allValues);
            Assert.Contains(CommandState.Cancelled, allValues);
            Assert.Contains(CommandState.Failed, allValues);
        }

        [Fact]
        public void CommandState_IsDefined_ReturnsTrueForValidValues()
        {
            // Act & Assert
            Assert.True(Enum.IsDefined(typeof(CommandState), CommandState.Queued));
            Assert.True(Enum.IsDefined(typeof(CommandState), CommandState.Executing));
            Assert.True(Enum.IsDefined(typeof(CommandState), CommandState.Completed));
            Assert.True(Enum.IsDefined(typeof(CommandState), CommandState.Cancelled));
            Assert.True(Enum.IsDefined(typeof(CommandState), CommandState.Failed));
        }

        [Fact]
        public void CommandState_IsDefined_ReturnsFalseForInvalidValues()
        {
            // Act & Assert
            Assert.False(Enum.IsDefined(typeof(CommandState), -1));
            Assert.False(Enum.IsDefined(typeof(CommandState), 5));
            Assert.False(Enum.IsDefined(typeof(CommandState), 999));
        }
    }
}