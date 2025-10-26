using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CodeSpirit.Audit.Tests.Infrastructure;

/// <summary>
/// 测试基础类，提供通用的测试基础设施
/// </summary>
public abstract class TestBase
{
    protected readonly ITestOutputHelper _output;
    protected readonly ServiceCollection _services;
    protected readonly ServiceProvider _serviceProvider;

    protected TestBase(ITestOutputHelper output)
    {
        _output = output;
        _services = new ServiceCollection();
        ConfigureServices(_services);
        _serviceProvider = _services.BuildServiceProvider();
    }

    /// <summary>
    /// 配置服务，添加测试所需的模拟服务
    /// </summary>
    /// <param name="services">服务集合</param>
    protected virtual void ConfigureServices(ServiceCollection services)
    {
        // 添加日志服务
        services.AddLogging(builder =>
        {
            builder.AddProvider(new XUnitLoggerProvider(_output));
        });
    }

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    protected T GetService<T>() where T : notnull
    {
        var service = _serviceProvider.GetRequiredService<T>();
        _output.WriteLine($"获取服务实例: {typeof(T).Name}");
        return service;
    }

    /// <summary>
    /// 获取日志记录器
    /// </summary>
    /// <typeparam name="T">日志类别类型</typeparam>
    /// <returns>日志记录器实例</returns>
    protected ILogger<T> GetLogger<T>()
    {
        return GetService<ILoggerFactory>().CreateLogger<T>();
    }

    /// <summary>
    /// 创建模拟日志记录器
    /// </summary>
    /// <typeparam name="T">日志类别类型</typeparam>
    /// <returns>模拟日志记录器</returns>
    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// 创建JSON序列化选项
    /// </summary>
    /// <returns>JSON序列化选项</returns>
    protected JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
} 