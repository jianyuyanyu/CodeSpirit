using CodeSpirit.Amis.Helpers;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Resources;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace CodeSpirit.Navigation.Tests.Services
{
    /// <summary>
    /// 导航本地化集成测试
    /// 测试从构建导航树到本地化的完整流程
    /// </summary>
    public class NavigationLocalizationIntegrationTests
    {
        /// <summary>
        /// 测试：完整的本地化流程 - 中文环境
        /// </summary>
        [Fact]
        public void FullLocalizationFlow_ChineseCulture_ShouldReturnLocalizedText()
        {
            // 安排：创建包含资源键信息的导航节点
            var nodes = CreateTestNavigationNodes();

            // 创建本地化服务
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new Mock<HttpContext>();
            var requestCultureFeature = new Mock<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var requestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(new CultureInfo("zh-CN"));
            
            requestCultureFeature.Setup(x => x.RequestCulture).Returns(requestCulture);
            httpContext.Setup(x => x.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>())
                .Returns(requestCultureFeature.Object);
            httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);

            var cultureLoggerMock = new Mock<ILogger<CultureResolver>>();
            var cultureResolver = new CultureResolver(httpContextAccessor.Object, cultureLoggerMock.Object);
            var logger = new Mock<ILogger<NavigationLocalizationService>>();
            var localizationService = new NavigationLocalizationService(cultureResolver, logger.Object);

            // 执行：本地化导航树
            var result = localizationService.LocalizeNavigationTree(nodes);

            // 断言：验证中文环境下的本地化结果
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Single(result);
            var moduleNode = result[0];
            Assert.NotNull(moduleNode);
            // 验证标题已被本地化（从资源文件获取）
            Assert.Equal("用户中心", moduleNode.Title); // 中文：用户中心
            
            Assert.NotNull(moduleNode.Children);
            Assert.NotEmpty(moduleNode.Children);
            Assert.Single(moduleNode.Children);
            var controllerNode = moduleNode.Children[0];
            Assert.Equal("用户管理", controllerNode.Title); // 中文：用户管理
        }

        /// <summary>
        /// 测试：完整的本地化流程 - 英文环境
        /// </summary>
        [Fact]
        public void FullLocalizationFlow_EnglishCulture_ShouldReturnLocalizedText()
        {
            // 安排：创建包含资源键信息的导航节点
            var nodes = CreateTestNavigationNodes();

            // 创建本地化服务
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new Mock<HttpContext>();
            var requestCultureFeature = new Mock<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var requestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(new CultureInfo("en"));
            
            requestCultureFeature.Setup(x => x.RequestCulture).Returns(requestCulture);
            httpContext.Setup(x => x.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>())
                .Returns(requestCultureFeature.Object);
            httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);

            var cultureLoggerMock = new Mock<ILogger<CultureResolver>>();
            var cultureResolver = new CultureResolver(httpContextAccessor.Object, cultureLoggerMock.Object);
            var logger = new Mock<ILogger<NavigationLocalizationService>>();
            var localizationService = new NavigationLocalizationService(cultureResolver, logger.Object);

            // 执行：本地化导航树
            var result = localizationService.LocalizeNavigationTree(nodes);

            // 断言：验证英文环境下的本地化结果
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Single(result);
            var moduleNode = result[0];
            Assert.NotNull(moduleNode);
            // 验证标题已被本地化（从资源文件获取）
            Assert.Equal("User Center", moduleNode.Title); // 英文：User Center
            
            Assert.NotNull(moduleNode.Children);
            Assert.NotEmpty(moduleNode.Children);
            Assert.Single(moduleNode.Children);
            var controllerNode = moduleNode.Children[0];
            Assert.Equal("User Management", controllerNode.Title); // 英文：User Management
        }

        /// <summary>
        /// 测试：混合场景 - 部分节点有资源键，部分没有
        /// </summary>
        [Fact]
        public void MixedScenario_SomeNodesHaveResourceKeys_ShouldLocalizeOnlyThoseWithKeys()
        {
            // 安排：创建混合场景的导航节点
            var nodes = new List<NavigationNode>
            {
                new NavigationNode("module1", "模块1", "/module1")
                {
                    TitleResourceKey = "Module.Identity",
                    TitleResourceType = typeof(NavigationResources).FullName,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("controller1", "控制器1", "/module1/controller1")
                        {
                            // 没有资源键，应保持原始文本
                            TitleResourceKey = null,
                            TitleResourceType = null
                        },
                        new NavigationNode("controller2", "控制器2", "/module1/controller2")
                        {
                            TitleResourceKey = "Controller.Users",
                            TitleResourceType = typeof(NavigationResources).FullName
                        }
                    }
                }
            };

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new Mock<HttpContext>();
            var requestCultureFeature = new Mock<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var requestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(new CultureInfo("zh-CN"));
            
            requestCultureFeature.Setup(x => x.RequestCulture).Returns(requestCulture);
            httpContext.Setup(x => x.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>())
                .Returns(requestCultureFeature.Object);
            httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);

            var cultureLoggerMock = new Mock<ILogger<CultureResolver>>();
            var cultureResolver = new CultureResolver(httpContextAccessor.Object, cultureLoggerMock.Object);
            var logger = new Mock<ILogger<NavigationLocalizationService>>();
            var localizationService = new NavigationLocalizationService(cultureResolver, logger.Object);

            // 执行
            var result = localizationService.LocalizeNavigationTree(nodes);

            // 断言
            Assert.Single(result);
            var moduleNode = result[0];
            Assert.Equal("用户中心", moduleNode.Title); // 有资源键，已本地化
            
            Assert.Equal(2, moduleNode.Children.Count);
            Assert.Equal("控制器1", moduleNode.Children[0].Title); // 没有资源键，保持原样
            Assert.Equal("用户管理", moduleNode.Children[1].Title); // 有资源键，已本地化
        }

        /// <summary>
        /// 测试：深拷贝验证 - 确保原始节点不被修改
        /// </summary>
        [Fact]
        public void DeepCopyVerification_OriginalNodesShouldNotBeModified()
        {
            // 安排
            var originalNodes = CreateTestNavigationNodes();
            var originalModuleTitle = originalNodes[0].Title;
            var originalControllerTitle = originalNodes[0].Children[0].Title;

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new Mock<HttpContext>();
            var requestCultureFeature = new Mock<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var requestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(new CultureInfo("en"));
            
            requestCultureFeature.Setup(x => x.RequestCulture).Returns(requestCulture);
            httpContext.Setup(x => x.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>())
                .Returns(requestCultureFeature.Object);
            httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);

            var cultureLoggerMock = new Mock<ILogger<CultureResolver>>();
            var cultureResolver = new CultureResolver(httpContextAccessor.Object, cultureLoggerMock.Object);
            var logger = new Mock<ILogger<NavigationLocalizationService>>();
            var localizationService = new NavigationLocalizationService(cultureResolver, logger.Object);

            // 执行
            var localizedNodes = localizationService.LocalizeNavigationTree(originalNodes);

            // 断言：验证本地化结果不为空
            Assert.NotNull(localizedNodes);
            Assert.NotEmpty(localizedNodes);
            Assert.NotNull(localizedNodes[0].Children);
            Assert.NotEmpty(localizedNodes[0].Children);
            
            // 原始节点应该保持不变
            Assert.Equal(originalModuleTitle, originalNodes[0].Title);
            Assert.Equal(originalControllerTitle, originalNodes[0].Children[0].Title);
            
            // 本地化后的节点应该不同
            Assert.NotEqual(originalNodes[0].Title, localizedNodes[0].Title);
            Assert.NotEqual(originalNodes[0].Children[0].Title, localizedNodes[0].Children[0].Title);
            
            // 验证是不同的对象引用
            Assert.NotSame(originalNodes[0], localizedNodes[0]);
            Assert.NotSame(originalNodes[0].Children[0], localizedNodes[0].Children[0]);
        }

        /// <summary>
        /// 创建测试用的导航节点
        /// </summary>
        private List<NavigationNode> CreateTestNavigationNodes()
        {
            return new List<NavigationNode>
            {
                new NavigationNode("identity", "用户中心", "/identity")
                {
                    ModuleName = "identity",
                    TitleResourceKey = "Module.Identity",
                    TitleResourceType = typeof(NavigationResources).FullName,
                    PlatformType = PlatformType.Both,
                    Children = new List<NavigationNode>
                    {
                        new NavigationNode("users", "用户管理", "/identity/users")
                        {
                            TitleResourceKey = "Controller.Users",
                            TitleResourceType = typeof(NavigationResources).FullName,
                            ModuleName = "identity",
                            PlatformType = PlatformType.Tenant
                        }
                    }
                }
            };
        }
    }
}

