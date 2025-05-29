using Microsoft.Extensions.Caching.Memory;

namespace CodeSpirit.MultiTenant.Tests.Services;

/// <summary>
/// 租户解析器单元测试
/// </summary>
public class TenantResolverTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<TenantResolver>> _loggerMock;
    private readonly Mock<ITenantStore> _tenantStoreMock;
    private readonly Mock<IOptions<TenantOptions>> _optionsMock;
    private readonly TenantResolver _tenantResolver;
    private readonly TenantOptions _tenantOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantResolverTests()
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
            ResolveFromQuery = true,
            TenantQueryName = "tenantId",
            EnableTenantCache = false // 禁用缓存以简化测试
        };

        _optionsMock.Setup(x => x.Value).Returns(_tenantOptions);

        _tenantResolver = new TenantResolver(
            _httpContextAccessorMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _tenantStoreMock.Object,
            _optionsMock.Object);
    }

    /// <summary>
    /// 测试从Header解析租户ID
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldReturnTenantIdFromHeader_WhenHeaderExists()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["TenantId"] = "test-tenant";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _tenantResolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be("test-tenant");
    }

    /// <summary>
    /// 测试从Query参数解析租户ID
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldReturnTenantIdFromQuery_WhenQueryExists()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?tenantId=query-tenant");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _tenantResolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be("query-tenant");
    }

    /// <summary>
    /// 测试Header优先级高于Query
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldPrioritizeHeader_WhenBothHeaderAndQueryExist()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["TenantId"] = "header-tenant";
        httpContext.Request.QueryString = new QueryString("?tenantId=query-tenant");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _tenantResolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be("header-tenant");
    }

    /// <summary>
    /// 测试返回默认租户ID
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldReturnDefaultTenantId_WhenNoTenantFound()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _tenantResolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be("default");
    }

    /// <summary>
    /// 测试无HTTP上下文时返回默认租户ID
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldReturnDefaultTenantId_WhenNoHttpContext()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);

        // Act
        var result = await _tenantResolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be("default");
    }

    /// <summary>
    /// 测试获取租户信息
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldReturnTenantInfo_WhenTenantExists()
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            TenantId = "test",
            Name = "测试租户",
            IsActive = true
        };

        _tenantStoreMock.Setup(x => x.GetTenantAsync("test"))
                       .ReturnsAsync(tenantInfo);

        // Act
        var result = await _tenantResolver.GetTenantInfoAsync("test");

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be("test");
        result.Name.Should().Be("测试租户");
    }

    /// <summary>
    /// 测试获取不存在的租户信息
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldReturnNull_WhenTenantNotExists()
    {
        // Arrange
        _tenantStoreMock.Setup(x => x.GetTenantAsync("nonexistent"))
                       .ReturnsAsync((ITenantInfo)null);

        // Act
        var result = await _tenantResolver.GetTenantInfoAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 测试空租户ID返回null
    /// </summary>
    [Fact]
    public async Task GetTenantInfoAsync_ShouldReturnNull_WhenTenantIdIsEmpty()
    {
        // Act
        var result = await _tenantResolver.GetTenantInfoAsync("");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 测试获取活跃租户列表
    /// </summary>
    [Fact]
    public async Task GetActiveTenantInfosAsync_ShouldReturnActiveTenants()
    {
        // Arrange
        var activeTenants = new List<ITenantInfo>
        {
            new TenantInfo { TenantId = "tenant1", Name = "租户1", IsActive = true },
            new TenantInfo { TenantId = "tenant2", Name = "租户2", IsActive = true }
        };

        _tenantStoreMock.Setup(x => x.GetActiveTenantsAsync())
                       .ReturnsAsync(activeTenants);

        // Act
        var result = await _tenantResolver.GetActiveTenantInfosAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.IsActive);
    }

    /// <summary>
    /// 测试获取当前租户信息
    /// </summary>
    [Fact]
    public async Task GetCurrentTenantInfoAsync_ShouldReturnCurrentTenantInfo()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["TenantId"] = "current-tenant";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var tenantInfo = new TenantInfo
        {
            TenantId = "current-tenant",
            Name = "当前租户",
            IsActive = true
        };

        _tenantStoreMock.Setup(x => x.GetTenantAsync("current-tenant"))
                       .ReturnsAsync(tenantInfo);

        // Act
        var result = await _tenantResolver.GetCurrentTenantInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be("current-tenant");
        result.Name.Should().Be("当前租户");
    }

    /// <summary>
    /// 测试从子域名解析租户ID
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldReturnTenantIdFromSubdomain_WhenSubdomainResolutionEnabled()
    {
        // Arrange
        _tenantOptions.ResolveFromSubdomain = true;
        _tenantOptions.ResolveFromHeader = false;
        _tenantOptions.ResolveFromQuery = false;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("tenant1.example.com");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _tenantResolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be("tenant1");
    }

    /// <summary>
    /// 测试从路径解析租户ID
    /// </summary>
    [Fact]
    public async Task ResolveTenantIdAsync_ShouldReturnTenantIdFromPath_WhenPathResolutionEnabled()
    {
        // Arrange
        _tenantOptions.ResolveFromPath = true;
        _tenantOptions.ResolveFromHeader = false;
        _tenantOptions.ResolveFromQuery = false;
        _tenantOptions.ResolveFromSubdomain = false;
        _tenantOptions.TenantPathPrefix = "tenant-";

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/tenant-abc123/api/users";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _tenantResolver.ResolveTenantIdAsync();

        // Assert
        result.Should().Be("abc123");
    }
} 