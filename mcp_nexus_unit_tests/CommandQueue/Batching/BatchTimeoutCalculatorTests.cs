using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.CommandQueue.Batching;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_unit_tests.CommandQueue.Batching
{
    /// <summary>
    /// Unit tests for BatchTimeoutCalculator
    /// </summary>
    public class BatchTimeoutCalculatorTests
    {
        #region Private Fields

        private readonly Mock<IOptions<BatchingConfiguration>> m_MockOptions;
        private readonly BatchingConfiguration m_Config;

        #endregion

        #region Constructor

        public BatchTimeoutCalculatorTests()
        {
            m_Config = new BatchingConfiguration
            {
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 30
            };

            m_MockOptions = new Mock<IOptions<BatchingConfiguration>>();
            m_MockOptions.Setup(x => x.Value).Returns(m_Config);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            var calculator = new BatchTimeoutCalculator(600000, m_MockOptions.Object);

            // Assert
            Assert.NotNull(calculator);
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BatchTimeoutCalculator(600000, null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Constructor_WithInvalidBaseTimeout_ShouldThrowArgumentOutOfRangeException(int baseTimeoutMs)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchTimeoutCalculator(baseTimeoutMs, m_MockOptions.Object));
        }

        #endregion

        #region CalculateBatchTimeout Tests

        [Fact]
        public void CalculateBatchTimeout_WithSingleCommand_ShouldReturnBaseTimeout()
        {
            // Arrange
            var calculator = new BatchTimeoutCalculator(600000, m_MockOptions.Object);
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("test-1", "lm")
            };

            // Act
            var timeout = calculator.CalculateBatchTimeout(commands);

            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(600000), timeout);
        }

        [Fact]
        public void CalculateBatchTimeout_WithMultipleCommands_ShouldMultiplyTimeout()
        {
            // Arrange
            var calculator = new BatchTimeoutCalculator(600000, m_MockOptions.Object);
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("test-1", "lm"),
                CreateTestCommand("test-2", "!threads"),
                CreateTestCommand("test-3", "!peb")
            };

            // Act
            var timeout = calculator.CalculateBatchTimeout(commands);

            // Assert
            // Expected: 600000ms * 3 commands * 1.0 multiplier = 1800000ms = 30 minutes (capped at MaxBatchTimeoutMinutes)
            Assert.Equal(TimeSpan.FromMinutes(30), timeout);
        }

        [Fact]
        public void CalculateBatchTimeout_WithMaxTimeoutExceeded_ShouldCapAtMaxTimeout()
        {
            // Arrange
            var calculator = new BatchTimeoutCalculator(600000, m_MockOptions.Object);
            var commands = new List<QueuedCommand>();
            for (int i = 0; i < 10; i++)
            {
                commands.Add(CreateTestCommand($"test-{i}", "lm"));
            }

            // Act
            var timeout = calculator.CalculateBatchTimeout(commands);

            // Assert
            // Should be capped at MaxBatchTimeoutMinutes (30 minutes)
            Assert.Equal(TimeSpan.FromMinutes(30), timeout);
        }

        [Fact]
        public void CalculateBatchTimeout_WithNullCommands_ShouldThrowArgumentNullException()
        {
            // Arrange
            var calculator = new BatchTimeoutCalculator(600000, m_MockOptions.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => calculator.CalculateBatchTimeout(null!));
        }

        [Fact]
        public void CalculateBatchTimeout_WithEmptyCommands_ShouldThrowArgumentException()
        {
            // Arrange
            var calculator = new BatchTimeoutCalculator(600000, m_MockOptions.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => calculator.CalculateBatchTimeout(new List<QueuedCommand>()));
        }

        #endregion

        #region Getter Tests

        [Fact]
        public void GetBaseTimeoutMs_ShouldReturnBaseTimeout()
        {
            // Arrange
            var calculator = new BatchTimeoutCalculator(600000, m_MockOptions.Object);

            // Act
            var baseTimeout = calculator.GetBaseTimeoutMs();

            // Assert
            Assert.Equal(600000, baseTimeout);
        }

        [Fact]
        public void GetMaxBatchTimeoutMinutes_ShouldReturnMaxTimeout()
        {
            // Arrange
            var calculator = new BatchTimeoutCalculator(600000, m_MockOptions.Object);

            // Act
            var maxTimeout = calculator.GetMaxBatchTimeoutMinutes();

            // Assert
            Assert.Equal(30, maxTimeout);
        }

        #endregion

        #region Helper Methods

        private static QueuedCommand CreateTestCommand(string id, string command)
        {
            return new QueuedCommand(
                id,
                command,
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource(),
                CommandState.Queued);
        }

        #endregion
    }
}
