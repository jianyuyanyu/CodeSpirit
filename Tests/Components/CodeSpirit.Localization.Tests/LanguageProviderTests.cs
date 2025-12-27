using CodeSpirit.Core;
using CodeSpirit.Localization.Constants;
using CodeSpirit.Localization.Models;
using CodeSpirit.Localization.Providers;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Settings.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CodeSpirit.Localization.Tests;

/// <summary>
/// 语言提供者测试
/// </summary>
public class LanguageProviderTests
{
    [Fact]
    public async Task CookieLanguageProvider_ShouldReturnLanguageFromCookie()
    {
        // Arrange
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = ".AspNetCore.Culture=c=en|uic=en";
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var provider = new CookieLanguageProvider(httpContextAccessor.Object);

        // Act
        var language = await provider.GetLanguageAsync();

        // Assert
        language.Should().Be("en");
    }

    [Fact]
    public async Task UserSettingsLanguageProvider_ShouldReturnUserPreferredLanguage()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();
        var currentUser = new Mock<ICurrentUser>();
        var options = Options.Create(new Models.LocalizationOptions());

        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Id).Returns(1L);
        
        settingsService.Setup(x => x.GetUserSettingAsync(
            CultureConstants.SettingsModule,
            CultureConstants.UserPreferredLanguageKey,
            "1"))
            .ReturnsAsync("en");

        var provider = new UserSettingsLanguageProvider(
            settingsService.Object, 
            currentUser.Object, 
            options);

        // Act
        var language = await provider.GetLanguageAsync();

        // Assert
        language.Should().Be("en");
    }

    [Fact]
    public async Task CompositeLanguageProvider_ShouldRespectPriority()
    {
        // Arrange
        var provider1 = new Mock<ILanguageProvider>();
        var provider2 = new Mock<ILanguageProvider>();
        var provider3 = new Mock<ILanguageProvider>();

        provider1.Setup(x => x.GetLanguageAsync()).ReturnsAsync((string?)null);
        provider2.Setup(x => x.GetLanguageAsync()).ReturnsAsync("en");
        provider3.Setup(x => x.GetLanguageAsync()).ReturnsAsync("zh-CN");

        var compositeProvider = new CompositeLanguageProvider(new[] { provider1.Object, provider2.Object, provider3.Object });

        // Act
        var language = await compositeProvider.GetLanguageAsync();

        // Assert
        language.Should().Be("en"); // 应该返回第一个非空值
    }

    [Fact]
    public async Task GlobalSettingsLanguageProvider_ShouldReturnDefaultLanguage()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();
        var options = Options.Create(new Models.LocalizationOptions 
        { 
            DefaultCulture = "zh-CN" 
        });

        settingsService.Setup(x => x.GetGlobalSettingAsync(
            CultureConstants.SettingsModule,
            CultureConstants.GlobalDefaultLanguageKey))
            .ReturnsAsync((string?)null);

        var provider = new GlobalSettingsLanguageProvider(
            settingsService.Object, 
            options);

        // Act
        var language = await provider.GetLanguageAsync();

        // Assert
        language.Should().Be("zh-CN"); // 应该回退到默认语言
    }
}
