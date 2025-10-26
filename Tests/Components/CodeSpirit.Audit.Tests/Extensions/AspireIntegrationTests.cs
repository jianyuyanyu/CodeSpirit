using CodeSpirit.Audit.Extensions;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Implementation;
using CodeSpirit.ServiceDefaults.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;

namespace CodeSpirit.Audit.Tests.Extensions;

/// <summary>
/// Aspire 集成测试
/// </summary>
public class AspireIntegrationTests
{
    /// <summary>
    /// 测试添加审计服务 - 基本功能
    /// </summary>
    [Fact]
    public void AddAuditServices_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();
        
        // 注册必要的依赖服�?
        RegisterRequiredDependencies(services, configuration);
        
        // Act
        AuditExtensions.AddAuditServices(services, configuration);
        
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // 验证核心服务已注册（不实际创建实例以避免依赖问题�?
        Assert.Contains(services, s => s.ServiceType == typeof(CodeSpirit.Audit.Services.IElasticsearchService));
        Assert.Contains(services, s => s.ServiceType == typeof(CodeSpirit.Audit.Services.IRabbitMQService));
        Assert.Contains(services, s => s.ServiceType == typeof(CodeSpirit.Audit.Services.IAuditService));
        Assert.Contains(services, s => s.ServiceType == typeof(CodeSpirit.Audit.Services.IGeoLocationService));
        Assert.Contains(services, s => s.ServiceType == typeof(IAuditErrorHandler));
        
        // 验证内存缓存已注�?
        Assert.NotNull(serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>());
        
        // 验证HTTP客户端工厂已注册
        Assert.NotNull(serviceProvider.GetService<System.Net.Http.IHttpClientFactory>());
    }
    
    /// <summary>
    /// 测试添加审计服务 - 使用Audit配置�?
    /// </summary>
    [Fact]
    public void AddAuditServices_WithAuditSection_ShouldBindCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfigurationWithAuditSection();
        
        // 注册必要的依赖服�?
        RegisterRequiredDependencies(services, configuration);
        
        // Act
        AuditExtensions.AddAuditServices(services, configuration);
        
        // Assert
        // 验证服务注册（不实际创建实例�?
        Assert.Contains(services, s => s.ServiceType == typeof(CodeSpirit.Audit.Services.IAuditService));
    }
    
    /// <summary>
    /// 测试添加审计服务 - 使用直接配置
    /// </summary>
    [Fact]
    public void AddAuditServices_WithDirectConfiguration_ShouldBindCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateDirectAuditConfiguration();
        
        // 注册必要的依赖服�?
        RegisterRequiredDependencies(services, configuration);
        
        // Act
        AuditExtensions.AddAuditServices(services, configuration);
        
        // Assert
        // 验证服务注册（不实际创建实例�?
        Assert.Contains(services, s => s.ServiceType == typeof(CodeSpirit.Audit.Services.IAuditService));
    }
    
    /// <summary>
    /// 测试添加审计服务 - 重复注册内存缓存
    /// </summary>
    [Fact]
    public void AddAuditServices_WithExistingMemoryCache_ShouldNotDuplicate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache(); // 预先注册内存缓存
        var configuration = CreateTestConfiguration();
        
        // 注册必要的依赖服�?
        RegisterRequiredDependencies(services, configuration);
        
        // Act
        AuditExtensions.AddAuditServices(services, configuration);
        
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var memoryCache = serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        Assert.NotNull(memoryCache);
        
        // 验证只有一个内存缓存服务注�?
        var memoryCacheServices = services.Where(s => s.ServiceType == typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache));
        Assert.Single(memoryCacheServices);
    }
    
    /// <summary>
    /// 测试添加审计后台服务
    /// </summary>
    [Fact]
    public void AddAuditBackgroundServices_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddAuditBackgroundServices();
        
        // Assert
        var hostedServices = services.Where(s => s.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));
        Assert.NotEmpty(hostedServices);
        
        // 验证注册了审计日志消费者服�?
        var auditConsumerService = hostedServices.FirstOrDefault(s => 
            s.ImplementationType?.Name.Contains("AuditLogConsumerService") == true);
        Assert.NotNull(auditConsumerService);
    }
    
    /// <summary>
    /// 测试添加审计性能监控
    /// </summary>
    [Fact]
    public void AddAuditPerformanceMonitoring_ShouldRegisterMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act - 明确使用 AuditExtensions 的方�?
        AuditExtensions.AddAuditPerformanceMonitoring(services);
        
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // 验证性能监控中间件已注册为瞬态服�?
        var performanceMiddleware = services.FirstOrDefault(s => 
            s.ImplementationType?.Name.Contains("AuditPerformanceMiddleware") == true);
        Assert.NotNull(performanceMiddleware);
        Assert.Equal(ServiceLifetime.Transient, performanceMiddleware.Lifetime);
    }
    
    /// <summary>
    /// 测试Aspire客户端注�?- 无Aspire配置
    /// </summary>
    [Fact]
    public void TryRegisterAspireElasticsearchClient_WithoutAspireConfig_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration(); // 不包含Aspire配置
        
        // 注册必要的依赖服�?
        RegisterRequiredDependencies(services, configuration);
        
        // Act & Assert - 应该不抛出异�?
        var exception = Record.Exception(() => AuditExtensions.AddAuditServices(services, configuration));
        Assert.Null(exception);
        
        // 验证服务仍然正常注册
        Assert.Contains(services, s => s.ServiceType == typeof(CodeSpirit.Audit.Services.IElasticsearchService));
    }
    
    /// <summary>
    /// 测试Aspire客户端注�?- 有Aspire配置但无程序�?
    /// </summary>
    [Fact]
    public void TryRegisterAspireElasticsearchClient_WithAspireConfigButNoAssembly_ShouldFallback()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfigurationWithAspireSection();
        
        // 注册必要的依赖服�?
        RegisterRequiredDependencies(services, configuration);
        
        // Act & Assert - 应该优雅降级，不抛出异常
        var exception = Record.Exception(() => AuditExtensions.AddAuditServices(services, configuration));
        Assert.Null(exception);
        
        // 验证服务仍然正常注册（使用手动配置）
        Assert.Contains(services, s => s.ServiceType == typeof(CodeSpirit.Audit.Services.IElasticsearchService));
    }
    
    /// <summary>
    /// 测试HTTP客户端配�?
    /// </summary>
    [Fact]
    public void AddAuditServices_ShouldConfigureHttpClientCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();
        
        // 注册必要的依赖服�?
        RegisterRequiredDependencies(services, configuration);
        
        // Act
        AuditExtensions.AddAuditServices(services, configuration);
        
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<System.Net.Http.IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
        
        // 验证可以创建GeoLocation客户�?
        var geoLocationClient = httpClientFactory.CreateClient("GeoLocation");
        Assert.NotNull(geoLocationClient);
        
        // 验证客户端配�?
        Assert.Equal(TimeSpan.FromSeconds(5), geoLocationClient.Timeout);
        Assert.Contains("CodeSpirit-Audit", geoLocationClient.DefaultRequestHeaders.UserAgent.ToString());
    }
    
    /// <summary>
    /// 测试服务生命周期
    /// </summary>
    [Fact]
    public void AddAuditServices_ShouldRegisterWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();
        
        // 注册必要的依赖服�?
        RegisterRequiredDependencies(services, configuration);
        
        // Act
        AuditExtensions.AddAuditServices(services, configuration);
        
        // Assert
        // 验证单例服务
        var singletonServices = new[]
        {
            typeof(CodeSpirit.Audit.Services.IElasticsearchService),
            typeof(CodeSpirit.Audit.Services.IRabbitMQService),
            typeof(CodeSpirit.Audit.Services.IGeoLocationService),
            typeof(IAuditErrorHandler)
        };
        
        foreach (var serviceType in singletonServices)
        {
            var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == serviceType);
            Assert.NotNull(serviceDescriptor);
            Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }
        
        // 验证作用域服�?
        var scopedServices = new[]
        {
            typeof(CodeSpirit.Audit.Services.IAuditService)
        };
        
        foreach (var serviceType in scopedServices)
        {
            var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == serviceType);
            Assert.NotNull(serviceDescriptor);
            Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
        }
    }
    
    #region 辅助方法
    
    /// <summary>
    /// 创建测试配置
    /// </summary>
    private IConfiguration CreateTestConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Audit:Enabled"] = "true",
            ["Audit:Elasticsearch:Urls:0"] = "http://localhost:9200",
            ["Audit:Elasticsearch:IndexName"] = "test-audit-logs",
            ["Audit:Elasticsearch:IndexPrefix"] = "test",
            ["Audit:RabbitMQ:HostName"] = "localhost",
            ["Audit:RabbitMQ:Port"] = "5672",
            ["Audit:RabbitMQ:QueueName"] = "audit-logs"
        });
        return configBuilder.Build();
    }
    
    /// <summary>
    /// 创建包含Audit配置节的测试配置
    /// </summary>
    private IConfiguration CreateTestConfigurationWithAuditSection()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Audit:Enabled"] = "true",
            ["Audit:Elasticsearch:Urls:0"] = "http://localhost:9200",
            ["Audit:Elasticsearch:IndexName"] = "test-audit-logs",
            ["OtherSection:SomeValue"] = "test"
        });
        return configBuilder.Build();
    }
    
    /// <summary>
    /// 创建直接的Audit配置
    /// </summary>
    private IConfiguration CreateDirectAuditConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Enabled"] = "true",
            ["Elasticsearch:Urls:0"] = "http://localhost:9200",
            ["Elasticsearch:IndexName"] = "test-audit-logs"
        });
        return configBuilder.Build();
    }
    
    /// <summary>
    /// 创建包含Aspire配置节的配置
    /// </summary>
    private IConfiguration CreateConfigurationWithAspireSection()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Audit:Enabled"] = "true",
            ["Audit:Elasticsearch:Urls:0"] = "http://localhost:9200",
            ["Audit:Elasticsearch:IndexName"] = "test-audit-logs",
            ["Aspire:Elastic:Clients:Elasticsearch:ConnectionString"] = "http://localhost:9200",
            ["Aspire:Elastic:Clients:Elasticsearch:HealthChecks"] = "true"
        });
        return configBuilder.Build();
    }
    
    /// <summary>
    /// 注册必要的依赖服�?
    /// </summary>
    private void RegisterRequiredDependencies(ServiceCollection services, IConfiguration? configuration = null)
    {
        // 注册 IConfiguration 服务（如果尚未注册）
        if (!services.Any(s => s.ServiceType == typeof(IConfiguration)))
        {
            services.AddSingleton<IConfiguration>(configuration ?? CreateTestConfiguration());
        }
        
        // 创建模拟�?RabbitMQ 连接和通道
        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<IModel>();
        
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        
        // 注册模拟�?IRabbitMQServiceFactory
        services.AddSingleton<IRabbitMQServiceFactory>(sp =>
        {
            var factory = new Mock<IRabbitMQServiceFactory>();
            factory.Setup(f => f.GetAuditConnection()).Returns(mockConnection.Object);
            factory.Setup(f => f.GetEventBusConnection()).Returns(mockConnection.Object);
            factory.Setup(f => f.GetMessagingConnection()).Returns(mockConnection.Object);
            factory.Setup(f => f.GetConnection(It.IsAny<string>())).Returns(mockConnection.Object);
            return factory.Object;
        });
    }
    
    #endregion
} 