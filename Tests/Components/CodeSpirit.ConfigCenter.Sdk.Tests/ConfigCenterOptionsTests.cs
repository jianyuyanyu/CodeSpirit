namespace CodeSpirit.ConfigCenter.Sdk.Tests;

/// <summary>
/// 配置中心选项测试
/// </summary>
public class ConfigCenterOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new ConfigCenterOptions();

        // Assert
        options.AppId.Should().BeNull();
        options.AutoRegister.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(60);
        options.ServiceUrl.Should().BeNull();
    }

    [Fact]
    public void AppId_CanBeSet()
    {
        // Arrange
        var expectedAppId = "my-custom-app";

        // Act
        var options = new ConfigCenterOptions { AppId = expectedAppId };

        // Assert
        options.AppId.Should().Be(expectedAppId);
    }

    [Fact]
    public void AutoRegister_CanBeDisabled()
    {
        // Act
        var options = new ConfigCenterOptions { AutoRegister = false };

        // Assert
        options.AutoRegister.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(1440)]
    public void CacheExpirationMinutes_AcceptsVariousValues(int minutes)
    {
        // Act
        var options = new ConfigCenterOptions { CacheExpirationMinutes = minutes };

        // Assert
        options.CacheExpirationMinutes.Should().Be(minutes);
    }

    [Fact]
    public void ServiceUrl_CanBeSet()
    {
        // Arrange
        var expectedUrl = "http://config-center:5000";

        // Act
        var options = new ConfigCenterOptions { ServiceUrl = expectedUrl };

        // Assert
        options.ServiceUrl.Should().Be(expectedUrl);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var appId = "test-app";
        var autoRegister = false;
        var cacheMinutes = 120;
        var serviceUrl = "https://config.example.com";

        // Act
        var options = new ConfigCenterOptions
        {
            AppId = appId,
            AutoRegister = autoRegister,
            CacheExpirationMinutes = cacheMinutes,
            ServiceUrl = serviceUrl
        };

        // Assert
        options.AppId.Should().Be(appId);
        options.AutoRegister.Should().Be(autoRegister);
        options.CacheExpirationMinutes.Should().Be(cacheMinutes);
        options.ServiceUrl.Should().Be(serviceUrl);
    }
}

