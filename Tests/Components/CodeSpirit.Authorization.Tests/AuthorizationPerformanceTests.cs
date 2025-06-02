using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using CodeSpirit.Authorization;
using CodeSpirit.Authorization.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Authorization.Tests;

/// <summary>
/// 权限验证性能测试
/// </summary>
public class AuthorizationPerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ILogger<PlatformAuthorizationHandler>> _mockPlatformLogger;
    private readonly Mock<ILogger<HasPermissionService>> _mockHasPermissionLogger;
    private readonly Mock<ILogger<PermissionService>> _mockPermissionLogger;
    private readonly Mock<IPermissionService> _mockPermissionService;

    public AuthorizationPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockPlatformLogger = new Mock<ILogger<PlatformAuthorizationHandler>>();
        _mockHasPermissionLogger = new Mock<ILogger<HasPermissionService>>();
        _mockPermissionLogger = new Mock<ILogger<PermissionService>>();
        _mockPermissionService = new Mock<IPermissionService>();
    }

    #region 平台权限验证性能测试

    [Fact]
    public async Task PlatformAuthorization_HighVolumeRequests_ShouldMaintainPerformance()
    {
        // Arrange
        const int requestCount = 1000;
        var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
        
        SetupMockUser(isAuthenticated: true, tenantId: "system", userId: 1L);

        var tasks = new List<Task<TimeSpan>>();
        
        // Act
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                
                var context = new AuthorizationHandlerContext(
                    new[] { new PlatformRequirement(PlatformType.System) },
                    null,
                    null);

                await platformHandler.HandleAsync(context);
                
                stopwatch.Stop();
                return stopwatch.Elapsed;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var totalTime = TimeSpan.FromTicks(results.Sum(r => r.Ticks));
        var averageTime = TimeSpan.FromTicks(totalTime.Ticks / requestCount);
        var maxTime = results.Max();

        _output.WriteLine($"平台权限验证性能测试结果:");
        _output.WriteLine($"请求数量: {requestCount}");
        _output.WriteLine($"总时间: {totalTime.TotalMilliseconds:F2} ms");
        _output.WriteLine($"平均时间: {averageTime.TotalMilliseconds:F4} ms");
        _output.WriteLine($"最大时间: {maxTime.TotalMilliseconds:F2} ms");

        // 性能断言
        Assert.True(averageTime.TotalMilliseconds < 1.0, $"平均响应时间应小于1ms，实际为 {averageTime.TotalMilliseconds:F4} ms");
        Assert.True(maxTime.TotalMilliseconds < 10.0, $"最大响应时间应小于10ms，实际为 {maxTime.TotalMilliseconds:F2} ms");
        Assert.True(totalTime.TotalSeconds < 2.0, $"总时间应小于2秒，实际为 {totalTime.TotalSeconds:F2} 秒");
    }

    [Fact]
    public async Task PlatformAuthorization_ConcurrentRequests_ShouldHandleCorrectly()
    {
        // Arrange
        const int concurrentRequests = 100;
        var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
        
        SetupMockUser(isAuthenticated: true, tenantId: "business-tenant", userId: 2L);

        var results = new ConcurrentBag<bool>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                var context = new AuthorizationHandlerContext(
                    new[] { new PlatformRequirement(PlatformType.Tenant) },
                    null,
                    null);

                await platformHandler.HandleAsync(context);
                results.Add(context.HasSucceeded);
            }))
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"并发平台权限验证测试结果:");
        _output.WriteLine($"并发数量: {concurrentRequests}");
        _output.WriteLine($"执行时间: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"成功请求: {results.Count(r => r)}");
        _output.WriteLine($"失败请求: {results.Count(r => !r)}");

        Assert.Equal(concurrentRequests, results.Count);
        Assert.True(results.All(r => r), "所有并发请求都应该成功");
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"并发请求执行时间应小于1秒，实际为 {stopwatch.ElapsedMilliseconds} ms");
    }

    #endregion

    #region 权限服务性能测试

    [Fact]
    public void PermissionService_LargePermissionSet_ShouldPerformWell()
    {
        // Arrange
        const int permissionCount = 10000;
        const int testIterations = 1000;
        
        var userPermissions = new HashSet<string>();
        for (int i = 0; i < permissionCount; i++)
        {
            userPermissions.Add($"module_{i % 100}_controller_{i % 50}_action_{i % 10}");
        }

        var permissionService = new PermissionService(
            new Mock<IServiceProvider>().Object,
            new Mock<Microsoft.Extensions.Caching.Distributed.IDistributedCache>().Object,
            _mockPermissionLogger.Object);

        var testPermissions = new List<string>();
        for (int i = 0; i < testIterations; i++)
        {
            testPermissions.Add($"module_{i % 100}_controller_{i % 50}_action_{i % 10}_specific");
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var testPermission in testPermissions)
        {
            permissionService.HasPermission(testPermission, userPermissions);
        }
        
        stopwatch.Stop();

        // Assert
        var averageTime = stopwatch.ElapsedMilliseconds / (double)testIterations;
        
        _output.WriteLine($"权限服务性能测试结果:");
        _output.WriteLine($"权限总数: {permissionCount}");
        _output.WriteLine($"测试次数: {testIterations}");
        _output.WriteLine($"总时间: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"平均时间: {averageTime:F4} ms/次");

        Assert.True(averageTime < 0.1, $"平均权限检查时间应小于0.1ms，实际为 {averageTime:F4} ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"总执行时间应小于100ms，实际为 {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void HasPermissionService_HighFrequencyAccess_ShouldMaintainPerformance()
    {
        // Arrange
        const int testCount = 5000;
        var userPermissions = new HashSet<string> 
        { 
            "user_management", "order_management", "product_management", 
            "report_view", "system_config", "tenant_admin" 
        };

        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "Manager" },
            permissions: userPermissions);

        _mockPermissionService.Setup(x => x.HasPermission(It.IsAny<string>(), It.IsAny<HashSet<string>>()))
            .Returns<string, HashSet<string>>((permission, permissions) => 
                permissions.Any(p => permission.StartsWith(p + "_")));

        var hasPermissionService = new HasPermissionService(
            _mockHasPermissionLogger.Object,
            _mockPermissionService.Object,
            _mockCurrentUser.Object);

        var testPermissions = new[]
        {
            "user_management_create", "user_management_edit", "order_management_view",
            "product_management_delete", "report_view_export", "system_config_update"
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < testCount; i++)
        {
            var permission = testPermissions[i % testPermissions.Length];
            hasPermissionService.HasPermission(permission);
        }
        
        stopwatch.Stop();

        // Assert
        var averageTime = stopwatch.ElapsedMilliseconds / (double)testCount * 1000; // 转换为微秒
        
        _output.WriteLine($"HasPermissionService 高频访问测试结果:");
        _output.WriteLine($"测试次数: {testCount}");
        _output.WriteLine($"总时间: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"平均时间: {averageTime:F2} μs/次");

        Assert.True(averageTime < 100, $"平均权限检查时间应小于100μs，实际为 {averageTime:F2} μs");
    }

    #endregion

    #region 内存使用测试

    [Fact]
    public void AuthorizationHandlers_MemoryUsage_ShouldBeReasonable()
    {
        // Arrange
        const int handlerCount = 100; // 减少处理器数量以提高测试稳定性
        
        // 强制GC并等待完成
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(true);
        
        var handlers = new List<PlatformAuthorizationHandler>();

        // Act
        for (int i = 0; i < handlerCount; i++)
        {
            var mockUser = new Mock<ICurrentUser>();
            var mockLogger = new Mock<ILogger<PlatformAuthorizationHandler>>();
            handlers.Add(new PlatformAuthorizationHandler(mockUser.Object, mockLogger.Object));
        }

        var afterCreationMemory = GC.GetTotalMemory(true);
        var memoryUsed = afterCreationMemory - initialMemory;

        // Assert
        var averageMemoryPerHandler = memoryUsed / handlerCount;
        
        _output.WriteLine($"内存使用测试结果:");
        _output.WriteLine($"创建处理器数量: {handlerCount}");
        _output.WriteLine($"总内存使用: {memoryUsed / 1024.0:F2} KB");
        _output.WriteLine($"平均每个处理器: {averageMemoryPerHandler} bytes");

        Assert.True(averageMemoryPerHandler < 5120, $"每个处理器内存使用应小于5KB，实际为 {averageMemoryPerHandler} bytes");
        
        // 清理
        handlers.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    #endregion

    #region 多线程压力测试

    [Fact]
    public async Task AuthorizationSystem_MultiThreadedStressTest_ShouldHandleLoad()
    {
        // Arrange
        const int threadCount = 10;
        const int requestsPerThread = 100;
        
        var results = new ConcurrentBag<(bool Success, TimeSpan Duration, string TenantId)>();
        var globalStopwatch = Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, threadCount)
            .Select(threadIndex => Task.Run(async () =>
            {
                var tenantId = threadIndex % 2 == 0 ? "system" : $"tenant-{threadIndex}";
                var platformType = threadIndex % 2 == 0 ? PlatformType.System : PlatformType.Tenant;

                for (int i = 0; i < requestsPerThread; i++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    var mockUser = new Mock<ICurrentUser>();
                    mockUser.Setup(x => x.IsAuthenticated).Returns(true);
                    mockUser.Setup(x => x.TenantId).Returns(tenantId);
                    mockUser.Setup(x => x.Id).Returns((long)(threadIndex * 1000 + i));

                    var mockLogger = new Mock<ILogger<PlatformAuthorizationHandler>>();
                    var handler = new PlatformAuthorizationHandler(mockUser.Object, mockLogger.Object);
                    
                    var context = new AuthorizationHandlerContext(
                        new[] { new PlatformRequirement(platformType) },
                        null,
                        null);

                    await handler.HandleAsync(context);
                    
                    stopwatch.Stop();
                    results.Add((context.HasSucceeded, stopwatch.Elapsed, tenantId));
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
        globalStopwatch.Stop();

        // Assert
        var totalRequests = threadCount * requestsPerThread;
        var successfulRequests = results.Count(r => r.Success);
        var averageTime = TimeSpan.FromTicks(results.Sum(r => r.Duration.Ticks) / results.Count);
        var systemRequests = results.Count(r => r.TenantId == "system");
        var tenantRequests = results.Count(r => r.TenantId.StartsWith("tenant-"));

        _output.WriteLine($"多线程压力测试结果:");
        _output.WriteLine($"线程数: {threadCount}");
        _output.WriteLine($"每线程请求数: {requestsPerThread}");
        _output.WriteLine($"总请求数: {totalRequests}");
        _output.WriteLine($"成功请求数: {successfulRequests}");
        _output.WriteLine($"系统租户请求数: {systemRequests}");
        _output.WriteLine($"业务租户请求数: {tenantRequests}");
        _output.WriteLine($"总执行时间: {globalStopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"平均请求时间: {averageTime.TotalMilliseconds * 1000:F2} μs");
        _output.WriteLine($"吞吐量: {totalRequests / globalStopwatch.Elapsed.TotalSeconds:F2} 请求/秒");

        Assert.Equal(totalRequests, successfulRequests);
        Assert.True(averageTime.TotalMilliseconds < 1.0, $"平均请求时间应小于1ms，实际为 {averageTime.TotalMilliseconds:F4} ms");
        Assert.True(globalStopwatch.ElapsedMilliseconds < 5000, $"总执行时间应小于5秒，实际为 {globalStopwatch.ElapsedMilliseconds} ms");
    }

    #endregion

    #region 权限继承性能测试

    [Fact]
    public void PermissionInheritance_ComplexHierarchy_ShouldPerformWell()
    {
        // Arrange
        const int testIterations = 1000;
        var permissionService = new PermissionService(
            new Mock<IServiceProvider>().Object,
            new Mock<Microsoft.Extensions.Caching.Distributed.IDistributedCache>().Object,
            _mockPermissionLogger.Object);

        // 创建复杂的权限层次结构
        var userPermissions = new HashSet<string>
        {
            "app",
            "app_module_a",
            "app_module_b_controller_x",
            "app_module_c_controller_y_action_z",
            "system_admin",
            "tenant_management",
            "user_profile_basic"
        };

        var testCases = new[]
        {
            "app_module_a_controller_test_action_create",      // 3级继承
            "app_module_b_controller_x_action_edit",          // 2级继承
            "app_module_c_controller_y_action_z",             // 精确匹配
            "system_admin_configuration_update",              // 2级继承
            "tenant_management_user_roles_assign",            // 3级继承
            "user_profile_basic_edit",                        // 1级继承
            "unrelated_permission_test"                       // 无匹配
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = new List<bool>();

        for (int i = 0; i < testIterations; i++)
        {
            foreach (var testCase in testCases)
            {
                results.Add(permissionService.HasPermission(testCase, userPermissions));
            }
        }

        stopwatch.Stop();

        // Assert
        var totalChecks = testIterations * testCases.Length;
        var averageTime = stopwatch.ElapsedMilliseconds / (double)totalChecks * 1000; // 转换为微秒

        _output.WriteLine($"权限继承性能测试结果:");
        _output.WriteLine($"测试迭代: {testIterations}");
        _output.WriteLine($"权限案例数: {testCases.Length}");
        _output.WriteLine($"总检查次数: {totalChecks}");
        _output.WriteLine($"总时间: {stopwatch.ElapsedMilliseconds} ms");
        _output.WriteLine($"平均时间: {averageTime:F2} μs/次");

        Assert.True(averageTime < 50, $"平均权限继承检查时间应小于50μs，实际为 {averageTime:F2} μs");
        
        // 验证结果正确性
        var expectedSuccesses = testIterations * 6; // 前6个应该成功，最后1个失败
        var actualSuccesses = results.Count(r => r);
        Assert.Equal(expectedSuccesses, actualSuccesses);
    }

    #endregion

    #region 辅助方法

    private void SetupMockUser(bool isAuthenticated, string tenantId = "test-tenant", long? userId = null,
        string[] roles = null, HashSet<string> permissions = null)
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
        _mockCurrentUser.Setup(x => x.TenantId).Returns(tenantId);
        _mockCurrentUser.Setup(x => x.Id).Returns(userId);
        _mockCurrentUser.Setup(x => x.Roles).Returns(roles ?? new[] { "User" });
        _mockCurrentUser.Setup(x => x.Permissions).Returns(permissions ?? new HashSet<string>());
        
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
        if (!string.IsNullOrEmpty(tenantId))
        {
            claims.Add(new Claim("TenantId", tenantId));
        }
        _mockCurrentUser.Setup(x => x.Claims).Returns(claims);
    }

    #endregion
} 