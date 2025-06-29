using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CodeSpirit.UdlCards.Builders;
using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Extensions;
using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Tests;

/// <summary>
/// ServiceCollectionExtensions 单元测试
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddUdlCards_WithoutOptions_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUdlCards();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<UdlCardsGenerator>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<UdlCardsOptions>>().Should().NotBeNull();
        
        // 验证所有建构器都已注册
        serviceProvider.GetService<IUdlCardBuilder<StatCardConfig>>().Should().NotBeNull();
        serviceProvider.GetService<IUdlCardBuilder<ChartCardConfig>>().Should().NotBeNull();
        serviceProvider.GetService<IUdlCardBuilder<TableCardConfig>>().Should().NotBeNull();
        serviceProvider.GetService<IUdlCardBuilder<InfoCardConfig>>().Should().NotBeNull();
        serviceProvider.GetService<IUdlCardBuilder<InfoGridCardConfig>>().Should().NotBeNull();
    }

    [Fact]
    public void AddUdlCards_WithConfiguration_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["UdlCards:DefaultTheme"] = "dark",
                ["UdlCards:EnableCaching"] = "true",
                ["UdlCards:MaxCardsPerPage"] = "20"
            })
            .Build();

        // Act
        services.AddUdlCards(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<UdlCardsOptions>>().Value;
        options.DefaultTheme.Should().Be("dark");
        options.EnableCaching.Should().BeTrue();
        options.MaxCardsPerPage.Should().Be(20);
    }

    [Fact]
    public void AddUdlCards_WithOptionsDelegate_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUdlCards(options =>
        {
            options.DefaultTheme = "primary";
            options.EnableCaching = true;
            options.MaxCardsPerPage = 15;
            options.StrictMode = true;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<UdlCardsOptions>>().Value;
        options.DefaultTheme.Should().Be("primary");
        options.EnableCaching.Should().BeTrue();
        options.MaxCardsPerPage.Should().Be(15);
        options.StrictMode.Should().BeTrue();
    }

    [Fact]
    public void AddUdlCards_MultipleCalls_ShouldNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUdlCards();
        services.AddUdlCards();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var generators = serviceProvider.GetServices<UdlCardsGenerator>().ToList();
        generators.Should().HaveCount(1, "不应该重复注册服务");
    }

    [Fact]
    public void AddUdlCardBuilder_ShouldRegisterCustomBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddUdlCards();
        services.AddUdlCardBuilder<StatCardConfig, StatCardBuilder>();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var builder = serviceProvider.GetService<IUdlCardBuilder<StatCardConfig>>();
        builder.Should().NotBeNull();
        builder.Should().BeOfType<StatCardBuilder>();
    }

    [Fact]
    public void RegisteredBuilders_ShouldImplementCorrectInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUdlCards();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var statBuilder = serviceProvider.GetService<IUdlCardBuilder<StatCardConfig>>();
        statBuilder.Should().NotBeNull();
        statBuilder.Should().BeAssignableTo<IUdlCardBuilderBase>();

        var chartBuilder = serviceProvider.GetService<IUdlCardBuilder<ChartCardConfig>>();
        chartBuilder.Should().NotBeNull();
        chartBuilder.Should().BeAssignableTo<IUdlCardBuilderBase>();

        var tableBuilder = serviceProvider.GetService<IUdlCardBuilder<TableCardConfig>>();
        tableBuilder.Should().NotBeNull();
        tableBuilder.Should().BeAssignableTo<IUdlCardBuilderBase>();
    }

    [Fact]
    public void UdlCardsGenerator_ShouldReceiveAllBuilders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUdlCards();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var generator = serviceProvider.GetRequiredService<UdlCardsGenerator>();

        // Assert
        generator.Should().NotBeNull();
        
        // 通过尝试生成不同类型的卡片来验证建构器已正确注入
        var statCard = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计卡片",
            Data = new StatDataConfig { Value = 100, Label = "测试" }
        };

        var result = generator.GenerateCard(statCard);
        result.Should().NotBeNull();
        result["type"].Should().Be("stat");
    }
} 