using Xunit;
using mcp_nexus.Startup;

namespace mcp_nexus.Tests.Startup
{
    /// <summary>
    /// Unit tests for ExceptionLogger class.
    /// </summary>
    public class ExceptionLoggerTests
    {
        [Fact]
        public void SetupGlobalExceptionHandlers_ExecutesWithoutException()
        {
            // Act & Assert - Should not throw
            ExceptionLogger.SetupGlobalExceptionHandlers();
        }

        [Fact]
        public void LogFatalException_WithNullException_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            ExceptionLogger.LogFatalException(null, "TestSource", false);
        }

        [Fact]
        public void LogFatalException_WithSimpleException_LogsDetails()
        {
            var exception = new InvalidOperationException("Test exception");
            // Act & Assert - Should not throw
            ExceptionLogger.LogFatalException(exception, "TestSource", false);
        }

        [Fact]
        public void LogFatalException_WithAggregateException_LogsAllInnerExceptions()
        {
            var innerExceptions = new Exception[]
            {
                new InvalidOperationException("First exception"),
                new ArgumentException("Second exception")
            };
            var aggregateException = new AggregateException("Multiple exceptions", innerExceptions);

            // Act & Assert - Should not throw
            ExceptionLogger.LogFatalException(aggregateException, "TestSource", false);
        }
    }
}

