namespace CodeSpirit.MultiTenant.Tests.Models;

/// <summary>
/// 租户配置选项单元测试
/// </summary>
public class TenantOptionsTests
{
    /// <summary>
    /// 测试TenantOptions默认值
    /// </summary>
    [Fact]
    public void TenantOptions_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var options = new TenantOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.DefaultTenantId.Should().Be("default");
        options.ResolveFromHeader.Should().BeTrue();
        options.TenantHeaderName.Should().Be("X-Tenant-Id");
        options.ResolveFromQuery.Should().BeFalse();
        options.TenantQueryName.Should().Be("tenantId");
        options.ResolveFromSubdomain.Should().BeFalse();
        options.ResolveFromPath.Should().BeFalse();
        options.TenantPathPrefix.Should().Be("tenant-");
        options.CacheExpirationMinutes.Should().Be(30);
        // StoreType 属性已被移除，现在使用统一的存储策略
        options.EnableTenantValidation.Should().BeTrue();
        options.EnableTenantCache.Should().BeTrue();
        options.FailureStrategy.Should().Be(TenantResolutionFailureStrategy.Return404);
    }

    /// <summary>
    /// 测试配置节名称常量
    /// </summary>
    [Fact]
    public void TenantOptions_ShouldHaveCorrectSectionName()
    {
        // Assert
        TenantOptions.SectionName.Should().Be("MultiTenant");
    }

    /// <summary>
    /// 测试禁用多租户功能
    /// </summary>
    [Fact]
    public void TenantOptions_ShouldAllowDisablingMultiTenant()
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.Enabled = false;

        // Assert
        options.Enabled.Should().BeFalse();
    }

    /// <summary>
    /// 测试自定义默认租户ID
    /// </summary>
    [Theory]
    [InlineData("custom-default")]
    [InlineData("system")]
    [InlineData("main")]
    public void TenantOptions_ShouldAllowCustomDefaultTenantId(string defaultTenantId)
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.DefaultTenantId = defaultTenantId;

        // Assert
        options.DefaultTenantId.Should().Be(defaultTenantId);
    }

    /// <summary>
    /// 测试Header解析配置
    /// </summary>
    [Theory]
    [InlineData(true, "X-Tenant-ID")]
    [InlineData(true, "Tenant")]
    [InlineData(false, "TenantId")]
    public void TenantOptions_ShouldConfigureHeaderResolution(bool enabled, string headerName)
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.ResolveFromHeader = enabled;
        options.TenantHeaderName = headerName;

        // Assert
        options.ResolveFromHeader.Should().Be(enabled);
        options.TenantHeaderName.Should().Be(headerName);
    }

    /// <summary>
    /// 测试Query参数解析配置
    /// </summary>
    [Theory]
    [InlineData(true, "tenant")]
    [InlineData(true, "t")]
    [InlineData(false, "tenantId")]
    public void TenantOptions_ShouldConfigureQueryResolution(bool enabled, string queryName)
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.ResolveFromQuery = enabled;
        options.TenantQueryName = queryName;

        // Assert
        options.ResolveFromQuery.Should().Be(enabled);
        options.TenantQueryName.Should().Be(queryName);
    }

    /// <summary>
    /// 测试子域名解析配置
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TenantOptions_ShouldConfigureSubdomainResolution(bool enabled)
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.ResolveFromSubdomain = enabled;

        // Assert
        options.ResolveFromSubdomain.Should().Be(enabled);
    }

    /// <summary>
    /// 测试路径解析配置
    /// </summary>
    [Theory]
    [InlineData(true, "org-")]
    [InlineData(true, "client-")]
    [InlineData(false, "tenant-")]
    public void TenantOptions_ShouldConfigurePathResolution(bool enabled, string pathPrefix)
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.ResolveFromPath = enabled;
        options.TenantPathPrefix = pathPrefix;

        // Assert
        options.ResolveFromPath.Should().Be(enabled);
        options.TenantPathPrefix.Should().Be(pathPrefix);
    }

    /// <summary>
    /// 测试缓存配置
    /// </summary>
    [Theory]
    [InlineData(true, 60)]
    [InlineData(true, 15)]
    [InlineData(false, 30)]
    public void TenantOptions_ShouldConfigureCache(bool enabled, int expirationMinutes)
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.EnableTenantCache = enabled;
        options.CacheExpirationMinutes = expirationMinutes;

        // Assert
        options.EnableTenantCache.Should().Be(enabled);
        options.CacheExpirationMinutes.Should().Be(expirationMinutes);
    }

    /// <summary>
    /// 测试统一存储策略（不再需要配置存储类型）
    /// </summary>
    [Fact]
    public void TenantOptions_ShouldUseUnifiedStorageStrategy()
    {
        // Arrange
        var options = new TenantOptions();

        // Assert
        // 验证不再需要配置存储类型，现在使用统一的内存→分布式缓存→API策略
        options.EnableTenantCache.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(30);
    }

    /// <summary>
    /// 测试简化后的配置选项
    /// </summary>
    [Fact]
    public void TenantOptions_ShouldHaveSimplifiedConfiguration()
    {
        // Arrange
        var options = new TenantOptions();

        // Assert
        // 验证简化后的配置选项
        options.Should().NotBeNull();
        options.EnableTenantCache.Should().BeTrue();
        options.EnableTenantValidation.Should().BeTrue();
        options.FailureStrategy.Should().Be(TenantResolutionFailureStrategy.Return404);
    }

    /// <summary>
    /// 测试租户验证配置
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TenantOptions_ShouldConfigureTenantValidation(bool enabled)
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.EnableTenantValidation = enabled;

        // Assert
        options.EnableTenantValidation.Should().Be(enabled);
    }

    /// <summary>
    /// 测试失败策略配置
    /// </summary>
    [Theory]
    [InlineData(TenantResolutionFailureStrategy.UseDefault)]
    [InlineData(TenantResolutionFailureStrategy.ThrowException)]
    [InlineData(TenantResolutionFailureStrategy.Return404)]
    public void TenantOptions_ShouldConfigureFailureStrategy(TenantResolutionFailureStrategy strategy)
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.FailureStrategy = strategy;

        // Assert
        options.FailureStrategy.Should().Be(strategy);
    }

    /// <summary>
    /// 测试完整配置场景
    /// </summary>
    [Fact]
    public void TenantOptions_ShouldSupportCompleteConfiguration()
    {
        // Arrange
        var options = new TenantOptions();

        // Act
        options.Enabled = true;
        options.DefaultTenantId = "production";
        options.ResolveFromHeader = true;
        options.TenantHeaderName = "X-Tenant";
        options.ResolveFromQuery = false;
        options.ResolveFromSubdomain = true;
        options.ResolveFromPath = false;
        options.EnableTenantCache = true;
        options.CacheExpirationMinutes = 45;
        // StoreType 属性已被移除，现在使用统一的存储策略
        options.EnableTenantValidation = true;
        options.FailureStrategy = TenantResolutionFailureStrategy.ThrowException;

        // Assert
        options.Enabled.Should().BeTrue();
        options.DefaultTenantId.Should().Be("production");
        options.ResolveFromHeader.Should().BeTrue();
        options.TenantHeaderName.Should().Be("X-Tenant");
        options.ResolveFromQuery.Should().BeFalse();
        options.ResolveFromSubdomain.Should().BeTrue();
        options.ResolveFromPath.Should().BeFalse();
        options.EnableTenantCache.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(45);
        // StoreType 属性已被移除，现在使用统一的存储策略
        options.EnableTenantValidation.Should().BeTrue();
        options.FailureStrategy.Should().Be(TenantResolutionFailureStrategy.ThrowException);
    }
} 