using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text;

namespace CodeSpirit.MultiTenant.Tests.Performance;

/// <summary>
/// 多租户性能测试
/// </summary>
public class MultiTenantPerformanceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<TenantResolver>> _loggerMock;
    private readonly Mock<ILogger<MemoryTenantStore>> _storeLoggerMock;
    private readonly MemoryTenantStore _tenantStore;
    private readonly Mock<IOptions<TenantOptions>> _optionsMock;
    private readonly TenantOptions _tenantOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MultiTenantPerformanceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<TenantResolver>>();
        _storeLoggerMock = new Mock<ILogger<MemoryTenantStore>>();
        _tenantStore = new MemoryTenantStore(_storeLoggerMock.Object);
        _optionsMock = new Mock<IOptions<TenantOptions>>();

        _tenantOptions = new TenantOptions
        {
            DefaultTenantId = "default",
            ResolveFromHeader = true,
            TenantHeaderName = "TenantId",
            EnableTenantCache = true,
            CacheExpirationMinutes = 30
        };

        _optionsMock.Setup(x => x.Value).Returns(_tenantOptions);

        // 初始化测试数据
        InitializeTestData().Wait();
    }

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    private async Task InitializeTestData()
    {
        // 创建1000个测试租户
        var tasks = new List<Task>();
        for (int i = 1; i <= 1000; i++)
        {
            var tenant = new TenantInfo
            {
                TenantId = $"tenant-{i:D4}",
                Name = $"租户{i}",
                DisplayName = $"测试租户{i}",
                Strategy = TenantStrategy.SharedDatabase,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            tasks.Add(_tenantStore.CreateTenantAsync(tenant));
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 测试大量租户存储的查询性能
    /// </summary>
    [Fact]
    public async Task TenantStore_ShouldHandleLargeDataSet_Efficiently()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<ITenantInfo?>>();

        // Act - 并发查询100个不同的租户
        for (int i = 1; i <= 100; i++)
        {
            var tenantId = $"tenant-{i:D4}";
            tasks.Add(_tenantStore.GetTenantAsync(tenantId));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(tenant => tenant.Should().NotBeNull());
        
        // 性能断言：100个查询应该在1秒内完成
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        
        // 输出性能指标
        var avgTimePerQuery = stopwatch.ElapsedMilliseconds / 100.0;
        Console.WriteLine($"平均查询时间: {avgTimePerQuery:F2}ms");
    }

    /// <summary>
    /// 测试高并发租户解析性能
    /// </summary>
    [Fact]
    public async Task TenantResolver_ShouldHandleHighConcurrency_Efficiently()
    {
        // Arrange
        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStore,
            _optionsMock.Object);

        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<ITenantInfo?>>();

        // Act - 1000个并发请求
        for (int i = 1; i <= 1000; i++)
        {
            var tenantId = $"tenant-{(i % 100) + 1:D4}"; // 重复使用前100个租户
            tasks.Add(resolver.GetTenantInfoAsync(tenantId));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(1000);
        results.Should().AllSatisfy(tenant => tenant.Should().NotBeNull());
        
        // 性能断言：1000个并发请求应该在5秒内完成
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
        
        // 输出性能指标
        var avgTimePerRequest = stopwatch.ElapsedMilliseconds / 1000.0;
        var requestsPerSecond = 1000.0 / (stopwatch.ElapsedMilliseconds / 1000.0);
        Console.WriteLine($"平均请求时间: {avgTimePerRequest:F2}ms");
        Console.WriteLine($"每秒请求数: {requestsPerSecond:F0} RPS");
    }

    /// <summary>
    /// 测试内存使用效率
    /// </summary>
    [Fact]
    public async Task TenantStore_ShouldUseMemoryEfficiently()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act - 添加更多租户
        var tasks = new List<Task>();
        for (int i = 1001; i <= 2000; i++)
        {
            var tenant = new TenantInfo
            {
                TenantId = $"tenant-{i:D4}",
                Name = $"租户{i}",
                DisplayName = $"测试租户{i}",
                Strategy = TenantStrategy.SharedDatabase,
                IsActive = true
            };
            tasks.Add(_tenantStore.CreateTenantAsync(tenant));
        }
        await Task.WhenAll(tasks);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        // 1000个租户的内存增长应该在合理范围内（小于10MB）
        memoryIncrease.Should().BeLessThan(10 * 1024 * 1024);
        
        Console.WriteLine($"内存增长: {memoryIncrease / 1024.0:F2} KB");
        Console.WriteLine($"每个租户平均内存: {memoryIncrease / 1000.0:F2} bytes");
    }

    /// <summary>
    /// 测试缓存性能提升
    /// </summary>
    [Fact]
    public async Task TenantResolver_ShouldBenefitFromCaching()
    {
        // Arrange
        var cachedTenantInfo = new TenantInfo
        {
            TenantId = "cached-tenant",
            Name = "缓存租户",
            IsActive = true
        };

        // 模拟缓存命中
        var cachedData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cachedTenantInfo));
        _cacheMock.Setup(x => x.GetAsync("tenant_info_cached-tenant", default))
                  .ReturnsAsync(cachedData);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStore,
            _optionsMock.Object);

        // Act - 测试缓存性能
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<ITenantInfo?>>();

        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(resolver.GetTenantInfoAsync("cached-tenant"));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(1000);
        results.Should().AllSatisfy(tenant => 
        {
            tenant.Should().NotBeNull();
            tenant!.TenantId.Should().Be("cached-tenant");
        });

        // 缓存查询应该非常快（小于100ms）
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        
        Console.WriteLine($"缓存查询总时间: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"平均缓存查询时间: {stopwatch.ElapsedMilliseconds / 1000.0:F3}ms");
    }

    /// <summary>
    /// 测试租户解析的吞吐量
    /// </summary>
    [Fact]
    public async Task TenantResolver_ShouldAchieveHighThroughput()
    {
        // Arrange
        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStore,
            _optionsMock.Object);

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var stopwatch = Stopwatch.StartNew();
        var completedRequests = 0;
        var duration = TimeSpan.FromSeconds(5); // 运行5秒

        // Act - 持续发送请求5秒钟
        var tasks = new List<Task>();
        while (stopwatch.Elapsed < duration)
        {
            for (int i = 0; i < 10; i++) // 每批10个请求
            {
                var tenantId = $"tenant-{(i % 10) + 1:D4}";
                httpContext.Request.Headers["TenantId"] = tenantId;
                
                tasks.Add(Task.Run(async () =>
                {
                    await resolver.ResolveTenantIdAsync();
                    Interlocked.Increment(ref completedRequests);
                }));
            }
            
            if (tasks.Count >= 100) // 限制并发任务数
            {
                await Task.WhenAll(tasks.Take(50));
                tasks.RemoveRange(0, 50);
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var requestsPerSecond = completedRequests / stopwatch.Elapsed.TotalSeconds;
        
        // 应该能达到至少1000 RPS
        requestsPerSecond.Should().BeGreaterThan(1000);
        
        Console.WriteLine($"总请求数: {completedRequests}");
        Console.WriteLine($"运行时间: {stopwatch.Elapsed.TotalSeconds:F2}秒");
        Console.WriteLine($"吞吐量: {requestsPerSecond:F0} RPS");
    }

    /// <summary>
    /// 测试大量租户的列表查询性能
    /// </summary>
    [Fact]
    public async Task TenantStore_ShouldHandleLargeListQueries_Efficiently()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - 获取所有租户
        var allTenants = await _tenantStore.GetActiveTenantsAsync();
        stopwatch.Stop();

        // Assert
        // 包含1000个测试租户 + 1个默认租户
        allTenants.Should().HaveCount(1001);
        
        // 获取1000个租户应该在500ms内完成
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        
        Console.WriteLine($"查询{allTenants.Count()}个租户耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 测试租户搜索性能
    /// </summary>
    [Theory]
    [InlineData("tenant-0001")]
    [InlineData("tenant-0500")]
    [InlineData("tenant-1000")]
    public async Task TenantStore_ShouldSearchEfficiently(string searchTenantId)
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tenant = await _tenantStore.GetTenantAsync(searchTenantId);
        stopwatch.Stop();

        // Assert
        tenant.Should().NotBeNull();
        tenant!.TenantId.Should().Be(searchTenantId);
        
        // 单个租户查询应该在10ms内完成
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10);
        
        Console.WriteLine($"查询租户 {searchTenantId} 耗时: {stopwatch.ElapsedMilliseconds}ms");
    }
} 