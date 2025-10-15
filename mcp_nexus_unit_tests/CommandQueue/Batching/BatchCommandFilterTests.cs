using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.CommandQueue.Batching;

namespace mcp_nexus_unit_tests.CommandQueue.Batching
{
    /// <summary>
    /// Unit tests for BatchCommandFilter
    /// </summary>
    public class BatchCommandFilterTests
    {
        #region Private Fields

        private readonly Mock<IOptions<BatchingConfiguration>> m_MockOptions;
        private readonly BatchingConfiguration m_Config;

        #endregion

        #region Constructor

        public BatchCommandFilterTests()
        {
            m_Config = new BatchingConfiguration
            {
                ExcludedCommands = new[] { "!analyze", "!dump", "!heap", "~*k" }
            };

            m_MockOptions = new Mock<IOptions<BatchingConfiguration>>();
            m_MockOptions.Setup(x => x.Value).Returns(m_Config);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidOptions_ShouldCreateInstance()
        {
            // Act
            var filter = new BatchCommandFilter(m_MockOptions.Object);

            // Assert
            Assert.NotNull(filter);
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BatchCommandFilter(null!));
        }

        #endregion

        #region CanBatchCommand Tests

        [Theory]
        [InlineData("lm", true)]
        [InlineData("!analyze", false)]
        [InlineData("!analyze -v", false)]
        [InlineData("!dump", false)]
        [InlineData("!heap", false)]
        [InlineData("~*k", false)]
        [InlineData("!threads", true)]
        [InlineData("!peb", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void CanBatchCommand_WithVariousCommands_ShouldReturnExpectedResult(string? command, bool expectedResult)
        {
            // Arrange
            var filter = new BatchCommandFilter(m_MockOptions.Object);

            // Act
            var result = filter.CanBatchCommand(command!);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void CanBatchCommand_WithCaseInsensitiveExcludedCommand_ShouldReturnFalse()
        {
            // Arrange
            var filter = new BatchCommandFilter(m_MockOptions.Object);

            // Act
            var result = filter.CanBatchCommand("!ANALYZE");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetExcludedCommands Tests

        [Fact]
        public void GetExcludedCommands_ShouldReturnExcludedCommands()
        {
            // Arrange
            var filter = new BatchCommandFilter(m_MockOptions.Object);

            // Act
            var excludedCommands = filter.GetExcludedCommands();

            // Assert
            Assert.NotNull(excludedCommands);
            Assert.Equal(m_Config.ExcludedCommands.Length, excludedCommands.Count);
            Assert.Contains("!analyze", excludedCommands);
            Assert.Contains("!dump", excludedCommands);
        }

        #endregion
    }
}
