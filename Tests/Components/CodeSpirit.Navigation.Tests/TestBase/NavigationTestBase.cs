using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Tests.TestControllers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Reflection;
using System.Linq;
using System;
using CodeSpirit.Core.Authorization;

namespace CodeSpirit.Navigation.Tests.TestBase
{
    /// <summary>
    /// 导航测试基类
    /// </summary>
    public abstract class NavigationTestBase
    {
        /// <summary>
        /// 模拟分布式缓存
        /// </summary>
        protected Mock<IDistributedCache> MockCache { get; private set; }

        /// <summary>
        /// 模拟配置
        /// </summary>
        protected Mock<IConfiguration> MockConfiguration { get; private set; }

        /// <summary>
        /// 模拟日志记录器
        /// </summary>
        protected Mock<ILogger<NavigationService>> MockLogger { get; private set; }

        /// <summary>
        /// 模拟权限服务
        /// </summary>
        protected Mock<IHasPermissionService> MockPermissionService { get; private set; }

        /// <summary>
        /// 模拟动作描述符提供者
        /// </summary>
        protected Mock<IActionDescriptorCollectionProvider> MockActionProvider { get; private set; }

        /// <summary>
        /// 导航服务实例
        /// </summary>
        protected NavigationService NavigationService { get; private set; }

        /// <summary>
        /// 初始化测试基类
        /// </summary>
        protected NavigationTestBase()
        {
            SetupMocks();
            NavigationService = new NavigationService(
                MockActionProvider.Object,
                MockCache.Object,
                MockLogger.Object,
                MockConfiguration.Object);
        }

        /// <summary>
        /// 设置模拟对象
        /// </summary>
        private void SetupMocks()
        {
            MockCache = new Mock<IDistributedCache>();
            MockConfiguration = new Mock<IConfiguration>();
            MockLogger = new Mock<ILogger<NavigationService>>();
            MockPermissionService = new Mock<IHasPermissionService>();
            MockActionProvider = new Mock<IActionDescriptorCollectionProvider>();

            // 设置测试控制器的动作描述符
            var actionDescriptors = CreateTestActionDescriptors();
            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actionDescriptors, 1));

            // 设置默认的权限服务行为
            MockPermissionService.Setup(x => x.HasNavigationPermission(It.IsAny<string>()))
                .Returns(true);
        }

        /// <summary>
        /// 创建测试动作描述符
        /// </summary>
        /// <returns></returns>
        private List<ActionDescriptor> CreateTestActionDescriptors()
        {
            var descriptors = new List<ActionDescriptor>();

            // 添加 TestModuleController 的动作描述符
            var testModuleType = typeof(TestModuleController);
            var methods = testModuleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == testModuleType);

            foreach (var method in methods)
            {
                descriptors.Add(new ControllerActionDescriptor
                {
                    ControllerTypeInfo = testModuleType.GetTypeInfo(),
                    MethodInfo = method,
                    ActionName = method.Name,
                    ControllerName = "TestModule"
                });
            }

            // 添加 AnotherModuleController 的动作描述符
            var anotherModuleType = typeof(AnotherModuleController);
            var anotherMethods = anotherModuleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == anotherModuleType);

            foreach (var method in anotherMethods)
            {
                descriptors.Add(new ControllerActionDescriptor
                {
                    ControllerTypeInfo = anotherModuleType.GetTypeInfo(),
                    MethodInfo = method,
                    ActionName = method.Name,
                    ControllerName = "AnotherModule"
                });
            }

            return descriptors;
        }

        /// <summary>
        /// 设置配置节
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        protected void SetupConfiguration(string key, object value)
        {
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns(value?.ToString());
            configSection.Setup(x => x.Key).Returns(key.Split(':').Last());
            
            MockConfiguration.Setup(x => x.GetSection(key)).Returns(configSection.Object);

            // 如果是获取对象类型，返回 null 以避免绑定错误
            configSection.Setup(x => x.Get<object>()).Returns((object)null);
        }

        /// <summary>
        /// 设置权限检查结果
        /// </summary>
        /// <param name="permission">权限</param>
        /// <param name="hasPermission">是否有权限</param>
        protected void SetupPermission(string permission, bool hasPermission)
        {
            MockPermissionService.Setup(x => x.HasNavigationPermission(permission))
                .Returns(hasPermission);
        }

        /// <summary>
        /// 设置权限检查行为
        /// </summary>
        /// <param name="setupAction">设置动作</param>
        protected void SetupPermissions(Action<Mock<IHasPermissionService>> setupAction)
        {
            setupAction(MockPermissionService);
        }
    }
} 