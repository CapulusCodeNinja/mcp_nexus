using nexus.protocol.Configuration;

namespace nexus.protocol.unittests.Configuration;

/// <summary>
/// Unit tests for HttpServerConfiguration class.
/// Tests configuration validation and default values.
/// </summary>
public class HttpServerConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var config = new HttpServerConfiguration();

        config.MaxRequestBodySize.Should().Be(50 * 1024 * 1024); // 50MB
        config.RequestHeadersTimeoutSeconds.Should().Be(60);
        config.KeepAliveTimeoutSeconds.Should().Be(120);
        config.MaxRequestLineSize.Should().Be(8192); // 8KB
        config.MaxRequestHeadersTotalSize.Should().Be(32768); // 32KB
        config.EnableCors.Should().BeTrue();
        config.EnableRateLimit.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDefaultValues_DoesNotThrow()
    {
        var config = new HttpServerConfiguration();

        var action = () => config.Validate();

        action.Should().NotThrow();
    }

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
            EnableRateLimit = false
        };

        var action = () => config.Validate();

        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithZeroMaxRequestBodySize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestBodySize = 0 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithMessage("*MaxRequestBodySize*")
            .WithParameterName("MaxRequestBodySize");
    }

    [Fact]
    public void Validate_WithNegativeMaxRequestBodySize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestBodySize = -1 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestBodySize");
    }

    [Fact]
    public void Validate_WithZeroRequestHeadersTimeout_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { RequestHeadersTimeoutSeconds = 0 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("RequestHeadersTimeoutSeconds");
    }

    [Fact]
    public void Validate_WithNegativeRequestHeadersTimeout_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { RequestHeadersTimeoutSeconds = -1 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("RequestHeadersTimeoutSeconds");
    }

    [Fact]
    public void Validate_WithZeroKeepAliveTimeout_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { KeepAliveTimeoutSeconds = 0 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("KeepAliveTimeoutSeconds");
    }

    [Fact]
    public void Validate_WithNegativeKeepAliveTimeout_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { KeepAliveTimeoutSeconds = -1 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("KeepAliveTimeoutSeconds");
    }

    [Fact]
    public void Validate_WithZeroMaxRequestLineSize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestLineSize = 0 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestLineSize");
    }

    [Fact]
    public void Validate_WithNegativeMaxRequestLineSize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestLineSize = -1 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestLineSize");
    }

    [Fact]
    public void Validate_WithZeroMaxRequestHeadersTotalSize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestHeadersTotalSize = 0 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestHeadersTotalSize");
    }

    [Fact]
    public void Validate_WithNegativeMaxRequestHeadersTotalSize_ThrowsArgumentException()
    {
        var config = new HttpServerConfiguration { MaxRequestHeadersTotalSize = -1 };

        var action = () => config.Validate();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("MaxRequestHeadersTotalSize");
    }

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
            EnableRateLimit = false
        };

        config.MaxRequestBodySize.Should().Be(123456);
        config.RequestHeadersTimeoutSeconds.Should().Be(45);
        config.KeepAliveTimeoutSeconds.Should().Be(90);
        config.MaxRequestLineSize.Should().Be(4096);
        config.MaxRequestHeadersTotalSize.Should().Be(16384);
        config.EnableCors.Should().BeFalse();
        config.EnableRateLimit.Should().BeFalse();
    }
}

