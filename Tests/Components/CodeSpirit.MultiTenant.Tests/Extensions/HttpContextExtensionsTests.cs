using CodeSpirit.MultiTenant.Extensions;

namespace CodeSpirit.MultiTenant.Tests.Extensions;

/// <summary>
/// HTTP上下文扩展方法单元测试
/// </summary>
public class HttpContextExtensionsTests
{
    /// <summary>
    /// 测试获取租户ID - 当未设置时返回null
    /// </summary>
    [Fact]
    public void GetTenantId_ShouldReturnNull_WhenNotSet()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var tenantId = httpContext.GetTenantId();

        // Assert
        tenantId.Should().BeNull();
    }

    /// <summary>
    /// 测试设置和获取租户ID
    /// </summary>
    [Theory]
    [InlineData("tenant-001")]
    [InlineData("test-tenant")]
    [InlineData("default")]
    public void SetTenantId_ShouldAllowGettingTenantId(string expectedTenantId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        httpContext.SetTenantId(expectedTenantId);
        var actualTenantId = httpContext.GetTenantId();

        // Assert
        actualTenantId.Should().Be(expectedTenantId);
    }

    /// <summary>
    /// 测试获取租户信息 - 当未设置时返回null
    /// </summary>
    [Fact]
    public void GetTenantInfo_ShouldReturnNull_WhenNotSet()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var tenantInfo = httpContext.GetTenantInfo();

        // Assert
        tenantInfo.Should().BeNull();
    }

    /// <summary>
    /// 测试设置和获取租户信息
    /// </summary>
    [Fact]
    public void SetTenantInfo_ShouldAllowGettingTenantInfo()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var expectedTenantInfo = new TenantInfo
        {
            TenantId = "test-tenant",
            Name = "测试租户",
            Strategy = TenantStrategy.SharedDatabase,
            IsActive = true
        };

        // Act
        httpContext.SetTenantInfo(expectedTenantInfo);
        var actualTenantInfo = httpContext.GetTenantInfo();

        // Assert
        actualTenantInfo.Should().NotBeNull();
        actualTenantInfo.Should().BeSameAs(expectedTenantInfo);
        actualTenantInfo!.TenantId.Should().Be("test-tenant");
        actualTenantInfo.Name.Should().Be("测试租户");
        actualTenantInfo.Strategy.Should().Be(TenantStrategy.SharedDatabase);
        actualTenantInfo.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// 测试覆盖租户ID
    /// </summary>
    [Fact]
    public void SetTenantId_ShouldOverridePreviousValue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        httpContext.SetTenantId("first-tenant");
        httpContext.SetTenantId("second-tenant");
        var tenantId = httpContext.GetTenantId();

        // Assert
        tenantId.Should().Be("second-tenant");
    }

    /// <summary>
    /// 测试覆盖租户信息
    /// </summary>
    [Fact]
    public void SetTenantInfo_ShouldOverridePreviousValue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var firstTenantInfo = new TenantInfo
        {
            TenantId = "first-tenant",
            Name = "第一个租户"
        };
        var secondTenantInfo = new TenantInfo
        {
            TenantId = "second-tenant",
            Name = "第二个租户"
        };

        // Act
        httpContext.SetTenantInfo(firstTenantInfo);
        httpContext.SetTenantInfo(secondTenantInfo);
        var tenantInfo = httpContext.GetTenantInfo();

        // Assert
        tenantInfo.Should().BeSameAs(secondTenantInfo);
        tenantInfo!.TenantId.Should().Be("second-tenant");
        tenantInfo.Name.Should().Be("第二个租户");
    }

    /// <summary>
    /// 测试设置null租户ID
    /// </summary>
    [Fact]
    public void SetTenantId_ShouldAllowNullValue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        httpContext.SetTenantId("test-tenant");
        httpContext.SetTenantId(null);
        var tenantId = httpContext.GetTenantId();

        // Assert
        tenantId.Should().BeNull();
    }

    /// <summary>
    /// 测试设置null租户信息
    /// </summary>
    [Fact]
    public void SetTenantInfo_ShouldAllowNullValue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tenantInfo = new TenantInfo
        {
            TenantId = "test-tenant",
            Name = "测试租户"
        };

        // Act
        httpContext.SetTenantInfo(tenantInfo);
        httpContext.SetTenantInfo(null);
        var result = httpContext.GetTenantInfo();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 测试同时设置租户ID和租户信息
    /// </summary>
    [Fact]
    public void HttpContext_ShouldSupportBothTenantIdAndTenantInfo()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tenantInfo = new TenantInfo
        {
            TenantId = "info-tenant",
            Name = "信息租户"
        };

        // Act
        httpContext.SetTenantId("id-tenant");
        httpContext.SetTenantInfo(tenantInfo);

        // Assert
        httpContext.GetTenantId().Should().Be("id-tenant");
        httpContext.GetTenantInfo().Should().BeSameAs(tenantInfo);
        httpContext.GetTenantInfo()!.TenantId.Should().Be("info-tenant");
    }

    /// <summary>
    /// 测试空字符串租户ID
    /// </summary>
    [Fact]
    public void SetTenantId_ShouldAllowEmptyString()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        httpContext.SetTenantId(string.Empty);
        var tenantId = httpContext.GetTenantId();

        // Assert
        tenantId.Should().Be(string.Empty);
    }

    /// <summary>
    /// 测试多个HTTP上下文的隔离性
    /// </summary>
    [Fact]
    public void HttpContextExtensions_ShouldIsolateBetweenContexts()
    {
        // Arrange
        var context1 = new DefaultHttpContext();
        var context2 = new DefaultHttpContext();
        var tenantInfo1 = new TenantInfo { TenantId = "tenant1", Name = "租户1" };
        var tenantInfo2 = new TenantInfo { TenantId = "tenant2", Name = "租户2" };

        // Act
        context1.SetTenantId("tenant1");
        context1.SetTenantInfo(tenantInfo1);
        context2.SetTenantId("tenant2");
        context2.SetTenantInfo(tenantInfo2);

        // Assert
        context1.GetTenantId().Should().Be("tenant1");
        context1.GetTenantInfo().Should().BeSameAs(tenantInfo1);
        context2.GetTenantId().Should().Be("tenant2");
        context2.GetTenantInfo().Should().BeSameAs(tenantInfo2);
    }
} 