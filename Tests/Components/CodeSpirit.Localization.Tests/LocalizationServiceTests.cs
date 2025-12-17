using CodeSpirit.Localization.Models;
using CodeSpirit.Localization.Providers;
using CodeSpirit.Localization.Services;
using CodeSpirit.Settings.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CodeSpirit.Localization.Tests;

/// <summary>
/// 本地化服务测试
/// </summary>
public class LocalizationServiceTests
{
    [Fact]
    public async Task GetCurrentLanguageAsync_ShouldReturnLanguageFromProvider()
    {
        // Arrange
        var languageProvider = new Mock<ILanguageProvider>();
        var settingsService = new Mock<ISettingsService>();
        var options = Options.Create(new Models.LocalizationOptions());

        languageProvider.Setup(x => x.GetLanguageAsync()).ReturnsAsync("en");

        var service = new LocalizationService(
            languageProvider.Object, 
            settingsService.Object, 
            options);

        // Act
        var language = await service.GetCurrentLanguageAsync();

        // Assert
        language.Should().Be("en");
    }

    [Fact]
    public async Task GetCurrentLanguageAsync_ShouldReturnDefaultWhenProviderReturnsNull()
    {
        // Arrange
        var languageProvider = new Mock<ILanguageProvider>();
        var settingsService = new Mock<ISettingsService>();
        var options = Options.Create(new Models.LocalizationOptions 
        { 
            DefaultCulture = "zh-CN" 
        });

        languageProvider.Setup(x => x.GetLanguageAsync()).ReturnsAsync((string?)null);

        var service = new LocalizationService(
            languageProvider.Object, 
            settingsService.Object, 
            options);

        // Act
        var language = await service.GetCurrentLanguageAsync();

        // Assert
        language.Should().Be("zh-CN");
    }

    [Fact]
    public async Task SetUserLanguageAsync_ShouldCallSettingsService()
    {
        // Arrange
        var languageProvider = new Mock<ILanguageProvider>();
        var settingsService = new Mock<ISettingsService>();
        var options = Options.Create(new Models.LocalizationOptions 
        { 
            EnableUserLevelLanguage = true 
        });

        settingsService.Setup(x => x.SetUserSettingAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var service = new LocalizationService(
            languageProvider.Object, 
            settingsService.Object, 
            options);

        // Act
        var result = await service.SetUserLanguageAsync("user123", "en");

        // Assert
        result.Should().BeTrue();
        settingsService.Verify(x => x.SetUserSettingAsync(
            "Localization", "PreferredLanguage", "en", "user123", It.IsAny<string>()), 
            Times.Once);
    }
}
