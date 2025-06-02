using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Tests.TestBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests
{
    /// <summary>
    /// 导航问题诊断测试
    /// </summary>
    public class SystemPlatformNavigationDiagnosticTests : NavigationTestBase
    {
        private readonly ITestOutputHelper _output;

        public SystemPlatformNavigationDiagnosticTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 诊断导航构建的完整流程
        /// </summary>
        [Fact]
        public async Task DiagnoseNavigationBuildingProcess()
        {
            _output.WriteLine("=== 导航构建流程诊断 ===");

            // Step 1: 检查ActionProvider设置
            _output.WriteLine("步骤1：检查ActionProvider设置");
            var systemUsersDescriptor = CreateSystemUsersControllerDescriptor();
            var descriptors = new List<ControllerActionDescriptor> { systemUsersDescriptor };

            MockActionProvider.Setup(x => x.ActionDescriptors)
                .Returns(CreateMockActionDescriptorCollection(descriptors));

            var actionDescriptors = MockActionProvider.Object.ActionDescriptors;
            _output.WriteLine($"ActionDescriptors count: {actionDescriptors.Items.Count}");

            foreach (var descriptor in actionDescriptors.Items.OfType<ControllerActionDescriptor>())
            {
                _output.WriteLine($"  Controller: {descriptor.ControllerName}, Action: {descriptor.ActionName}");
                _output.WriteLine($"  Type: {descriptor.ControllerTypeInfo?.Name}");
            }

            // Step 2: 验证模块识别
            _output.WriteLine("\n步骤2：验证模块识别");
            var controllerTypes = actionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(x => x.ControllerTypeInfo)
                .Distinct();

            foreach (var controllerType in controllerTypes)
            {
                var moduleAttr = controllerType.GetCustomAttribute<ModuleAttribute>();
                _output.WriteLine($"控制器 {controllerType.Name}:");
                _output.WriteLine($"  模块特性: {moduleAttr?.Name} - {moduleAttr?.DisplayName}");
            }

            // Step 3: 调用BuildCodeBasedNavigation
            _output.WriteLine("\n步骤3：调用BuildCodeBasedNavigation");
            var buildMethod = typeof(NavigationService).GetMethod("BuildCodeBasedNavigation", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (buildMethod != null)
            {
                var result = (List<NavigationNode>)buildMethod.Invoke(NavigationService, new object[] { "identity" });
                _output.WriteLine($"BuildCodeBasedNavigation结果: {result?.Count ?? 0} 个节点");

                if (result?.Any() == true)
                {
                    foreach (var node in result)
                    {
                        PrintNavigationNode(node, 0);
                    }
                }
            }

            // Step 4: 测试GetNavigationTreeAsync
            _output.WriteLine("\n步骤4：测试GetNavigationTreeAsync");
            var allNodesResult = await NavigationService.GetNavigationTreeAsync(PlatformType.Both);
            _output.WriteLine($"GetNavigationTreeAsync(Both) 结果: {allNodesResult?.Count ?? 0} 个节点");

            var systemNodesResult = await NavigationService.GetNavigationTreeAsync(PlatformType.System);
            _output.WriteLine($"GetNavigationTreeAsync(System) 结果: {systemNodesResult?.Count ?? 0} 个节点");

            if (systemNodesResult?.Any() == true)
            {
                foreach (var node in systemNodesResult)
                {
                    PrintNavigationNode(node, 0);
                }
            }

            // Step 5: 测试平台过滤逻辑
            _output.WriteLine("\n步骤5：测试平台过滤逻辑");
            if (allNodesResult?.Any() == true)
            {
                var filteredNodes = NavigationService.FilterNodesByPlatform(allNodesResult, PlatformType.System);
                _output.WriteLine($"FilterNodesByPlatform(System) 结果: {filteredNodes?.Count ?? 0} 个节点");

                if (filteredNodes?.Any() == true)
                {
                    foreach (var node in filteredNodes)
                    {
                        PrintNavigationNode(node, 0);
                    }
                }
            }
        }

        /// <summary>
        /// 创建SystemUsersController描述符
        /// </summary>
        private ControllerActionDescriptor CreateSystemUsersControllerDescriptor()
        {
            var systemUsersControllerType = typeof(TestSystemUsersController);
            
            return new ControllerActionDescriptor
            {
                ControllerTypeInfo = systemUsersControllerType.GetTypeInfo(),
                ControllerName = "SystemUsers",
                ActionName = "Index",
                MethodInfo = systemUsersControllerType.GetMethod("Index")
            };
        }

        /// <summary>
        /// 创建ActionDescriptorCollection
        /// </summary>
        private ActionDescriptorCollection CreateMockActionDescriptorCollection(List<ControllerActionDescriptor> descriptors)
        {
            return new ActionDescriptorCollection(descriptors.Cast<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>().ToList(), 1);
        }

        /// <summary>
        /// 打印导航节点结构
        /// </summary>
        private void PrintNavigationNode(NavigationNode node, int indent)
        {
            var prefix = new string(' ', indent * 2);
            _output.WriteLine($"{prefix}- {node.Title} (Name: {node.Name}, PlatformType: {node.PlatformType}, OriginalPlatformType: {node.OriginalPlatformType})");
            
            foreach (var child in node.Children)
            {
                PrintNavigationNode(child, indent + 1);
            }
        }

        /// <summary>
        /// 测试位运算逻辑的详细分析
        /// </summary>
        [Fact]
        public void AnalyzeBitwiseFilteringLogic()
        {
            _output.WriteLine("=== 位运算过滤逻辑分析 ===");

            var testCases = new[]
            {
                new { NodeType = PlatformType.System, QueryType = PlatformType.System },
                new { NodeType = PlatformType.System, QueryType = PlatformType.Tenant },
                new { NodeType = PlatformType.System, QueryType = PlatformType.Both },
                new { NodeType = PlatformType.Tenant, QueryType = PlatformType.System },
                new { NodeType = PlatformType.Tenant, QueryType = PlatformType.Tenant },
                new { NodeType = PlatformType.Tenant, QueryType = PlatformType.Both },
                new { NodeType = PlatformType.Both, QueryType = PlatformType.System },
                new { NodeType = PlatformType.Both, QueryType = PlatformType.Tenant },
                new { NodeType = PlatformType.Both, QueryType = PlatformType.Both },
                new { NodeType = PlatformType.Inherit, QueryType = PlatformType.System },
                new { NodeType = PlatformType.Inherit, QueryType = PlatformType.Tenant },
                new { NodeType = PlatformType.None, QueryType = PlatformType.System }
            };

            foreach (var testCase in testCases)
            {
                var result = (testCase.NodeType & testCase.QueryType) != 0;
                var nodeBinary = Convert.ToString((int)testCase.NodeType, 2).PadLeft(3, '0');
                var queryBinary = Convert.ToString((int)testCase.QueryType, 2).PadLeft(3, '0');
                var andResult = (int)(testCase.NodeType & testCase.QueryType);
                var andBinary = Convert.ToString(andResult, 2).PadLeft(3, '0');

                _output.WriteLine($"{testCase.NodeType}({nodeBinary}) & {testCase.QueryType}({queryBinary}) = {andResult}({andBinary}) -> {result}");
            }
        }

        /// <summary>
        /// 测试实际真实控制器的导航识别
        /// </summary>
        [Fact]
        public void TestRealSystemUsersControllerNavigation()
        {
            _output.WriteLine("=== 真实SystemUsersController导航识别测试 ===");

            // 使用反射获取真实的SystemUsersController类型
            var identityApiAssembly = System.IO.File.Exists("Src/CodeSpirit.IdentityApi/bin/Debug/net9.0/CodeSpirit.IdentityApi.dll") ?
                Assembly.LoadFrom("Src/CodeSpirit.IdentityApi/bin/Debug/net9.0/CodeSpirit.IdentityApi.dll") : null;

            if (identityApiAssembly != null)
            {
                var realSystemUsersController = identityApiAssembly.GetTypes()
                    .FirstOrDefault(t => t.Name == "SystemUsersController");

                if (realSystemUsersController != null)
                {
                    _output.WriteLine($"找到真实控制器: {realSystemUsersController.FullName}");

                    var moduleAttr = realSystemUsersController.GetCustomAttribute<ModuleAttribute>();
                    var navigationAttr = realSystemUsersController.GetCustomAttribute<NavigationAttribute>();

                    _output.WriteLine($"模块特性: {moduleAttr?.Name} - {moduleAttr?.DisplayName}");
                    _output.WriteLine($"导航特性: Icon={navigationAttr?.Icon}, PlatformType={navigationAttr?.PlatformType}");
                }
                else
                {
                    _output.WriteLine("未找到SystemUsersController类型");
                }
            }
            else
            {
                _output.WriteLine("未找到IdentityApi程序集");
            }
        }
    }
} 