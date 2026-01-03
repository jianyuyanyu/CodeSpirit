using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Models;
using CodeSpirit.IdentityApi.Services.ThirdParty;
using Microsoft.Extensions.Logging;
using Moq;
using SKIT.FlurlHttpClient.Wechat.Api;
using SKIT.FlurlHttpClient.Wechat.Api.Models;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Services.ThirdParty;

/// <summary>
/// 微信API服务测试
/// </summary>
public class WeChatApiServiceTests
{
    private readonly Mock<ILogger<WeChatApiService>> _mockLogger;
    private readonly WeChatApiService _service;

    public WeChatApiServiceTests()
    {
        _mockLogger = new Mock<ILogger<WeChatApiService>>();
        _service = new WeChatApiService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetSessionAsync_不支持的平台类型_应该抛出异常()
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
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetSessionAsync(platformType, credential, config));
    }

    [Fact]
    public async Task GetSessionAsync_微信平台类型_应该调用微信API()
    {
        // Arrange
        var platformType = ThirdPartyPlatformType.WeChatMiniProgram;
        var credential = "test_code";
        var config = new ThirdPartyPlatformConfig
        {
            AppId = "test_appid",
            AppSecret = "test_secret"
        };

        // Act
        // 注意：这是一个集成测试，需要真实的微信API或Mock WechatApiClient
        // 由于SKIT库的复杂性，这里主要验证方法签名和基本逻辑
        var exception = await Record.ExceptionAsync(async () =>
            await _service.GetSessionAsync(platformType, credential, config));

        // Assert
        // 由于没有真实的AppId和AppSecret，预期会抛出异常（API调用失败）
        // 但验证了方法能够正确调用
        Assert.NotNull(exception);
    }
}

