using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeSpirit.ScheduledTasks.Tests;

/// <summary>
/// 服务集合扩展方法测试
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCodeSpiritScheduledTasks_WithServiceName_ShouldSetServiceNameInOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceName = "test-service";
        
        // 创建空配置
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScheduledTasks:Enabled", "true" },
                { "ScheduledTasks:MaxConcurrentTasks", "10" }
            })
            .Build();

        // Act
        services.AddCodeSpiritScheduledTasks(configuration, serviceName);
        
        // 构建服务提供者并获取选项
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ScheduledTasksOptions>>().Value;

        // Assert
        Assert.Equal(serviceName, options.ServiceName);
    }

    [Fact]
    public void AddCodeSpiritScheduledTasks_WithServiceName_ShouldOverrideConfigurationServiceName()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceName = "override-service";
        
        // 创建配置，其中已经包含 ServiceName
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScheduledTasks:Enabled", "true" },
                { "ScheduledTasks:ServiceName", "config-service" } // 配置文件中的值
            })
            .Build();

        // Act
        services.AddCodeSpiritScheduledTasks(configuration, serviceName);
        
        // 构建服务提供者并获取选项
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ScheduledTasksOptions>>().Value;

        // Assert - 传入的 serviceName 应该覆盖配置文件中的值
        Assert.Equal(serviceName, options.ServiceName);
    }

    [Fact]
    public void AddCodeSpiritScheduledTasks_WithEmptyServiceName_ShouldUseConfigurationServiceName()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceName = ""; // 空的服务名称
        
        // 创建配置，其中已经包含 ServiceName
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScheduledTasks:Enabled", "true" },
                { "ScheduledTasks:ServiceName", "config-service" }
            })
            .Build();

        // Act
        services.AddCodeSpiritScheduledTasks(configuration, serviceName);
        
        // 构建服务提供者并获取选项
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ScheduledTasksOptions>>().Value;

        // Assert - 当传入的 serviceName 为空时，应该使用配置文件中的值
        Assert.Equal("config-service", options.ServiceName);
    }

    [Fact]
    public void AddCodeSpiritScheduledTasks_WithConfigurationSection_ShouldSetServiceName()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceName = "section-service";
        
        // 创建配置
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScheduledTasks:Enabled", "true" },
                { "ScheduledTasks:MaxConcurrentTasks", "5" }
            })
            .Build();

        var configSection = configuration.GetSection(ScheduledTasksOptions.SectionName);

        // Act
        services.AddCodeSpiritScheduledTasks(configSection, serviceName);
        
        // 构建服务提供者并获取选项
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ScheduledTasksOptions>>().Value;

        // Assert
        Assert.Equal(serviceName, options.ServiceName);
        Assert.True(options.Enabled);
        Assert.Equal(5, options.MaxConcurrentTasks);
    }

    [Fact]
    public void AddCodeSpiritScheduledTasks_WithConfigureAction_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedServiceName = "action-service";

        // Act
        services.AddCodeSpiritScheduledTasks(options =>
        {
            options.ServiceName = expectedServiceName;
            options.Enabled = true;
            options.MaxConcurrentTasks = 20;
        });
        
        // 构建服务提供者并获取选项
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ScheduledTasksOptions>>().Value;

        // Assert
        Assert.Equal(expectedServiceName, options.ServiceName);
        Assert.True(options.Enabled);
        Assert.Equal(20, options.MaxConcurrentTasks);
    }

    [Fact]
    public void AddCodeSpiritScheduledTasks_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceName = "test-service";
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ScheduledTasks:Enabled", "true" }
            })
            .Build();

        // Act
        services.AddCodeSpiritScheduledTasks(configuration, serviceName);

        // Assert - 验证必要的服务已注册
        Assert.Contains(services, s => s.ServiceType == typeof(IOptions<ScheduledTasksOptions>).Assembly.GetType("Microsoft.Extensions.Options.IConfigureOptions`1")?.MakeGenericType(typeof(ScheduledTasksOptions)));
    }
}
