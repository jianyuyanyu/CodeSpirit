using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace CodeSpirit.Charts.Tests.Infrastructure
{
    /// <summary>
    /// 集成测试基类，提供更接近生产环境的依赖注入
    /// </summary>
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ServiceProvider Services;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected IntegrationTestBase()
        {
            var services = CreateServiceCollection();
            RegisterBaseServices(services);
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
            ServiceProvider = Services;
        }
        
        /// <summary>
        /// 创建服务集合
        /// </summary>
        protected virtual ServiceCollection CreateServiceCollection()
        {
            return new ServiceCollection();
        }
        
        /// <summary>
        /// 注册基础服务
        /// </summary>
        protected virtual void RegisterBaseServices(ServiceCollection services)
        {
            // 添加NullLogger（不输出日志）
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            
            // 添加基本依赖项
            services.AddHttpClient();
            
            // 添加图表相关服务
            services.AddSingleton<IDataAnalyzer, DataAnalyzer>();
            services.AddSingleton<IChartRecommender, ChartRecommender>();
            services.AddSingleton<IEChartConfigGenerator, EChartConfigGenerator>();
            services.AddScoped<IChartService, ChartService>();
        }
        
        /// <summary>
        /// 配置特定服务，子类可重写
        /// </summary>
        protected virtual void ConfigureServices(ServiceCollection services)
        {
            // 子类可以重写添加额外服务
        }
        
        /// <summary>
        /// 获取服务
        /// </summary>
        protected T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            Services?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
} 