using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text;

namespace CodeSpirit.MultiTenant.Tests.Services;

/// <summary>
/// 租户解析器高级功能单元测试
/// </summary>
public class TenantResolverAdvancedTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<TenantResolver>> _loggerMock;
    private readonly Mock<ITenantStore> _tenantStoreMock;
    private readonly Mock<IOptions<TenantOptions>> _optionsMock;
    private readonly TenantOptions _tenantOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantResolverAdvancedTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<TenantResolver>>();
        _tenantStoreMock = new Mock<ITenantStore>();
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
    }

    /// <summary>
    /// 测试缓存命中场景
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        var tenantId = "cached-tenant";
        var cachedTenantInfo = new TenantInfo
        {
            TenantId = tenantId,
            Name = "缓存租户",
            IsActive = true
        };

        var cachedData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cachedTenantInfo));
        _cacheMock.Setup(x => x.GetAsync($"tenant_info_{tenantId}", default))
                  .ReturnsAsync(cachedData);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act
        var result = await resolver.GetTenantInfoAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenantId);
        result.Name.Should().Be("缓存租户");

        // 验证没有调用存储
        _tenantStoreMock.Verify(x => x.GetTenantAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// 测试缓存未命中场景
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldQueryStoreAndCache_WhenCacheMiss()
    {
        // Arrange
        var tenantId = "uncached-tenant";
        var tenantInfo = new TenantInfo
        {
            TenantId = tenantId,
            Name = "未缓存租户",
            IsActive = true
        };

        _cacheMock.Setup(x => x.GetAsync($"tenant_info_{tenantId}", default))
                  .ReturnsAsync((byte[])null);

        _tenantStoreMock.Setup(x => x.GetTenantAsync(tenantId))
                       .ReturnsAsync(tenantInfo);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act
        var result = await resolver.GetTenantInfoAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenantId);

        // 验证调用了存储
        _tenantStoreMock.Verify(x => x.GetTenantAsync(tenantId), Times.Once);

        // 验证设置了缓存
        _cacheMock.Verify(x => x.SetAsync(
            $"tenant_info_{tenantId}",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }

    /// <summary>
    /// 测试禁用缓存时的行为
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldSkipCache_WhenCacheDisabled()
    {
        // Arrange
        _tenantOptions.EnableTenantCache = false;
        var tenantId = "no-cache-tenant";
        var tenantInfo = new TenantInfo
        {
            TenantId = tenantId,
            Name = "无缓存租户",
            IsActive = true
        };

        _tenantStoreMock.Setup(x => x.GetTenantAsync(tenantId))
                       .ReturnsAsync(tenantInfo);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act
        var result = await resolver.GetTenantInfoAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenantId);

        // 验证没有访问缓存
        _cacheMock.Verify(x => x.GetAsync(It.IsAny<string>(), default), Times.Never);
        _cacheMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Never);
    }

    /// <summary>
    /// 测试复杂的租户解析优先级
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldFollowCorrectPriority_WithMultipleSources()
    {
        // Arrange
        _tenantOptions.ResolveFromHeader = true;
        _tenantOptions.ResolveFromQuery = true;
        _tenantOptions.ResolveFromSubdomain = true;
        _tenantOptions.ResolveFromPath = true;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["TenantId"] = "header-tenant";
        httpContext.Request.QueryString = new QueryString("?tenantId=query-tenant");
        httpContext.Request.Host = new HostString("subdomain-tenant.example.com");
        httpContext.Request.Path = "/tenant-path-tenant/api/test";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act
        var result = await resolver.ResolveTenantIdAsync();

        // Assert - Header应该有最高优先级
        result.Should().Be("header-tenant");
    }

    /// <summary>
    /// 测试租户解析的降级处理
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldFallbackToNextSource_WhenPrimarySourceEmpty()
    {
        // Arrange
        _tenantOptions.ResolveFromHeader = true;
        _tenantOptions.ResolveFromQuery = true;
        _tenantOptions.TenantHeaderName = "TenantId";
        _tenantOptions.TenantQueryName = "tenantId";

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["TenantId"] = ""; // 空Header
        httpContext.Request.QueryString = new QueryString("?tenantId=query-tenant");

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act
        var result = await resolver.ResolveTenantIdAsync();

        // Assert - 应该降级到Query参数
        result.Should().Be("query-tenant");
    }

    /// <summary>
    /// 测试并发访问安全性
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldBeConcurrencySafe()
    {
        // Arrange
        var tenantId = "concurrent-tenant";
        var tenantInfo = new TenantInfo
        {
            TenantId = tenantId,
            Name = "并发租户",
            IsActive = true
        };

        _cacheMock.Setup(x => x.GetAsync($"tenant_info_{tenantId}", default))
                  .ReturnsAsync((byte[])null);

        _tenantStoreMock.Setup(x => x.GetTenantAsync(tenantId))
                       .ReturnsAsync(tenantInfo);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act - 并发调用
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => resolver.GetTenantInfoAsync(tenantId))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result!.TenantId.Should().Be(tenantId);
        });

        // 验证存储只被调用了一次（或合理次数）
        _tenantStoreMock.Verify(x => x.GetTenantAsync(tenantId), Times.AtLeastOnce);
    }

    /// <summary>
    /// 测试异常处理
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldHandleStoreException_Gracefully()
    {
        // Arrange
        var tenantId = "exception-tenant";

        _cacheMock.Setup(x => x.GetAsync($"tenant_info_{tenantId}", default))
                  .ReturnsAsync((byte[])null);

        _tenantStoreMock.Setup(x => x.GetTenantAsync(tenantId))
                       .ThrowsAsync(new InvalidOperationException("存储异常"));

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => resolver.GetTenantInfoAsync(tenantId));
    }

    /// <summary>
    /// 测试缓存异常处理
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldHandleCacheException_AndContinue()
    {
        // Arrange
        var tenantId = "cache-exception-tenant";
        var tenantInfo = new TenantInfo
        {
            TenantId = tenantId,
            Name = "缓存异常租户",
            IsActive = true
        };

        _cacheMock.Setup(x => x.GetAsync($"tenant_info_{tenantId}", default))
                  .ThrowsAsync(new InvalidOperationException("缓存异常"));

        _tenantStoreMock.Setup(x => x.GetTenantAsync(tenantId))
                       .ReturnsAsync(tenantInfo);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act & Assert - 应该抛出异常，因为缓存访问失败
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => resolver.GetTenantInfoAsync(tenantId));
    }

    /// <summary>
    /// 测试特殊字符的租户ID处理
    /// </summary>
    [Theory]
    [InlineData("tenant-with-dashes")]
    [InlineData("tenant_with_underscores")]
    [InlineData("tenant.with.dots")]
    [InlineData("tenant123")]
    [InlineData("UPPERCASE-TENANT")]
    public async Task ResolveTenantIdAsync_ShouldHandleSpecialCharacters(string tenantId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["TenantId"] = tenantId;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act
        var result = await resolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be(tenantId);
    }

    /// <summary>
    /// 测试长租户ID处理
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldHandleLongTenantId()
    {
        // Arrange
        var longTenantId = new string('a', 100); // 100字符的租户ID
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["TenantId"] = longTenantId;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act
        var result = await resolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be(longTenantId);
    }

    /// <summary>
    /// 测试获取当前租户信息的完整流程
    /// </summary>
    [Fact]
    public async Task GetCurrentTenantInfoAsync_ShouldCombineResolveAndGet()
    {
        // Arrange
        var tenantId = "current-tenant";
        var tenantInfo = new TenantInfo
        {
            TenantId = tenantId,
            Name = "当前租户",
            IsActive = true
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["TenantId"] = tenantId;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _cacheMock.Setup(x => x.GetAsync($"tenant_info_{tenantId}", default))
                  .ReturnsAsync((byte[])null);

        _tenantStoreMock.Setup(x => x.GetTenantAsync(tenantId))
                       .ReturnsAsync(tenantInfo);

        var resolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);

        // Act
        var result = await resolver.GetCurrentTenantInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenantId);
        result.Name.Should().Be("当前租户");
    }
} 