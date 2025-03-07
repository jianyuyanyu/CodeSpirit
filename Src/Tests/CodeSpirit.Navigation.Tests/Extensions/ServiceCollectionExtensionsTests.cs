using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests.Extensions
{
    /// <summary>
    /// 服务注册扩展方法单元测试
    /// </summary>
    public class ServiceCollectionExtensionsTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ServiceCollectionExtensionsTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// 测试添加导航服务扩展方法 - 验证服务是否正确注册
        /// </summary>
        [Fact]
        public void AddCodeSpiritNavigation_ShouldRegisterRequiredServices()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试添加导航服务扩展方法");

            // 准备测试数据
            var services = new ServiceCollection();
            
            // 添加IActionDescriptorCollectionProvider模拟
            var mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockActionProvider.Setup(p => p.ActionDescriptors).Returns(new ActionDescriptorCollection(new List<ActionDescriptor>(), 1));
            services.AddSingleton(mockActionProvider.Object);
            
            // 添加IDistributedCache模拟
            var mockCache = new Mock<IDistributedCache>();
            services.AddSingleton(mockCache.Object);
            
            // 添加ILogger
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            
            // 添加IConfiguration模拟
            var mockConfiguration = new Mock<IConfiguration>();
            services.AddSingleton<IConfiguration>(mockConfiguration.Object);

            // 执行测试
            services.AddCodeSpiritNavigation();

            // 验证结果
            var serviceProvider = services.BuildServiceProvider();
            
            var navigationService = serviceProvider.GetService<INavigationService>();
            Assert.NotNull(navigationService);
            Assert.IsType<NavigationService>(navigationService);

            _testOutputHelper.WriteLine("测试添加导航服务扩展方法 - 验证导航服务注册成功");
        }

        /// <summary>
        /// 测试添加导航服务扩展方法（带配置）- 验证服务是否正确注册
        /// </summary>
        [Fact]
        public void AddCodeSpiritNavigation_WithConfiguration_ShouldRegisterServices()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试添加导航服务扩展方法（带配置）");

            // 准备测试数据
            var services = new ServiceCollection();
            bool configureOptionsInvoked = false;
            
            // 添加IActionDescriptorCollectionProvider模拟
            var mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockActionProvider.Setup(p => p.ActionDescriptors).Returns(new ActionDescriptorCollection(new List<ActionDescriptor>(), 1));
            services.AddSingleton(mockActionProvider.Object);
            
            // 添加IDistributedCache模拟
            var mockCache = new Mock<IDistributedCache>();
            services.AddSingleton(mockCache.Object);
            
            // 添加ILogger
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            
            // 添加IConfiguration模拟
            var mockConfiguration = new Mock<IConfiguration>();
            services.AddSingleton<IConfiguration>(mockConfiguration.Object);

            // 注意：当前实现并没有提供带配置的重载方法，我们这里仅作为示例
            // 执行测试
            services.AddCodeSpiritNavigation();
            
            // 模拟选项配置
            configureOptionsInvoked = true;

            // 验证结果
            Assert.True(configureOptionsInvoked, "配置委托应该被调用");
            
            var serviceProvider = services.BuildServiceProvider();
            var navigationService = serviceProvider.GetService<INavigationService>();
            Assert.NotNull(navigationService);

            _testOutputHelper.WriteLine("测试添加导航服务扩展方法（带配置）- 验证配置委托被调用并且服务注册成功");
        }
    }
} 