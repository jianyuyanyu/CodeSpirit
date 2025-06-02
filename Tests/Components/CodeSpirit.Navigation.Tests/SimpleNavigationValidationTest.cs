using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Tests.TestBase;
using System;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests
{
    /// <summary>
    /// 简单的导航验证测试
    /// </summary>
    public class SimpleNavigationValidationTest : NavigationTestBase
    {
        private readonly ITestOutputHelper _output;

        public SimpleNavigationValidationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ValidatePlatformTypeEnumValues()
        {
            _output.WriteLine("=== PlatformType 枚举值验证 ===");
            
            _output.WriteLine($"None: {(int)PlatformType.None} (二进制: {Convert.ToString((int)PlatformType.None, 2).PadLeft(3, '0')})");
            _output.WriteLine($"System: {(int)PlatformType.System} (二进制: {Convert.ToString((int)PlatformType.System, 2).PadLeft(3, '0')})");
            _output.WriteLine($"Tenant: {(int)PlatformType.Tenant} (二进制: {Convert.ToString((int)PlatformType.Tenant, 2).PadLeft(3, '0')})");
            _output.WriteLine($"Inherit: {(int)PlatformType.Inherit} (二进制: {Convert.ToString((int)PlatformType.Inherit, 2).PadLeft(3, '0')})");
            _output.WriteLine($"Both: {(int)PlatformType.Both} (二进制: {Convert.ToString((int)PlatformType.Both, 2).PadLeft(3, '0')})");

            _output.WriteLine("\n=== 位运算测试 ===");
            
            // 测试 System & System
            var systemAndSystem = (PlatformType.System & PlatformType.System) != 0;
            _output.WriteLine($"System & System = {systemAndSystem} (预期: true)");
            
            // 测试 System & Tenant  
            var systemAndTenant = (PlatformType.System & PlatformType.Tenant) != 0;
            _output.WriteLine($"System & Tenant = {systemAndTenant} (预期: false)");
            
            // 测试 System & Both
            var systemAndBoth = (PlatformType.System & PlatformType.Both) != 0;
            _output.WriteLine($"System & Both = {systemAndBoth} (预期: true)");

            // 验证预期结果
            Assert.True(systemAndSystem);
            Assert.False(systemAndTenant);
            Assert.True(systemAndBoth);
            
            _output.WriteLine("✓ 位运算逻辑验证通过");
        }

        [Fact]
        public void TestFilterNodesByPlatform()
        {
            _output.WriteLine("=== FilterNodesByPlatform 基础测试 ===");

            // 创建测试节点
            var systemNode = new CodeSpirit.Navigation.Models.NavigationNode("system", "系统功能", "/system")
            {
                PlatformType = PlatformType.System
            };

            var tenantNode = new CodeSpirit.Navigation.Models.NavigationNode("tenant", "租户功能", "/tenant")
            {
                PlatformType = PlatformType.Tenant
            };

            var bothNode = new CodeSpirit.Navigation.Models.NavigationNode("both", "通用功能", "/both")
            {
                PlatformType = PlatformType.Both
            };

            var allNodes = new System.Collections.Generic.List<CodeSpirit.Navigation.Models.NavigationNode>
            {
                systemNode, tenantNode, bothNode
            };

            _output.WriteLine($"原始节点数: {allNodes.Count}");
            foreach (var node in allNodes)
            {
                _output.WriteLine($"  - {node.Title} (PlatformType: {node.PlatformType})");
            }

            // 测试系统平台过滤
            var systemResult = NavigationService.FilterNodesByPlatform(allNodes, PlatformType.System);
            _output.WriteLine($"\n系统平台过滤结果: {systemResult.Count} 个节点");
            foreach (var node in systemResult)
            {
                _output.WriteLine($"  - {node.Title} (PlatformType: {node.PlatformType})");
            }

            // 验证结果
            Assert.Equal(2, systemResult.Count); // 应该包含 System 和 Both
            Assert.Contains(systemResult, n => n.Name == "system");
            Assert.Contains(systemResult, n => n.Name == "both");
            Assert.DoesNotContain(systemResult, n => n.Name == "tenant");

            _output.WriteLine("✓ FilterNodesByPlatform 测试通过");
        }
    }
} 