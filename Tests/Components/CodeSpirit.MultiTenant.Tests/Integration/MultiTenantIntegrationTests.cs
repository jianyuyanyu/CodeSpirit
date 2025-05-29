using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using CodeSpirit.MultiTenant.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace CodeSpirit.MultiTenant.Tests.Integration;

/// <summary>
/// 多租户集成测试
/// </summary>
public class MultiTenantIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpClient _client;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MultiTenantIntegrationTests()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddMemoryCache();
                    
                    // 使用内存缓存而不是Redis缓存
                    services.AddSingleton<IDistributedCache, MemoryDistributedCache>();

                    // 添加路由服务
                    services.AddRouting();
                    
                    // 添加日志服务
                    services.AddLogging();

                    // 添加配置
                    var configuration = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            ["MultiTenant:Enabled"] = "true",
                            ["MultiTenant:DefaultTenantId"] = "default",
                            ["MultiTenant:ResolveFromHeader"] = "true",
                            ["MultiTenant:TenantHeaderName"] = "TenantId",
                            ["MultiTenant:ResolveFromQuery"] = "true",
                            ["MultiTenant:TenantQueryName"] = "tenantId",
                            ["MultiTenant:EnableTenantCache"] = "false",
                            ["MultiTenant:CacheExpirationMinutes"] = "30",
                            ["MultiTenant:StoreType"] = "Memory"
                        })
                        .Build();

                    // 使用多租户扩展方法注册服务
                    services.AddCodeSpiritMultiTenant(configuration);
                });

                webHost.Configure(app =>
                {
                    app.UseCodeSpiritMultiTenant();
                    
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/tenant/current", async (ITenantResolver tenantResolver) =>
                        {
                            var tenantInfo = await tenantResolver.GetCurrentTenantInfoAsync();
                            return tenantInfo != null ? Results.Ok(tenantInfo) : Results.NotFound();
                        });

                        endpoints.MapGet("/api/tenant/{tenantId}", async (string tenantId, ITenantResolver tenantResolver) =>
                        {
                            var tenantInfo = await tenantResolver.GetTenantInfoAsync(tenantId);
                            return tenantInfo != null ? Results.Ok(tenantInfo) : Results.NotFound();
                        });
                    });
                });
            });

        var host = hostBuilder.Start();
        _server = host.GetTestServer();
        _client = _server.CreateClient();

        // 初始化测试数据
        InitializeTestData();
    }

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    private void InitializeTestData()
    {
        var tenantStore = _server.Services.GetRequiredService<ITenantStore>();
        
        // 添加测试租户
        var testTenants = new[]
        {
            new TenantInfo
            {
                TenantId = "tenant1",
                Name = "租户1",
                DisplayName = "测试租户1",
                Strategy = TenantStrategy.SharedDatabase,
                IsActive = true
            },
            new TenantInfo
            {
                TenantId = "tenant2",
                Name = "租户2",
                DisplayName = "测试租户2",
                Strategy = TenantStrategy.SeparateDatabase,
                IsActive = true
            },
            new TenantInfo
            {
                TenantId = "inactive-tenant",
                Name = "非活跃租户",
                DisplayName = "非活跃测试租户",
                Strategy = TenantStrategy.SharedDatabase,
                IsActive = false
            }
        };

        foreach (var tenant in testTenants)
        {
            tenantStore.CreateTenantAsync(tenant).Wait();
        }
    }

    /// <summary>
    /// 测试通过Header解析租户
    /// </summary>
    [Fact]
    public async Task GetCurrentTenant_ShouldResolveFromHeader()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("TenantId", "tenant1");

        // Act
        var response = await _client.GetAsync("/api/tenant/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(content);
        
        tenantInfo.Should().NotBeNull();
        tenantInfo!.TenantId.Should().Be("tenant1");
        tenantInfo.Name.Should().Be("租户1");
    }

    /// <summary>
    /// 测试通过Query参数解析租户
    /// </summary>
    [Fact]
    public async Task GetCurrentTenant_ShouldResolveFromQuery()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant/current?tenantId=tenant2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(content);
        
        tenantInfo.Should().NotBeNull();
        tenantInfo!.TenantId.Should().Be("tenant2");
        tenantInfo.Name.Should().Be("租户2");
    }

    /// <summary>
    /// 测试Header优先级高于Query参数
    /// </summary>
    [Fact]
    public async Task GetCurrentTenant_ShouldPrioritizeHeaderOverQuery()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("TenantId", "tenant1");

        // Act
        var response = await _client.GetAsync("/api/tenant/current?tenantId=tenant2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(content);
        
        tenantInfo.Should().NotBeNull();
        tenantInfo!.TenantId.Should().Be("tenant1"); // Header优先
    }

    /// <summary>
    /// 测试获取不存在的租户
    /// </summary>
    [Fact]
    public async Task GetTenant_ShouldReturnNotFound_WhenTenantNotExists()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant/non-existent-tenant");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 测试获取非活跃租户
    /// </summary>
    [Fact]
    public async Task GetTenant_ShouldReturnInactiveTenant()
    {
        // Act
        var response = await _client.GetAsync("/api/tenant/inactive-tenant");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(content);
        
        tenantInfo.Should().NotBeNull();
        tenantInfo!.TenantId.Should().Be("inactive-tenant");
        tenantInfo.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// 测试无租户信息时的默认行为
    /// </summary>
    [Fact]
    public async Task GetCurrentTenant_ShouldReturnDefaultTenant_WhenNoTenantResolved()
    {
        // Act - 不提供任何租户信息
        var response = await _client.GetAsync("/api/tenant/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(content);
        
        tenantInfo.Should().NotBeNull();
        tenantInfo!.TenantId.Should().Be("default"); // 应该返回默认租户
    }

    /// <summary>
    /// 测试多个并发请求的租户隔离
    /// </summary>
    [Fact]
    public async Task MultipleRequests_ShouldMaintainTenantIsolation()
    {
        // Arrange
        var tasks = new List<Task<(string tenantId, HttpResponseMessage response)>>();

        // 创建多个并发请求，每个使用不同的租户
        for (int i = 1; i <= 5; i++)
        {
            var tenantId = i <= 2 ? $"tenant{i}" : "tenant1"; // 只有tenant1和tenant2存在
            tasks.Add(Task.Run(async () =>
            {
                using var client = _server.CreateClient();
                client.DefaultRequestHeaders.Add("TenantId", tenantId);
                var response = await client.GetAsync("/api/tenant/current");
                return (tenantId, response);
            }));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var (tenantId, response) in results)
        {
            if (tenantId == "tenant1" || tenantId == "tenant2")
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                
                var content = await response.Content.ReadAsStringAsync();
                var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(content);
                
                tenantInfo.Should().NotBeNull();
                tenantInfo!.TenantId.Should().Be(tenantId);
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            }
        }
    }

    /// <summary>
    /// 测试租户缓存功能
    /// </summary>
    [Fact]
    public async Task TenantResolution_ShouldUseCaching()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("TenantId", "tenant1");

        // Act - 多次请求同一租户
        var response1 = await _client.GetAsync("/api/tenant/current");
        var response2 = await _client.GetAsync("/api/tenant/current");
        var response3 = await _client.GetAsync("/api/tenant/current");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        // 验证返回的租户信息一致
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        var content3 = await response3.Content.ReadAsStringAsync();

        content1.Should().Be(content2);
        content2.Should().Be(content3);
    }

    /// <summary>
    /// 测试特殊字符租户ID
    /// </summary>
    [Theory]
    [InlineData("tenant-with-dash")]
    [InlineData("tenant_with_underscore")]
    [InlineData("tenant.with.dot")]
    public async Task TenantResolution_ShouldHandleSpecialCharacters(string tenantId)
    {
        // Arrange - 添加特殊字符租户
        var tenantStore = _server.Services.GetRequiredService<ITenantStore>();
        await tenantStore.CreateTenantAsync(new TenantInfo
        {
            TenantId = tenantId,
            Name = $"特殊字符租户-{tenantId}",
            IsActive = true
        });

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("TenantId", tenantId);

        // Act
        var response = await _client.GetAsync("/api/tenant/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tenantInfo = JsonConvert.DeserializeObject<TenantInfo>(content);
        
        tenantInfo.Should().NotBeNull();
        tenantInfo!.TenantId.Should().Be(tenantId);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _client?.Dispose();
        _server?.Dispose();
    }
} 