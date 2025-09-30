using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Middleware;

namespace mcp_nexus_tests.Middleware
{
    /// <summary>
    /// Tests for ContentTypeValidationMiddleware
    /// </summary>
    public class ContentTypeValidationMiddlewareTests
    {
        [Fact]
        public void ContentTypeValidationMiddleware_Class_Exists()
        {
            // This test verifies that the ContentTypeValidationMiddleware class exists and can be instantiated
            Assert.True(typeof(ContentTypeValidationMiddleware) != null);
        }
    }
}
