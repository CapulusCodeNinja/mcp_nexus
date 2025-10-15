using Xunit;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    /// <summary>
    /// Tests for DefaultProcessMemoryProvider.
    /// </summary>
    public class DefaultProcessMemoryProviderTests
    {
        [Fact]
        public void PrivateBytes_ShouldReturnPositiveValue()
        {
            // Arrange
            var provider = new DefaultProcessMemoryProvider();

            // Act
            var privateBytes = provider.PrivateBytes;

            // Assert
            Assert.True(privateBytes > 0, "Process memory should be greater than zero");
            // Sanity check: memory should be less than 10 GB (reasonable for a test process)
            Assert.True(privateBytes < 10L * 1024 * 1024 * 1024, "Process memory should be less than 10 GB");
        }

        [Fact]
        public void PrivateBytes_ShouldReturnConsistentValue()
        {
            // Arrange
            var provider = new DefaultProcessMemoryProvider();

            // Act
            var value1 = provider.PrivateBytes;
            var value2 = provider.PrivateBytes;

            // Assert
            // Values should be similar (within 10 MB difference)
            var diff = Math.Abs(value1 - value2);
            Assert.True(diff < 10 * 1024 * 1024, $"Memory values should be consistent: {value1} vs {value2}");
        }

        [Fact]
        public void PrivateBytes_ShouldReflectMemoryGrowth()
        {
            // Arrange
            var provider = new DefaultProcessMemoryProvider();
            var initialMemory = provider.PrivateBytes;

            // Act - Allocate memory
            var largeArray = new byte[1024 * 1024]; // 1 MB
            for (int i = 0; i < largeArray.Length; i++)
            {
                largeArray[i] = (byte)(i % 256);
            }
            GC.KeepAlive(largeArray); // Ensure it's not optimized away

            var afterAllocation = provider.PrivateBytes;

            // Assert
            Assert.True(afterAllocation >= initialMemory,
                $"Memory after allocation ({afterAllocation}) should be >= initial memory ({initialMemory})");
        }

        [Fact]
        public void PrivateBytes_MultipleProviders_ShouldReturnSameProcess()
        {
            // Arrange
            var provider1 = new DefaultProcessMemoryProvider();
            var provider2 = new DefaultProcessMemoryProvider();

            // Act
            var memory1 = provider1.PrivateBytes;
            var memory2 = provider2.PrivateBytes;

            // Assert
            // Both should measure the same process, so values should be similar
            var diff = Math.Abs(memory1 - memory2);
            Assert.True(diff < 10 * 1024 * 1024,
                $"Both providers should measure same process: {memory1} vs {memory2}");
        }

        [Fact]
        public void PrivateBytes_ShouldImplementInterface()
        {
            // Arrange & Act
            IProcessMemoryProvider provider = new DefaultProcessMemoryProvider();

            // Assert
            Assert.NotNull(provider);
            Assert.True(provider.PrivateBytes > 0);
        }
    }
}

