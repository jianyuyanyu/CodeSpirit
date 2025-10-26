#pragma warning disable CS8917 // 无法推断委托类型
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.Audit.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit.Abstractions;

namespace CodeSpirit.Audit.Tests.Integration;

/// <summary>
/// 审计功能集成测试
/// </summary>
public class AuditIntegrationTests : CodeSpirit.Audit.Tests.Infrastructure.IntegrationTestBase
{
    private Infrastructure.InMemoryAuditService? _auditService;
    
    private Infrastructure.InMemoryAuditService AuditService 
    {
        get
        {
            if (_auditService == null)
            {
                var service = GetService<IAuditService>();
                _auditService = service as Infrastructure.InMemoryAuditService;
                if (_auditService == null)
                {
                    throw new InvalidOperationException("无法获取InMemoryAuditService服务");
                }
            }
            return _auditService;
        }
    }

    public AuditIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    // 重写服务配置，添加测试特定的服务
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // 添加配置
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Audit:Enabled"] = "true",
                ["Audit:LogRequestParams"] = "true",
                ["Audit:LogResponseData"] = "true",
                ["Audit:LogUnauthorizedRequests"] = "true",
                ["Audit:LogAnonymousRequests"] = "true",
                // 禁用RabbitMQ
                ["Audit:UseRabbitMQ"] = "false"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        // 添加审计服务 (明确指定使用AuditExtensions)
        AuditExtensions.AddAuditServices(services, configuration);
        
        // 配置审计选项
        services.Configure<AuditOptions>(options => {
            options.Enabled = true;
            options.LogRequestParams = true;
            options.LogResponseData = true;
        });

        // 使用内存审计服务
        services.AddSingleton<IAuditService, CodeSpirit.Audit.Tests.Infrastructure.InMemoryAuditService>();
        
        // 使用模拟的RabbitMQ服务
        services.AddSingleton<IRabbitMQService, MockRabbitMQService>();
        
        // 添加模拟的客户端IP服务
        services.AddSingleton<CodeSpirit.Shared.Services.IClientIpService, MockClientIpService>();

        // 添加模拟的当前用户服务
        services.AddSingleton<CodeSpirit.Core.ICurrentUser, MockCurrentUser>();

        // 添加控制器并注册测试控制器
        services.AddControllers();
    }

    // 配置中间件
    protected override void ConfigureAuditMiddleware(IApplicationBuilder app)
    {
        // 使用审计中间件
        app.UseAudit();
    }

    [Fact]
    public async Task Get_ControllerWithMethodLevelAudit_ShouldAuditRequest()
    {
        // 安排
        base._output.WriteLine("测试方法级别审计特性的GET请求");
        AuditService.ClearLogs();

        // 执行
        var response = await base._client.GetAsync("/api/MethodLevelAudit");

        // 断言
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        base._output.WriteLine($"响应内容: {content}");

        // 验证审计日志
        var logs = AuditService.GetAuditLogs();
        Assert.NotEmpty(logs);
        var log = logs.First();

        // 验证请求信息
        Assert.Contains("/api/MethodLevelAudit", log.RequestPath);
        Assert.Equal("Query", log.OperationType);
        Assert.Equal("测试获取操作", log.OperationName);

        base._output.WriteLine($"审计日志已创建: {JsonSerializer.Serialize(log, base._jsonOptions)}");
    }

    [Fact]
    public async Task Post_ControllerWithMethodLevelAudit_ShouldAuditRequestWithBody()
    {
        // 安排
        base._output.WriteLine("测试方法级别审计特性的POST请求");
        AuditService.ClearLogs();
        var requestDto = new TestDto
        {
            Name = "集成测试",
            Description = "这是一个集成测试",
            Value = 100
        };

        // 执行
        var response = await base._client.PostAsJsonAsync("/api/MethodLevelAudit", requestDto);

        // 断言
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        base._output.WriteLine($"响应内容: {content}");

        // 验证审计日志
        var logs = AuditService.GetAuditLogs();
        Assert.NotEmpty(logs);
        var log = logs.First();

        // 验证请求信息
        Assert.Contains("/api/MethodLevelAudit", log.RequestPath);
        Assert.Equal("Create", log.OperationType);
        Assert.Equal("测试创建操作", log.OperationName);
        
        // 解析RequestParams并验证内容
        if (!string.IsNullOrEmpty(log.RequestParams))
        {
            base._output.WriteLine($"RequestParams: {log.RequestParams}");
            var requestParams = JsonSerializer.Deserialize<TestDto>(log.RequestParams, base._jsonOptions);
            Assert.NotNull(requestParams);
            Assert.Equal("集成测试", requestParams.Name);
        }
        else
        {
            Assert.Fail("RequestParams为空");
        }

        base._output.WriteLine($"审计日志已创建: {JsonSerializer.Serialize(log, base._jsonOptions)}");
    }

    [Fact]
    public async Task Put_ControllerWithMethodLevelAudit_ShouldAuditRequestWithIdParam()
    {
        // 安排
        base._output.WriteLine("测试方法级别审计特性的PUT请求");
        AuditService.ClearLogs();
        var id = 123;
        var requestDto = new TestDto
        {
            Name = "更新测试",
            Description = "这是一个更新操作",
            Value = 200
        };

        // 执行
        var response = await base._client.PutAsJsonAsync($"/api/MethodLevelAudit/{id}", requestDto);

        // 断言
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        base._output.WriteLine($"响应内容: {content}");

        // 验证审计日志
        var logs = AuditService.GetAuditLogs();
        Assert.NotEmpty(logs);
        var log = logs.First();

        // 验证请求信息
        Assert.Contains($"/api/MethodLevelAudit/{id}", log.RequestPath);
        Assert.Equal("Update", log.OperationType);
        Assert.Equal("测试更新操作", log.OperationName);
        
        // 解析RequestParams并验证内容
        if (!string.IsNullOrEmpty(log.RequestParams))
        {
            base._output.WriteLine($"RequestParams: {log.RequestParams}");
            var requestParams = JsonSerializer.Deserialize<TestDto>(log.RequestParams, base._jsonOptions);
            Assert.NotNull(requestParams);
            Assert.Equal("更新测试", requestParams.Name);
        }
        else
        {
            Assert.Fail("RequestParams为空");
        }
        
        // 验证实体信息
        Assert.Contains("123", log.RequestPath); // 实体ID现在存储在RequestPath中

        base._output.WriteLine($"审计日志已创建: {JsonSerializer.Serialize(log, base._jsonOptions)}");
    }

    [Fact]
    public async Task Get_ControllerWithControllerLevelAudit_ShouldAuditRequest()
    {
        // 安排
        base._output.WriteLine("测试控制器级别审计特性的GET请求");
        AuditService.ClearLogs();

        // 执行
        var response = await base._client.GetAsync("/api/ControllerLevelAudit");

        // 断言
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        base._output.WriteLine($"响应内容: {content}");

        // 验证审计日志
        var logs = AuditService.GetAuditLogs();
        Assert.NotEmpty(logs);
        var log = logs.First();

        // 验证请求信息
        Assert.Contains("/api/ControllerLevelAudit", log.RequestPath);
        Assert.Equal("Action", log.OperationType); // 默认操作类型

        base._output.WriteLine($"控制器级别审计日志已创建: {JsonSerializer.Serialize(log, base._jsonOptions)}");
    }

    [Fact(Skip = "由于PipeWriter问题暂时忽略")]
    public void Get_NoAuditController_ShouldNotAuditRequest()
    {
        // 安排
        base._output.WriteLine("测试无审计特性的控制器");
        AuditService.ClearLogs();
        
        // 先发送一个审计请求，确保审计服务正常工作
        base._client.GetAsync("/api/MethodLevelAudit").Wait();
        
        // 清除日志
        AuditService.ClearLogs();
        
        // 执行 - 发送请求到NoAudit控制器
        var response = base._client.GetAsync("/api/NoAudit").Result;
        
        // 断言
        response.EnsureSuccessStatusCode();
        
        // 验证审计日志 - 应该没有日志
        var logs = AuditService.GetAuditLogs();
        base._output.WriteLine($"发送NoAudit请求后的日志数量: {logs.Count()}");
        
        // 应该没有日志
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Get_CustomAuditController_WithCustomConfig_ShouldRespectConfig()
    {
        // 安排
        base._output.WriteLine("测试自定义审计配置 - 不记录响应");
        AuditService.ClearLogs();

        // 执行
        var response = await base._client.GetAsync("/api/CustomAudit");

        // 断言
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        base._output.WriteLine($"响应内容: {content}");

        // 验证审计日志
        var logs = AuditService.GetAuditLogs();
        Assert.NotEmpty(logs);
        var log = logs.First();

        // 验证请求信息
        Assert.Contains("/api/CustomAudit", log.RequestPath);
        Assert.Equal("Query", log.OperationType);
        Assert.Equal("自定义审计配置-不记录响应", log.OperationName);

        // 验证敏感数据没有被记录
        Assert.False(log.AfterData?.Contains("这个不应该被记录") ?? false);

        base._output.WriteLine($"自定义审计日志已创建: {JsonSerializer.Serialize(log, base._jsonOptions)}");
    }

    [Fact]
    public async Task Options_AnyEndpoint_ShouldNotAuditRequest()
    {
        // 安排
        base._output.WriteLine("测试OPTIONS请求过滤功能 - CORS预检请求不应被审计");
        AuditService.ClearLogs();

        // 执行 - 发送OPTIONS请求（CORS预检请求）
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/MethodLevelAudit");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");
        
        var response = await base._client.SendAsync(request);

        // 断言
        // OPTIONS请求可能返回404或其他状态码，这里不验证响应状态
        base._output.WriteLine($"OPTIONS请求响应状态码: {response.StatusCode}");

        // 验证审计日志 - 应该没有日志被记录
        var logs = AuditService.GetAuditLogs();
        base._output.WriteLine($"发送OPTIONS请求后的日志数量: {logs.Count()}");
        
        // 应该没有日志，因为OPTIONS请求被过滤了
        Assert.Empty(logs);
        
        base._output.WriteLine("OPTIONS请求成功被过滤，未记录审计日志");
    }

    [Fact]
    public async Task Options_MultipleEndpoints_ShouldNotAuditAnyRequest()
    {
        // 安排
        base._output.WriteLine("测试多个OPTIONS请求过滤功能");
        AuditService.ClearLogs();

        // 执行 - 发送多个OPTIONS请求到不同端点
        var endpoints = new[] { "/api/MethodLevelAudit", "/api/ControllerLevelAudit", "/api/CustomAudit" };
        
        foreach (var endpoint in endpoints)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, endpoint);
            request.Headers.Add("Origin", "http://localhost:3000");
            request.Headers.Add("Access-Control-Request-Method", "GET");
            
            var response = await base._client.SendAsync(request);
            base._output.WriteLine($"OPTIONS请求到 {endpoint}，响应状态码: {response.StatusCode}");
        }

        // 断言
        // 验证审计日志 - 应该没有任何日志被记录
        var logs = AuditService.GetAuditLogs();
        base._output.WriteLine($"发送多个OPTIONS请求后的日志数量: {logs.Count()}");
        
        // 应该没有日志，因为所有OPTIONS请求都被过滤了
        Assert.Empty(logs);
        
        base._output.WriteLine("所有OPTIONS请求成功被过滤，未记录任何审计日志");
    }
}

/// <summary>
/// 内存审计服务（仅用于测试）
/// </summary>
public class InMemoryAuditService : IAuditService
{
    private static readonly List<Models.AuditLog> _logs = new List<Models.AuditLog>();
    private readonly ILogger<InMemoryAuditService> _logger;
    private readonly AuditOptions _options;

    public InMemoryAuditService(IConfiguration configuration, ILogger<InMemoryAuditService> logger)
    {
        _logger = logger;
        
        // 获取配置
        _options = new AuditOptions();
        configuration.GetSection("Audit").Bind(_options);
    }

    public Task LogAsync(Models.AuditLog auditLog)
    {
        _logger.LogDebug("记录审计日志: {Id}", auditLog.Id);
        lock (_logs)
        {
            auditLog.Id ??= Guid.NewGuid().ToString();
            _logs.Add(auditLog);
        }
        return Task.CompletedTask;
    }

    public Task<Models.AuditLog?> GetByIdAsync(string id)
    {
        lock (_logs)
        {
            return Task.FromResult(_logs.FirstOrDefault(l => l.Id == id));
        }
    }

    public Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        Dictionary<string, long> stats = new Dictionary<string, long>();
        
        lock (_logs)
        {
            var logsInRange = _logs.Where(l => l.OperationTime >= startTime && l.OperationTime <= endTime);
            if (tenantId != null)
            {
                logsInRange = logsInRange.Where(l => l.TenantId == tenantId);
            }
            stats = logsInRange
                .GroupBy(l => l.OperationType ?? "未知")
                .ToDictionary(g => g.Key, g => (long)g.Count());
        }
        
        return Task.FromResult(stats);
    }

    public Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10, string? tenantId = null)
    {
        Dictionary<string, long> stats = new Dictionary<string, long>();
        
        lock (_logs)
        {
            var logsInRange = _logs.Where(l => l.OperationTime >= startTime && l.OperationTime <= endTime);
            if (tenantId != null)
            {
                logsInRange = logsInRange.Where(l => l.TenantId == tenantId);
            }
            stats = logsInRange
                .Where(l => !string.IsNullOrEmpty(l.UserName))
                .GroupBy(l => l.UserName!)
                .OrderByDescending(g => g.Count())
                .Take(topN)
                .ToDictionary(g => g.Key, g => (long)g.Count());
        }
        
        return Task.FromResult(stats);
    }

    public Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24, string? tenantId = null)
    {
        var result = new Dictionary<DateTime, long>();
        lock (_logs)
        {
            var logsInRange = _logs.Where(l => l.OperationTime >= startTime && l.OperationTime <= endTime);
            if (tenantId != null)
            {
                logsInRange = logsInRange.Where(l => l.TenantId == tenantId);
            }
            
            // 按时间分组统计
            var intervalInHours = interval;
            result = logsInRange
                .GroupBy(l => new DateTime(l.OperationTime.Year, l.OperationTime.Month, l.OperationTime.Day, 
                                          (l.OperationTime.Hour / intervalInHours) * intervalInHours, 0, 0))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => (long)g.Count());
        }
        
        return Task.FromResult(result);
    }

    public Task<(IEnumerable<AuditLog> Items, long Total)> SearchAsync(AuditLogQueryDto query)
    {
        IEnumerable<AuditLog> filteredItems;
        
        lock (_logs)
        {
            var queryable = _logs.AsQueryable();
            
            // 应用过滤条件
            if (!string.IsNullOrEmpty(query.UserId))
                queryable = queryable.Where(l => l.UserId == query.UserId);
                
            if (!string.IsNullOrEmpty(query.UserName))
                queryable = queryable.Where(l => l.UserName != null && l.UserName.Contains(query.UserName));
                
            if (!string.IsNullOrEmpty(query.IpAddress))
                queryable = queryable.Where(l => l.IpAddress == query.IpAddress);
                
            if (query.StartTime.HasValue)
                queryable = queryable.Where(l => l.OperationTime >= query.StartTime.Value);
                
            if (query.EndTime.HasValue)
                queryable = queryable.Where(l => l.OperationTime <= query.EndTime.Value);
                            
            if (query.IsSuccess.HasValue)
                queryable = queryable.Where(l => l.IsSuccess == query.IsSuccess.Value);
            
            // 计算总数
            var totalCount = queryable.Count();
            
            // 应用排序
            if (!string.IsNullOrEmpty(query.OrderBy))
            {
                // 根据字段进行排序
                queryable = query.OrderDir?.ToLower() == "asc" 
                    ? queryable.OrderBy(l => GetPropertyValue(l, query.OrderBy))
                    : queryable.OrderByDescending(l => GetPropertyValue(l, query.OrderBy));
            }
            else
            {
                // 默认按操作时间降序排序
                queryable = queryable.OrderByDescending(l => l.OperationTime);
            }
            
            // 分页
            filteredItems = queryable
                .Skip((query.Page - 1) * query.PerPage)
                .Take(query.PerPage)
                .ToList();
                
            return Task.FromResult((filteredItems, (long)totalCount));
        }
    }
    
    private static object? GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    public void ClearLogs()
    {
        lock (_logs)
        {
            _logs.Clear();
        }
    }

    public List<AuditLog> GetAuditLogs()
    {
        lock (_logs)
        {
            return _logs.ToList();
        }
    }
}