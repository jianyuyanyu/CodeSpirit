using CodeSpirit.Navigation.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Navigation.Tests.Extensions
{
    /// <summary>
    /// 分布式缓存扩展方法单元测试
    /// </summary>
    public class DistributedCacheExtensionsTests
    {
        private readonly IDistributedCache _cache;
        private readonly ITestOutputHelper _testOutputHelper;

        public DistributedCacheExtensionsTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            // 设置内存缓存作为分布式缓存
            var services = new ServiceCollection();
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
            services.AddOptions();
            services.AddMemoryCache();
            var serviceProvider = services.BuildServiceProvider();
            _cache = serviceProvider.GetRequiredService<IDistributedCache>();
        }

        /// <summary>
        /// 测试设置和获取对象到缓存 - 验证序列化和反序列化
        /// </summary>
        [Fact]
        public async Task SetAndGetAsync_WithComplexObject_ShouldStoreAndRetrieveCorrectly()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试设置和获取对象到缓存");

            // 准备测试数据
            var testKey = "test_key_" + Guid.NewGuid().ToString("N");
            var testData = new List<string> { "item1", "item2", "item3" };
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            // 执行测试 - 设置缓存
            await _cache.SetAsync(testKey, testData, cacheOptions);
            _testOutputHelper.WriteLine($"测试设置和获取对象到缓存 - 设置缓存键: {testKey}");

            // 执行测试 - 获取缓存
            var result = await _cache.GetAsync<List<string>>(testKey);

            // 验证结果
            Assert.NotNull(result);
            Assert.Equal(testData.Count, result.Count);
            Assert.Equal(testData[0], result[0]);
            Assert.Equal(testData[1], result[1]);
            Assert.Equal(testData[2], result[2]);

            _testOutputHelper.WriteLine($"测试设置和获取对象到缓存 - 成功获取缓存, 项数: {result.Count}");
        }

        /// <summary>
        /// 测试获取不存在的缓存项 - 应返回默认值
        /// </summary>
        [Fact]
        public async Task GetAsync_WithNonExistingKey_ShouldReturnNull()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试获取不存在的缓存项");

            // 准备测试数据
            var nonExistingKey = "non_existing_key_" + Guid.NewGuid().ToString("N");

            // 执行测试
            var result = await _cache.GetAsync<List<string>>(nonExistingKey);

            // 验证结果
            Assert.Null(result);

            _testOutputHelper.WriteLine("测试获取不存在的缓存项 - 成功返回null值");
        }

        /// <summary>
        /// 测试缓存项过期 - 验证过期后获取结果为null
        /// </summary>
        [Fact]
        public async Task GetAsync_WithExpiredItem_ShouldReturnNull()
        {
            // 记录测试信息
            _testOutputHelper.WriteLine("测试缓存项过期");

            // 准备测试数据
            var testKey = "expired_key_" + Guid.NewGuid().ToString("N");
            var testData = new List<string> { "temp_item" };
            
            // 设置极短的过期时间
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(50)
            };

            // 执行测试 - 设置缓存
            await _cache.SetAsync(testKey, testData, cacheOptions);
            _testOutputHelper.WriteLine($"测试缓存项过期 - 设置缓存键: {testKey}, 过期时间: 50ms");

            // 等待缓存过期
            await Task.Delay(100);
            _testOutputHelper.WriteLine("测试缓存项过期 - 等待100ms使缓存过期");

            // 执行测试 - 获取缓存
            var result = await _cache.GetAsync<List<string>>(testKey);

            // 验证结果
            Assert.Null(result);

            _testOutputHelper.WriteLine("测试缓存项过期 - 成功验证过期缓存返回null");
        }
    }
} 