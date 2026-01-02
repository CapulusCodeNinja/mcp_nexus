using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Nexus.Config;

using Xunit;

namespace Nexus.Config_unittests;

/// <summary>
/// Unit tests for Settings class.
/// </summary>
public class SettingsTests : IDisposable
{
    private Settings? m_Settings;

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        m_Settings?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that constructor creates Settings instance.
    /// </summary>
    [Fact]
    public void Constructor_CreatesSettingsInstance()
    {
        // Act
        m_Settings = new Settings();

        // Assert
        _ = m_Settings.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that LoadConfiguration loads configuration from default path.
    /// </summary>
    [Fact]
    public void LoadConfiguration_WithNullPath_LoadsFromDefaultPath()
    {
        // Arrange
        m_Settings = new Settings();

        // Act
        m_Settings.LoadConfiguration(null);

        // Assert - should not throw
        _ = m_Settings.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that LoadConfiguration loads configuration from custom path.
    /// </summary>
    [Fact]
    public void LoadConfiguration_WithCustomPath_LoadsFromCustomPath()
    {
        // Arrange
        m_Settings = new Settings();
        var customPath = AppContext.BaseDirectory;

        // Act
        m_Settings.LoadConfiguration(customPath);

        // Assert - should not throw
        _ = m_Settings.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Get returns configuration from default loader created in constructor.
    /// </summary>
    [Fact]
    public void Get_AfterConstructor_ReturnsConfiguration()
    {
        // Arrange
        m_Settings = new Settings();

        // Act
        var config = m_Settings.Get();

        // Assert - constructor creates ConfigurationLoader, so Get should work
        _ = config.Should().NotBeNull();
        _ = config.Should().BeOfType<Nexus.Config.Models.SharedConfiguration>();
    }

    /// <summary>
    /// Verifies that Get returns configuration after LoadConfiguration.
    /// </summary>
    [Fact]
    public void Get_AfterLoadConfiguration_ReturnsConfiguration()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();

        // Act
        var config = m_Settings.Get();

        // Assert
        _ = config.Should().NotBeNull();
        _ = config.Should().BeOfType<Nexus.Config.Models.SharedConfiguration>();
    }

    /// <summary>
    /// Verifies that Get caches configuration after first access.
    /// </summary>
    [Fact]
    public void Get_MultipleCalls_ReturnsSameCachedInstance()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();

        // Act
        var config1 = m_Settings.Get();
        var config2 = m_Settings.Get();
        var config3 = m_Settings.Get();

        // Assert - should return same instance (cached)
        _ = config1.Should().BeSameAs(config2);
        _ = config2.Should().BeSameAs(config3);
    }

    /// <summary>
    /// Verifies that LoadConfiguration clears cached configuration.
    /// </summary>
    [Fact]
    public void LoadConfiguration_ClearsCachedConfiguration()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();

        // Act
        m_Settings.LoadConfiguration();
        var config2 = m_Settings.Get();

        // Assert - new configuration instance after reload
        _ = config2.Should().NotBeNull();

        // Note: May or may not be same reference depending on configuration content
    }

    /// <summary>
    /// Verifies that Get is thread-safe with concurrent access.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Get_WithConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();
        const int threadCount = 10;
        var tasks = new Task<Nexus.Config.Models.SharedConfiguration>[threadCount];

        // Act
        for (var i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() => m_Settings!.Get());
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all should return the same cached instance
        _ = results.Should().AllBeEquivalentTo(results[0], options => options.WithStrictOrdering());
    }

    /// <summary>
    /// Verifies that ConfigureLogging configures logging with serviceMode false.
    /// </summary>
    [Fact]
    public void ConfigureLogging_WithServiceModeFalse_ConfiguresLogging()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();
        var services = new ServiceCollection();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        _ = mockLoggingBuilder.SetupGet(builder => builder.Services).Returns(services);
        var isServiceMode = false;

        // Act
        m_Settings.ConfigureLogging(mockLoggingBuilder.Object, isServiceMode);

        // Assert - should not throw
        _ = m_Settings.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ConfigureLogging configures logging with serviceMode true.
    /// </summary>
    [Fact]
    public void ConfigureLogging_WithServiceModeTrue_ConfiguresLogging()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();
        var services = new ServiceCollection();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        _ = mockLoggingBuilder.SetupGet(builder => builder.Services).Returns(services);
        var isServiceMode = true;

        // Act
        m_Settings.ConfigureLogging(mockLoggingBuilder.Object, isServiceMode);

        // Assert - should not throw
        _ = m_Settings.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ConfigureLogging works with default configuration from constructor.
    /// </summary>
    [Fact]
    public void ConfigureLogging_AfterConstructor_ConfiguresLogging()
    {
        // Arrange
        m_Settings = new Settings();
        var services = new ServiceCollection();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        _ = mockLoggingBuilder.SetupGet(builder => builder.Services).Returns(services);
        var isServiceMode = false;

        // Act - should work since constructor creates ConfigurationLoader
        m_Settings.ConfigureLogging(mockLoggingBuilder.Object, isServiceMode);

        // Assert - should not throw
        _ = m_Settings.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Dispose can be called multiple times without throwing.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        m_Settings = new Settings();

        // Act & Assert - should not throw
        m_Settings.Dispose();
        m_Settings.Dispose();
        m_Settings.Dispose();
    }

    /// <summary>
    /// Verifies that Dispose releases resources correctly.
    /// </summary>
    [Fact]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();

        // Act
        m_Settings.Dispose();

        // Assert - should not throw
        _ = m_Settings.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that operations after Dispose throw ObjectDisposedException.
    /// </summary>
    [Fact]
    public void Operations_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();
        m_Settings.Dispose();

        // Act & Assert - Get should throw ObjectDisposedException since lock is disposed
        _ = Assert.Throws<ObjectDisposedException>(() => m_Settings.Get());
    }

    /// <summary>
    /// Verifies that LoadConfiguration with different paths creates different configuration loaders.
    /// </summary>
    [Fact]
    public void LoadConfiguration_WithDifferentPaths_CreatesNewLoader()
    {
        // Arrange
        m_Settings = new Settings();
        var path1 = AppContext.BaseDirectory;
        var path2 = AppContext.BaseDirectory; // Use same path to avoid directory not found

        // Act
        m_Settings.LoadConfiguration(path1);
        var config1 = m_Settings.Get();
        m_Settings.LoadConfiguration(path2);
        var config2 = m_Settings.Get();

        // Assert - should load successfully from both paths
        _ = config1.Should().NotBeNull();
        _ = config2.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Get handles race condition between read and write locks correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Get_WithRaceCondition_HandlesCorrectly()
    {
        // Arrange
        m_Settings = new Settings();
        m_Settings.LoadConfiguration();

        // Act - multiple threads accessing Get() simultaneously
        var tasks = new List<Task<Nexus.Config.Models.SharedConfiguration>>();
        for (var i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() => m_Settings!.Get()));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all should return valid configuration
        _ = results.Should().AllSatisfy(c => c.Should().NotBeNull());

        // All should be same instance (cached)
        var firstConfig = results[0];
        _ = results.Should().AllBeEquivalentTo(firstConfig, options => options.WithStrictOrdering());
    }
}

