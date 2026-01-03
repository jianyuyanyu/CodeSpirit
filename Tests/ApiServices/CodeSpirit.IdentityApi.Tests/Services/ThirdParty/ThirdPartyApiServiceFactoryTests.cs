using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Models;
using CodeSpirit.IdentityApi.Services.ThirdParty;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Services.ThirdParty;

/// <summary>
/// 第三方API服务工厂测试
/// </summary>
public class ThirdPartyApiServiceFactoryTests
{
    private readonly Mock<WeChatApiService> _mockWeChatApiService;
    private readonly Mock<ILogger<ThirdPartyApiServiceFactory>> _mockLogger;
    private readonly ThirdPartyApiServiceFactory _factory;

    public ThirdPartyApiServiceFactoryTests()
    {
        _mockWeChatApiService = new Mock<WeChatApiService>(Mock.Of<ILogger<WeChatApiService>>());
        _mockLogger = new Mock<ILogger<ThirdPartyApiServiceFactory>>();
        _factory = new ThirdPartyApiServiceFactory(_mockWeChatApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetSessionAsync_微信平台_应该调用微信服务()
    {
        // Arrange
        var platformType = ThirdPartyPlatformType.WeChatMiniProgram;
        var credential = "test_code";
        var config = new ThirdPartyPlatformConfig
        {
            AppId = "test_appid",
            AppSecret = "test_secret"
        };

        var expectedSessionInfo = new ThirdPartySessionInfo
        {
            OpenId = "test_openid",
            UnionId = "test_unionid",
            SessionKey = "test_sessionkey"
        };

        _mockWeChatApiService
            .Setup(s => s.GetSessionAsync(platformType, credential, config))
            .ReturnsAsync(expectedSessionInfo);

        // Act
        var result = await _factory.GetSessionAsync(platformType, credential, config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSessionInfo.OpenId, result.OpenId);
        Assert.Equal(expectedSessionInfo.UnionId, result.UnionId);
        Assert.Equal(expectedSessionInfo.SessionKey, result.SessionKey);
        _mockWeChatApiService.Verify(s => s.GetSessionAsync(platformType, credential, config), Times.Once);
    }

    [Fact]
    public async Task GetSessionAsync_支付宝平台_应该抛出未实现异常()
    {
        // Arrange
        var platformType = ThirdPartyPlatformType.AlipayMiniProgram;
        var credential = "test_code";
        var config = new ThirdPartyPlatformConfig
        {
            AppId = "test_appid",
            AppSecret = "test_secret"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
            await _factory.GetSessionAsync(platformType, credential, config));
    }

    [Fact]
    public async Task GetSessionAsync_不支持的平台类型_应该抛出异常()
    {
        // Arrange
        var platformType = (ThirdPartyPlatformType)999; // 不存在的平台类型
        var credential = "test_code";
        var config = new ThirdPartyPlatformConfig
        {
            AppId = "test_appid",
            AppSecret = "test_secret"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await _factory.GetSessionAsync(platformType, credential, config));
    }
}

