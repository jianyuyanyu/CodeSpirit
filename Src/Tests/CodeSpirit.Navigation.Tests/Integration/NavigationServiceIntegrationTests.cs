using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests.Integration
{
    /// <summary>
    /// 导航服务集成测试
    /// 测试导航服务与缓存、配置的实际集成
    /// </summary>
    public class NavigationServiceIntegrationTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ITestOutputHelper _testOutputHelper;

        public NavigationServiceIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            // 构建测试服务集合
            var services = new ServiceCollection();
            
            // 添加内存缓存作为分布式缓存
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
            
            // 添加内存缓存选项
            services.AddOptions();
            services.AddMemoryCache();
            
            // 添加日志
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
            });
            
            // 添加测试配置
            var configValues = new Dictionary<string, string?>
            {
                {"Navigation:ModulePrefix", "TestPrefix"},
                {"Navigation:CacheExpiration", "3600"}
            };
            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();
            
            services.AddSingleton<IConfiguration>(configuration);
            
            // 添加IActionDescriptorCollectionProvider模拟
            var mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockActionProvider.Setup(p => p.ActionDescriptors).Returns(new ActionDescriptorCollection(new List<ActionDescriptor>(), 1));
            services.AddSingleton(mockActionProvider.Object);
            
            // 注册服务
            services.AddSingleton<INavigationService, NavigationService>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// 测试清除导航缓存并重新初始化 - 验证集成过程
        /// </summary>
        [Fact]
        public async Task ClearAndInitializeNavigation_IntegratedComponents_ShouldWorkTogether()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试清除导航缓存并重新初始化 - 集成测试");

            // 获取服务
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            var cache = _serviceProvider.GetRequiredService<IDistributedCache>();
            
            // 验证服务是否成功注入
            Assert.NotNull(navigationService);
            Assert.NotNull(cache);
            
            _testOutputHelper.WriteLine("测试清除导航缓存并重新初始化 - 服务注入成功");

            // 测试初始化过程
            await navigationService.InitializeNavigationTree();
            
            _testOutputHelper.WriteLine("测试清除导航缓存并重新初始化 - 初始化完成");
            
            // 清除一个模块的缓存
            const string testModule = "TestModule";
            await navigationService.ClearModuleNavigationCacheAsync(testModule);
            
            _testOutputHelper.WriteLine($"测试清除导航缓存并重新初始化 - 已清除模块 {testModule} 的缓存");
            
            // 重新获取导航树
            var navigationTree = await navigationService.GetNavigationTreeAsync();
            
            // 验证结果 - 本测试中导航树可能为空，因为我们没有实际添加导航项
            Assert.NotNull(navigationTree);
            
            _testOutputHelper.WriteLine($"测试清除导航缓存并重新初始化 - 成功获取导航树，节点数量: {navigationTree.Count}");
        }

        /// <summary>
        /// 测试导航服务在DI容器中的生命周期 - 验证是否为单例
        /// </summary>
        [Fact]
        public void NavigationService_LifetimeInDIContainer_ShouldBeSingleton()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试导航服务在DI容器中的生命周期");
            
            // 两次获取服务
            var service1 = _serviceProvider.GetRequiredService<INavigationService>();
            var service2 = _serviceProvider.GetRequiredService<INavigationService>();
            
            // 验证两次获取的是同一个实例（单例模式）
            Assert.Same(service1, service2);
            
            _testOutputHelper.WriteLine("测试导航服务在DI容器中的生命周期 - 验证为单例实例");
        }
    }
} 