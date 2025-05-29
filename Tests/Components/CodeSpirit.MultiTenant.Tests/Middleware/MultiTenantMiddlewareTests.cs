using CodeSpirit.MultiTenant.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CodeSpirit.MultiTenant.Tests.Middleware;

/// <summary>
/// 多租户中间件单元测试
/// </summary>
public class MultiTenantMiddlewareTests
{
    /// <summary>
    /// 测试中间件成功解析租户
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldSetTenantInfo_WhenTenantResolutionSucceeds()
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            TenantId = "test-tenant",
            Name = "测试租户",
            IsActive = true
        };

        using var host = await CreateTestHost(tenantInfo, "test-tenant");
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/?tenantId=test-tenant");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("test-tenant");
    }

    /// <summary>
    /// 测试租户不存在时的处理
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldUseDefaultTenant_WhenTenantNotExists()
    {
        // Arrange
        using var host = await CreateTestHost(null, "default");
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/?tenantId=nonexistent");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("default");
    }

    /// <summary>
    /// 测试租户被禁用时的处理
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldUseDefaultTenant_WhenTenantIsInactive()
    {
        // Arrange
        var inactiveTenantInfo = new TenantInfo
        {
            TenantId = "inactive-tenant",
            Name = "非活跃租户",
            IsActive = false
        };

        using var host = await CreateTestHost(inactiveTenantInfo, "default");
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/?tenantId=inactive-tenant");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("default");
    }

    /// <summary>
    /// 测试多租户功能禁用时的处理
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldSkipTenantResolution_WhenMultiTenantDisabled()
    {
        // Arrange
        var options = new TenantOptions { Enabled = false };
        using var host = await CreateTestHost(null, null, options);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/?tenantId=any-tenant");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("No tenant");
    }

    /// <summary>
    /// 创建测试主机
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <param name="resolvedTenantId">解析到的租户ID</param>
    /// <param name="options">租户选项</param>
    /// <returns>测试主机</returns>
    private async Task<IHost> CreateTestHost(ITenantInfo? tenantInfo, string? resolvedTenantId = null, TenantOptions? options = null)
    {
        var tenantOptions = options ?? new TenantOptions
        {
            Enabled = true,
            DefaultTenantId = "default",
            ResolveFromQuery = true,
            TenantQueryName = "tenantId"
        };

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.Configure(app =>
                {
                    app.UseMiddleware<MultiTenantMiddleware>();
                    app.Run(async context =>
                    {
                        var tenantId = context.Items["TenantId"] as string;
                        var currentTenantInfo = context.Items["TenantInfo"] as ITenantInfo;
                        
                        if (string.IsNullOrEmpty(tenantId))
                        {
                            await context.Response.WriteAsync("No tenant");
                        }
                        else
                        {
                            await context.Response.WriteAsync($"Tenant: {tenantId}");
                        }
                    });
                });
                webHost.ConfigureServices(services =>
                {
                    services.Configure<TenantOptions>(opt =>
                    {
                        opt.Enabled = tenantOptions.Enabled;
                        opt.DefaultTenantId = tenantOptions.DefaultTenantId;
                        opt.ResolveFromQuery = tenantOptions.ResolveFromQuery;
                        opt.TenantQueryName = tenantOptions.TenantQueryName;
                    });

                    // Mock ITenantResolver
                    var tenantResolverMock = new Mock<ITenantResolver>();
                    
                    // 修复Mock设置 - 根据传入的resolvedTenantId返回相应的租户ID
                    tenantResolverMock.Setup(x => x.ResolveTenantIdAsync())
                                     .ReturnsAsync(() =>
                                     {
                                         return resolvedTenantId ?? tenantOptions.DefaultTenantId;
                                     });

                    tenantResolverMock.Setup(x => x.GetTenantInfoAsync(It.IsAny<string>()))
                                     .ReturnsAsync((string id) =>
                                     {
                                         if (tenantInfo != null && tenantInfo.TenantId == id)
                                         {
                                             return tenantInfo;
                                         }
                                         return null;
                                     });

                    services.AddSingleton(tenantResolverMock.Object);
                    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                    services.AddLogging();
                });
            });

        return await hostBuilder.StartAsync();
    }
} 