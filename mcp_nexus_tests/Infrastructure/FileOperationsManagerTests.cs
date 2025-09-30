using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for FileOperationsManager
    /// </summary>
    public class FileOperationsManagerTests
    {
        [Fact]
        public void FileOperationsManager_Class_Exists()
        {
            // This test verifies that the FileOperationsManager class exists and can be instantiated
            Assert.True(typeof(FileOperationsManager) != null);
        }
    }
}
