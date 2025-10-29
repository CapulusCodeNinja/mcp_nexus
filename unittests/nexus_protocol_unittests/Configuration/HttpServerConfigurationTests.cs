using FluentAssertions;

using Nexus.Protocol.Configuration;

using Xunit;

namespace Nexus.Protocol.Unittests.Configuration;

/// <summary>
/// Unit tests for HttpServerConfiguration class.
/// Tests configuration validation and default values.
/// </summary>
public class HttpServerConfigurationTests
{
    /// <summary>
    /// Verifies that HttpServerConfiguration constructor sets default values correctly.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var config = new HttpServerConfiguration();

        _ = config.MaxRequestBodySize.Should().Be(50 * 1024 * 1024); // 50MB
        _ = config.RequestHeadersTimeoutSeconds.Should().Be(60);
        _ = config.KeepAliveTimeoutSeconds.Should().Be(120);
        _ = config.MaxRequestLineSize.Should().Be(8192); // 8KB
        _ = config.MaxRequestHeadersTotalSize.Should().Be(32768); // 32KB
        _ = config.EnableCors.Should().BeTrue();
        _ = config.EnableRateLimit.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Validate does not throw with default configuration values.
    /// </summary>
    [Fact]
    public void Validate_WithDefaultValues_DoesNotThrow()
    {
        var config = new HttpServerConfiguration();

        var action = () => config.Validate();

        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Validate does not throw with valid custom configuration values.
    /// </summary>
    [Fact]
    public void Validate_WithValidCustomValues_DoesNotThrow()
    {
        var config = new HttpServerConfiguration
        {
            MaxRequestBodySize = 100 * 1024 * 1024,
            RequestHeadersTimeoutSeconds = 30,
            KeepAliveTimeoutSeconds = 60,
            MaxRequestLineSize = 4096,
            MaxRequestHeadersTotalSize = 16384,
            EnableCors = false,
            EnableRateLimit = false,
        };

        var action = () => config.Validate();

        _ = action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when MaxRequestBodySize is zero.
    /// </summary>
    [Fact]
    public void Validate_WithZeroMaxRequestBodySize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestBodySize = 0 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithMessage("*MaxRequestBodySize*")
            .WithParameterName("MaxRequestBodySize");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when MaxRequestBodySize is negative.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeMaxRequestBodySize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestBodySize = -1 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestBodySize");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when RequestHeadersTimeoutSeconds is zero.
    /// </summary>
    [Fact]
    public void Validate_WithZeroRequestHeadersTimeout_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { RequestHeadersTimeoutSeconds = 0 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("RequestHeadersTimeoutSeconds");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when RequestHeadersTimeoutSeconds is negative.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeRequestHeadersTimeout_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { RequestHeadersTimeoutSeconds = -1 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("RequestHeadersTimeoutSeconds");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when KeepAliveTimeoutSeconds is zero.
    /// </summary>
    [Fact]
    public void Validate_WithZeroKeepAliveTimeout_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { KeepAliveTimeoutSeconds = 0 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("KeepAliveTimeoutSeconds");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when KeepAliveTimeoutSeconds is negative.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeKeepAliveTimeout_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { KeepAliveTimeoutSeconds = -1 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("KeepAliveTimeoutSeconds");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when MaxRequestLineSize is zero.
    /// </summary>
    [Fact]
    public void Validate_WithZeroMaxRequestLineSize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestLineSize = 0 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestLineSize");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when MaxRequestLineSize is negative.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeMaxRequestLineSize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestLineSize = -1 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestLineSize");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when MaxRequestHeadersTotalSize is zero.
    /// </summary>
    [Fact]
    public void Validate_WithZeroMaxRequestHeadersTotalSize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestHeadersTotalSize = 0 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestHeadersTotalSize");
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException when MaxRequestHeadersTotalSize is negative.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeMaxRequestHeadersTotalSize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestHeadersTotalSize = -1 };

        var action = () => config.Validate();

        _ = action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestHeadersTotalSize");
    }

    /// <summary>
    /// Verifies that all properties can be set and retrieved correctly.
    /// </summary>
    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var config = new HttpServerConfiguration
        {
            MaxRequestBodySize = 123456,
            RequestHeadersTimeoutSeconds = 45,
            KeepAliveTimeoutSeconds = 90,
            MaxRequestLineSize = 4096,
            MaxRequestHeadersTotalSize = 16384,
            EnableCors = false,
            EnableRateLimit = false,
        };

        _ = config.MaxRequestBodySize.Should().Be(123456);
        _ = config.RequestHeadersTimeoutSeconds.Should().Be(45);
        _ = config.KeepAliveTimeoutSeconds.Should().Be(90);
        _ = config.MaxRequestLineSize.Should().Be(4096);
        _ = config.MaxRequestHeadersTotalSize.Should().Be(16384);
        _ = config.EnableCors.Should().BeFalse();
        _ = config.EnableRateLimit.Should().BeFalse();
    }
}
