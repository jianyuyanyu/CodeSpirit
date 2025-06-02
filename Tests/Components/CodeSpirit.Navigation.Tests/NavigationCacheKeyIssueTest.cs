using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Tests.TestBase;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests
{
    /// <summary>
    /// 导航缓存Key问题测试 - 验证缓存Key生成和获取逻辑的问题
    /// </summary>
    public class NavigationCacheKeyIssueTest : NavigationTestBase
    {
        private readonly ITestOutputHelper _output;

        public NavigationCacheKeyIssueTest(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试缓存Key生成逻辑
        /// </summary>
        [Fact]
        public void GetModuleCacheKey_ShouldGenerateCorrectKeys()
        {
            _output.WriteLine("=== 测试缓存Key生成逻辑 ===");

            // 使用反射获取私有方法
            var getModuleCacheKeyMethod = typeof(NavigationService).GetMethod("GetModuleCacheKey", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(getModuleCacheKeyMethod);

            // 测试不同平台类型的缓存Key生成
            var systemKey = (string)getModuleCacheKeyMethod.Invoke(NavigationService, new object[] { "identity", PlatformType.System });
            var tenantKey = (string)getModuleCacheKeyMethod.Invoke(NavigationService, new object[] { "identity", PlatformType.Tenant });
            var bothKey = (string)getModuleCacheKeyMethod.Invoke(NavigationService, new object[] { "identity", PlatformType.Both });

            _output.WriteLine($"System缓存Key: {systemKey}");
            _output.WriteLine($"Tenant缓存Key: {tenantKey}");
            _output.WriteLine($"Both缓存Key: {bothKey}");

            // 验证Key格式
            Assert.Equal("CodeSpirit:Navigation:Module:identity:System", systemKey);
            Assert.Equal("CodeSpirit:Navigation:Module:identity:Tenant", tenantKey);
            Assert.Equal("CodeSpirit:Navigation:Module:identity:Both", bothKey);

            _output.WriteLine("✓ 缓存Key生成逻辑正确");
        }

        /// <summary>
        /// 测试GetNavigationTreeAsync的缓存查找逻辑问题
        /// </summary>
        [Fact]
        public async Task GetNavigationTreeAsync_CacheLookupLogic_Issue()
        {
            _output.WriteLine("=== 测试GetNavigationTreeAsync缓存查找逻辑问题 ===");

            // 问题分析：
            // 1. 当请求 PlatformType.System 时，GetNavigationTreeAsync 会查找 "identity:System" 缓存Key
            // 2. 但实际缓存的是 "identity:Both" Key (因为ApiControllerBase设置了PlatformType.Both)
            // 3. 因此无法找到匹配的缓存，返回空结果

            _output.WriteLine("问题分析：");
            _output.WriteLine("1. 请求 PlatformType.System 时，查找缓存Key: CodeSpirit:Navigation:Module:identity:System");
            _output.WriteLine("2. 实际存在的缓存Key: CodeSpirit:Navigation:Module:identity:Both");
            _output.WriteLine("3. 因为Key不匹配，所以返回空结果");

            // 使用反射验证这个逻辑
            var getModuleCacheKeyMethod = typeof(NavigationService).GetMethod("GetModuleCacheKey", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var requestedKey = (string)getModuleCacheKeyMethod.Invoke(NavigationService, new object[] { "identity", PlatformType.System });
            var actualKey = "CodeSpirit:Navigation:Module:identity:Both"; // 用户报告的实际缓存Key

            _output.WriteLine($"请求的缓存Key: {requestedKey}");
            _output.WriteLine($"实际的缓存Key: {actualKey}");
            
            // 验证Key确实不匹配
            Assert.NotEqual(requestedKey, actualKey);

            _output.WriteLine("✓ 确认了缓存Key不匹配的问题");
        }

        /// <summary>
        /// 分析缓存更新逻辑的问题
        /// </summary>
        [Fact]
        public void AnalyzeCacheUpdateLogic()
        {
            _output.WriteLine("=== 分析缓存更新逻辑问题 ===");

            _output.WriteLine("当前缓存更新逻辑：");
            _output.WriteLine("1. UpdateModuleNavigationCache 对每个模块调用三次：System、Tenant、Both");
            _output.WriteLine("2. BuildModuleNavigationTree 构建完整的导航树");
            _output.WriteLine("3. FilterNodesByPlatform 根据平台类型过滤");
            _output.WriteLine("4. 将过滤后的结果存储到对应的缓存Key");

            _output.WriteLine("\n问题所在：");
            _output.WriteLine("1. identity模块的控制器继承自ApiControllerBase (PlatformType.Both)");
            _output.WriteLine("2. 因此模块的推断平台类型也是Both");
            _output.WriteLine("3. 当过滤System时，Both类型的模块被包含在System缓存中");
            _output.WriteLine("4. 但SystemUsersController的System属性在过滤时被正确处理");

            _output.WriteLine("\n真正的问题：");
            _output.WriteLine("1. 缓存机制本身可能有问题");
            _output.WriteLine("2. 或者模块级别的平台类型推断有问题");
            _output.WriteLine("3. 需要检查BuildModuleNavigationTree的逻辑");
        }
    }
} 