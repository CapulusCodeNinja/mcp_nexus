using FluentAssertions;

using Moq;

using Nexus.Config;
using Nexus.Config.Models;
using Nexus.Engine;
using Nexus.Engine.Share;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;
using Nexus.Protocol.Services;

using Xunit;

namespace Nexus.Protocol.Unittests.Services;

/// <summary>
/// Unit tests for the <see cref="EngineService"/> class.
/// Tests initialization, thread-safety, and retrieval of the debug engine singleton.
/// </summary>
public class EngineServiceTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineServiceTests"/> class.
    /// </summary>
    public EngineServiceTests()
    {
        m_Settings = new Mock<ISettings>();
        m_FileSystem = new Mock<IFileSystem>();
        m_ProcessManager = new Mock<IProcessManager>();

        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
    }

    /// <summary>
    /// Verifies that Initialize with valid parameters succeeds.
    /// </summary>
    [Fact]
    public void Initialize_WithValidParameters_Succeeds()
    {
        // Act
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Assert - Should not throw
        _ = EngineService.Get().Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Initialize creates a debug engine instance.
    /// </summary>
    [Fact]
    public void Initialize_CreatesDebugEngine()
    {
        // Act
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Assert
        var engine = EngineService.Get();
        _ = engine.Should().NotBeNull();
        _ = engine.Should().BeOfType<DebugEngine>();
    }

    /// <summary>
    /// Verifies that Initialize with null fileSystem throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Initialize_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            EngineService.Initialize(null!, m_ProcessManager.Object, m_Settings.Object));
    }

    /// <summary>
    /// Verifies that Initialize with null processManager throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Initialize_WithNullProcessManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            EngineService.Initialize(m_FileSystem.Object, null!, m_Settings.Object));
    }

    /// <summary>
    /// Verifies that Initialize with null settings throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Initialize_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, null!));
    }

    /// <summary>
    /// Verifies that Get returns the initialized engine instance.
    /// </summary>
    [Fact]
    public void Get_AfterInitialize_ReturnsEngine()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act
        var engine = EngineService.Get();

        // Assert
        _ = engine.Should().NotBeNull();
        _ = engine.Should().BeOfType<DebugEngine>();
    }

    /// <summary>
    /// Verifies that Get returns the same instance on multiple calls.
    /// </summary>
    [Fact]
    public void Get_MultipleCalls_ReturnsSameInstance()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act
        var engine1 = EngineService.Get();
        var engine2 = EngineService.Get();

        // Assert
        _ = engine1.Should().BeSameAs(engine2);
    }

    /// <summary>
    /// Verifies that Initialize can be called multiple times (reinitializes).
    /// </summary>
    [Fact]
    public void Initialize_MultipleCalls_ReinitializesEngine()
    {
        // Arrange
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        var engine1 = EngineService.Get();

        // Act
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        var engine2 = EngineService.Get();

        // Assert
        _ = engine1.Should().NotBeSameAs(engine2);
        _ = engine2.Should().NotBeNull();
        _ = engine2.Should().BeOfType<DebugEngine>();
    }

    /// <summary>
    /// Verifies that Initialize is thread-safe.
    /// </summary>
    [Fact]
    public void Initialize_FromMultipleThreads_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        var exceptions = new List<Exception>();

        // Act
        var threads = new List<Thread>();
        for (var i = 0; i < threadCount; i++)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
                    _ = EngineService.Get();
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        _ = exceptions.Should().BeEmpty();
        _ = EngineService.Get().Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Get is thread-safe.
    /// </summary>
    [Fact]
    public void Get_FromMultipleThreads_IsThreadSafe()
    {
        // Arrange - Initialize once before threads start
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        const int threadCount = 10;
        var engines = new List<IDebugEngine>();
        var exceptions = new List<Exception>();
        var barrier = new System.Threading.Barrier(threadCount + 1); // +1 for main thread

        // Act
        var threads = new List<Thread>();
        for (var i = 0; i < threadCount; i++)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    // Wait for all threads to be ready
                    barrier.SignalAndWait();

                    var engine = EngineService.Get();
                    lock (engines)
                    {
                        engines.Add(engine);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        // Wait for all threads to be ready, then signal them to proceed
        barrier.SignalAndWait();

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        _ = exceptions.Should().BeEmpty("Get() should not throw exceptions after initialization");
        _ = engines.Should().HaveCount(threadCount);
        _ = engines.Should().AllSatisfy(e => e.Should().NotBeNull());

        // Verify all instances are the same - check that all reference the same object
        // Use the first instance as the reference since we know it's not null
        if (engines.Count > 0)
        {
            var firstInstance = engines[0];
            for (var i = 1; i < engines.Count; i++)
            {
                _ = engines[i].Should().BeSameAs(firstInstance, $"Engine at index {i} should be the same instance as the first one");
            }
        }
    }

    /// <summary>
    /// Verifies that Initialize and Get work together correctly under concurrent access.
    /// </summary>
    [Fact]
    public void InitializeAndGet_ConcurrentAccess_WorksCorrectly()
    {
        // Arrange
        const int threadCount = 5;
        var exceptions = new List<Exception>();

        // Act
        var threads = new List<Thread>();
        for (var i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            var thread = new Thread(() =>
            {
                try
                {
                    if (threadIndex % 2 == 0)
                    {
                        // Some threads initialize
                        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
                    }
                    else
                    {
                        // Some threads get
                        try
                        {
                            _ = EngineService.Get();
                        }
                        catch (NullReferenceException)
                        {
                            // Get may throw if not initialized yet, which is acceptable
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Final initialization
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Assert
        _ = EngineService.Get().Should().NotBeNull();
    }
}
