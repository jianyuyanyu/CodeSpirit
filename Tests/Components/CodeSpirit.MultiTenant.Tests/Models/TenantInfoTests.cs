namespace CodeSpirit.MultiTenant.Tests.Models;

/// <summary>
/// 租户信息模型单元测试
/// </summary>
public class TenantInfoTests
{
    /// <summary>
    /// 测试TenantInfo默认值
    /// </summary>
    [Fact]
    public void TenantInfo_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var tenantInfo = new TenantInfo();

        // Assert
        tenantInfo.TenantId.Should().Be(string.Empty);
        tenantInfo.Name.Should().Be(string.Empty);
        tenantInfo.DisplayName.Should().Be(string.Empty);
        tenantInfo.Description.Should().Be(string.Empty);
        tenantInfo.Strategy.Should().Be(TenantStrategy.SharedDatabase);
        tenantInfo.ConnectionString.Should().Be(string.Empty);
        tenantInfo.TablePrefix.Should().Be(string.Empty);
        tenantInfo.IsActive.Should().BeTrue();
        tenantInfo.Configuration.Should().Be("{}");
        tenantInfo.Domain.Should().Be(string.Empty);
        tenantInfo.LogoUrl.Should().Be(string.Empty);
        tenantInfo.ThemeConfig.Should().Be("{}");
        tenantInfo.MaxUsers.Should().Be(1000);
        tenantInfo.StorageLimit.Should().Be(10240);
        tenantInfo.ExpiresAt.Should().BeNull();
        tenantInfo.IsDeleted.Should().BeFalse();
    }

    /// <summary>
    /// 测试TenantInfo属性设置
    /// </summary>
    [Fact]
    public void TenantInfo_ShouldAllowPropertySetting()
    {
        // Arrange
        var tenantInfo = new TenantInfo();
        var now = DateTime.UtcNow;

        // Act
        tenantInfo.TenantId = "test-tenant";
        tenantInfo.Name = "测试租户";
        tenantInfo.DisplayName = "测试租户显示名";
        tenantInfo.Description = "这是一个测试租户";
        tenantInfo.Strategy = TenantStrategy.SeparateDatabase;
        tenantInfo.ConnectionString = "Server=localhost;Database=TestTenant;";
        tenantInfo.TablePrefix = "test_";
        tenantInfo.IsActive = false;
        tenantInfo.Configuration = "{\"theme\":\"dark\"}";
        tenantInfo.Domain = "test.example.com";
        tenantInfo.LogoUrl = "https://example.com/logo.png";
        tenantInfo.ThemeConfig = "{\"primaryColor\":\"#007bff\"}";
        tenantInfo.MaxUsers = 500;
        tenantInfo.StorageLimit = 5120;
        tenantInfo.ExpiresAt = now.AddYears(1);
        tenantInfo.CreatedAt = now;
        tenantInfo.CreatedBy = 1;
        tenantInfo.UpdatedAt = now;
        tenantInfo.UpdatedBy = 2;

        // Assert
        tenantInfo.TenantId.Should().Be("test-tenant");
        tenantInfo.Name.Should().Be("测试租户");
        tenantInfo.DisplayName.Should().Be("测试租户显示名");
        tenantInfo.Description.Should().Be("这是一个测试租户");
        tenantInfo.Strategy.Should().Be(TenantStrategy.SeparateDatabase);
        tenantInfo.ConnectionString.Should().Be("Server=localhost;Database=TestTenant;");
        tenantInfo.TablePrefix.Should().Be("test_");
        tenantInfo.IsActive.Should().BeFalse();
        tenantInfo.Configuration.Should().Be("{\"theme\":\"dark\"}");
        tenantInfo.Domain.Should().Be("test.example.com");
        tenantInfo.LogoUrl.Should().Be("https://example.com/logo.png");
        tenantInfo.ThemeConfig.Should().Be("{\"primaryColor\":\"#007bff\"}");
        tenantInfo.MaxUsers.Should().Be(500);
        tenantInfo.StorageLimit.Should().Be(5120);
        tenantInfo.ExpiresAt.Should().Be(now.AddYears(1));
        tenantInfo.CreatedAt.Should().Be(now);
        tenantInfo.CreatedBy.Should().Be(1);
        tenantInfo.UpdatedAt.Should().Be(now);
        tenantInfo.UpdatedBy.Should().Be(2);
    }

    /// <summary>
    /// 测试TenantInfo实现ITenantInfo接口
    /// </summary>
    [Fact]
    public void TenantInfo_ShouldImplementITenantInfo()
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            TenantId = "interface-test",
            Name = "接口测试",
            Strategy = TenantStrategy.SharedDatabaseSeparateSchema,
            ConnectionString = "test-connection",
            TablePrefix = "prefix_",
            IsActive = true,
            Configuration = "{\"test\":true}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        ITenantInfo iTenantInfo = tenantInfo;

        // Assert
        iTenantInfo.TenantId.Should().Be("interface-test");
        iTenantInfo.Name.Should().Be("接口测试");
        iTenantInfo.Strategy.Should().Be(TenantStrategy.SharedDatabaseSeparateSchema);
        iTenantInfo.ConnectionString.Should().Be("test-connection");
        iTenantInfo.TablePrefix.Should().Be("prefix_");
        iTenantInfo.IsActive.Should().BeTrue();
        iTenantInfo.Configuration.Should().Be("{\"test\":true}");
        iTenantInfo.CreatedAt.Should().Be(tenantInfo.CreatedAt);
        iTenantInfo.UpdatedAt.Should().Be(tenantInfo.UpdatedAt);
    }

    /// <summary>
    /// 测试软删除功能
    /// </summary>
    [Fact]
    public void TenantInfo_ShouldSupportSoftDelete()
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            TenantId = "soft-delete-test",
            Name = "软删除测试",
            IsActive = true
        };

        // Act
        tenantInfo.IsDeleted = true;
        tenantInfo.DeletedAt = DateTime.UtcNow;
        tenantInfo.DeletedBy = 999;

        // Assert
        tenantInfo.IsDeleted.Should().BeTrue();
        tenantInfo.DeletedAt.Should().NotBeNull();
        tenantInfo.DeletedBy.Should().Be(999);
    }

    /// <summary>
    /// 测试租户过期功能
    /// </summary>
    [Fact]
    public void TenantInfo_ShouldSupportExpiration()
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            TenantId = "expiration-test",
            Name = "过期测试"
        };

        var futureDate = DateTime.UtcNow.AddMonths(6);
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert - 未过期
        tenantInfo.ExpiresAt = futureDate;
        (tenantInfo.ExpiresAt > DateTime.UtcNow).Should().BeTrue();

        // Act & Assert - 已过期
        tenantInfo.ExpiresAt = pastDate;
        (tenantInfo.ExpiresAt < DateTime.UtcNow).Should().BeTrue();

        // Act & Assert - 永不过期
        tenantInfo.ExpiresAt = null;
        tenantInfo.ExpiresAt.Should().BeNull();
    }

    /// <summary>
    /// 测试租户限制配置
    /// </summary>
    [Theory]
    [InlineData(100, 1024)]
    [InlineData(1000, 10240)]
    [InlineData(5000, 51200)]
    public void TenantInfo_ShouldSupportLimitsConfiguration(int maxUsers, long storageLimit)
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            TenantId = "limits-test",
            Name = "限制测试"
        };

        // Act
        tenantInfo.MaxUsers = maxUsers;
        tenantInfo.StorageLimit = storageLimit;

        // Assert
        tenantInfo.MaxUsers.Should().Be(maxUsers);
        tenantInfo.StorageLimit.Should().Be(storageLimit);
    }

    /// <summary>
    /// 测试不同租户策略
    /// </summary>
    [Theory]
    [InlineData(TenantStrategy.SharedDatabase)]
    [InlineData(TenantStrategy.SharedDatabaseSeparateSchema)]
    [InlineData(TenantStrategy.SeparateDatabase)]
    [InlineData(TenantStrategy.Hybrid)]
    public void TenantInfo_ShouldSupportAllTenantStrategies(TenantStrategy strategy)
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            TenantId = $"strategy-test-{strategy}",
            Name = $"策略测试-{strategy}"
        };

        // Act
        tenantInfo.Strategy = strategy;

        // Assert
        tenantInfo.Strategy.Should().Be(strategy);
    }
} 