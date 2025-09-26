using Xunit;

namespace mcp_nexus_tests
{
    /// <summary>
    /// Test collection definition to prevent parallel execution of notification tests
    /// This ensures test isolation and prevents flaky test behavior due to shared resources
    /// </summary>
    [CollectionDefinition("NotificationTestCollection", DisableParallelization = true)]
    public class NotificationTestCollection
    {
    }
}
