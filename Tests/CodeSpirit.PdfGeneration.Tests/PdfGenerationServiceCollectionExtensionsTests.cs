using CodeSpirit.PdfGeneration.Extensions;
using CodeSpirit.PdfGeneration.Options;
using CodeSpirit.PdfGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeSpirit.PdfGeneration.Tests;

public class PdfGenerationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPdfGeneration_WithConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // 添加日志服务
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"PdfGeneration:MaxConcurrentJobs", "3"},
                {"PdfGeneration:BrowserPoolSize", "2"},
                {"PdfGeneration:BrowserTimeout", "00:01:00"},
                {"PdfGeneration:Headless", "true"},
                {"PdfGeneration:RetryCount", "2"}
            })
            .Build();

        // Act
        services.AddPdfGeneration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var pdfService = serviceProvider.GetService<IPdfGenerationService>();
        Assert.NotNull(pdfService);
        Assert.IsType<PdfGenerationService>(pdfService);
    }

    [Fact]
    public void AddPdfGeneration_WithOptions_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // 添加日志服务
        services.AddLogging();
        
        // Act
        services.AddPdfGeneration(options =>
        {
            options.MaxConcurrentJobs = 3;
            options.BrowserPoolSize = 2;
            options.BrowserTimeout = TimeSpan.FromMinutes(1);
            options.Headless = true;
            options.RetryCount = 2;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var pdfService = serviceProvider.GetService<IPdfGenerationService>();
        Assert.NotNull(pdfService);
        Assert.IsType<PdfGenerationService>(pdfService);
    }

    [Fact]
    public void AddPdfGeneration_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // 添加日志服务
        services.AddLogging();
        
        services.AddPdfGeneration(options => { });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var service1 = serviceProvider.GetService<IPdfGenerationService>();
        var service2 = serviceProvider.GetService<IPdfGenerationService>();

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Same(service1, service2);
    }

    [Fact]
    public void AddPdfGeneration_WithDefaultOptions_HasCorrectDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // 添加日志服务
        services.AddLogging();
        
        services.AddPdfGeneration(options => { });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<PdfGenerationOptions>>()?.Value;

        // Assert
        Assert.NotNull(options);
        Assert.Equal(5, options.MaxConcurrentJobs);
        Assert.Equal(3, options.BrowserPoolSize);
        Assert.Equal(TimeSpan.FromMinutes(2), options.BrowserTimeout);
        Assert.True(options.Headless);
        Assert.Equal(3, options.RetryCount);
        Assert.Equal(512, options.BrowserMemoryLimit);
        Assert.NotNull(options.BrowserArguments);
        Assert.Contains("--no-sandbox", options.BrowserArguments);
    }
}