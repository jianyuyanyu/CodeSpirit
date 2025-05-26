# CodeSpirit.Audit 审计组件集成使用指南

## 📋 目录

1. [概述](#概述)
2. [快速开始](#快速开始)
3. [详细配置](#详细配置)
4. [集成步骤](#集成步骤)
5. [使用示例](#使用示例)
6. [高级功能](#高级功能)
7. [性能优化](#性能优化)
8. [监控与运维](#监控与运维)
9. [故障排除](#故障排除)
10. [最佳实践](#最佳实践)

## 概述

CodeSpirit.Audit 是一个功能强大的审计组件，提供全面的API操作记录、敏感数据脱敏、实时查询分析等功能。本文档将指导您完成组件的集成和使用。

### 核心特性

- 🔍 **自动审计记录** - 无侵入式捕获所有API请求
- 🛡️ **敏感数据保护** - 智能脱敏和数据安全
- 📊 **实时分析** - 丰富的查询和统计功能
- 🚀 **高性能** - 异步处理和性能优化
- 🔧 **易于集成** - 简单配置即可使用

## 快速开始

### 1. 添加项目引用

在您的Web项目中添加审计组件引用：

```xml
<ProjectReference Include="..\Components\CodeSpirit.Audit\CodeSpirit.Audit.csproj" />
```

### 2. 基础配置

在 `appsettings.json` 中添加基础配置：

```json
{
  "Audit": {
    "Enabled": true,
    "LogRequestParams": true,
    "LogResponseData": false,
    "RabbitMQ": {
      "ExchangeName": "audit.exchange",
      "QueueName": "audit.queue",
      "RoutingKey": "audit.log"
    },
    "Elasticsearch": {
      "Urls": ["http://localhost:9200"],
      "IndexName": "auditlogs",
      "IndexPrefix": "dev"
    }
  }
}
```

### 3. 服务注册

在 `Program.cs` 中注册服务：

```csharp
// 添加审计服务
builder.Services.AddAuditServices(builder.Configuration);

var app = builder.Build();

// 使用审计中间件
app.UseRouting();
app.UseAudit();
app.UseAuthorization();
```

### 4. 标记审计操作

在控制器或方法上添加审计特性：

```csharp
[Audit("用户管理", AuditOperationType.Action)]
public class UsersController : ControllerBase
{
    [Audit("创建用户", AuditOperationType.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        // 业务逻辑
        return Ok();
    }
}
```

## 详细配置

### 完整配置示例

```json
{
  "Audit": {
    "Enabled": true,
    "LogRequestParams": true,
    "LogResponseData": false,
    "LogUnauthorizedRequests": true,
    "LogAnonymousRequests": false,
    "LogHealthChecks": false,
    "EnableOperationTypeInference": true,
    "EnableGeoLocation": false,
    "GeoLocationApiUrl": "http://ip-api.com/json/{0}?fields=status,message,country,countryCode,region,regionName,city,lat,lon,isp,org",
    "GeoLocationApiType": "ipapi",
    "ExcludedPathPrefixes": [
      "/swagger",
      "/healthz", 
      "/favicon.ico"
    ],
    "SensitiveData": {
      "Enabled": true,
      "SensitiveFieldPatterns": [
        "password", "pwd", "secret", "token", "apiKey", "key", "auth", "credential",
        "creditCard", "cardNumber", "cvv", "ssn", "idCard"
      ],
      "MaskCharacter": "*",
      "KeepFirstChars": 0,
      "KeepLastChars": 0,
      "ExcludedFields": [
        "password", "newPassword", "confirmPassword", "currentPassword"
      ]
    },
    "OperationInference": {
      "QueryKeywords": ["Get", "List", "Find", "Search", "Query"],
      "CreateKeywords": ["Create", "Add", "Insert", "Post"],
      "UpdateKeywords": ["Update", "Edit", "Modify", "Put", "Patch"],
      "DeleteKeywords": ["Delete", "Remove", "Clear"],
      "HttpMethodMappings": {
        "GET": "Query",
        "POST": "Create",
        "PUT": "Update", 
        "PATCH": "Update",
        "DELETE": "Delete"
      },
      "CommonIdParameterNames": ["id", "Id", "ID", "key", "Key"]
    },
    "RabbitMQ": {
      "ExchangeName": "audit.exchange",
      "QueueName": "audit.queue",
      "RoutingKey": "audit.log"
    },
    "Elasticsearch": {
      "Urls": ["http://localhost:9200"],
      "IndexName": "auditlogs",
      "IndexPrefix": "dev",
      "UserName": "",
      "Password": "",
      "NumberOfShards": 1,
      "NumberOfReplicas": 1
    }
  }
}
```

### 配置项说明

#### 基础配置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `Enabled` | bool | true | 是否启用审计功能 |
| `LogRequestParams` | bool | true | 是否记录请求参数 |
| `LogResponseData` | bool | false | 是否记录响应数据 |
| `LogUnauthorizedRequests` | bool | true | 是否记录未授权请求 |
| `LogAnonymousRequests` | bool | false | 是否记录匿名请求 |
| `LogHealthChecks` | bool | false | 是否记录健康检查请求 |

#### 高级配置

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `EnableOperationTypeInference` | bool | true | 启用操作类型自动推断 |
| `EnableGeoLocation` | bool | false | 启用地理位置查询 |
| `ExcludedPathPrefixes` | string[] | ["/swagger", "/healthz", "/favicon.ico"] | 排除的路径前缀 |

## 集成步骤

### 步骤1：环境准备

确保以下服务正常运行：

1. **RabbitMQ** - 消息队列服务
2. **Elasticsearch** - 日志存储和查询
3. **Redis** (可选) - 缓存服务

### 步骤2：Aspire配置

如果使用Aspire，在AppHost中配置：

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// 添加RabbitMQ
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// 添加Elasticsearch  
var elasticsearch = builder.AddElasticsearch("elasticsearch")
    .WithDataVolume();

// 配置Web项目
builder.AddProject<Projects.CodeSpirit_Web>("webfrontend")
    .WithReference(rabbitmq)
    .WithReference(elasticsearch);
```

### 步骤3：连接字符串配置

在Web项目的配置中添加连接字符串：

```json
{
  "ConnectionStrings": {
    "rabbitmq": "amqp://admin:Password123@rabbitmq:5672",
    "elasticsearch": "http://elastic:Password123@elasticsearch:9200"
  },
  "Aspire": {
    "RabbitMQ": {
      "Client": {
        "ConnectionString": "amqp://admin:Password123@rabbitmq:5672"
      }
    },
    "Elastic": {
      "Clients": {
        "Elasticsearch": {
          "Endpoint": "http://elastic:Password123@elasticsearch:9200"
        }
      }
    }
  }
}
```

### 步骤4：服务注册和中间件配置

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加Aspire服务默认配置
builder.AddServiceDefaults();

// 添加审计服务
builder.Services.AddAuditServices(builder.Configuration);

// 可选：添加性能监控
builder.Services.AddAuditPerformanceMonitoring();

var app = builder.Build();

// 配置中间件管道
app.UseRouting();

// 可选：性能监控中间件
app.UseAuditPerformanceMonitoring();

// 审计中间件（必须在UseRouting之后，UseAuthorization之前）
app.UseAudit();

app.UseAuthorization();
app.MapControllers();

app.Run();
```

## 使用示例

### 基础审计标记

```csharp
// 控制器级别审计
[Audit("用户管理")]
public class UsersController : ControllerBase
{
    // 方法级别审计（会覆盖控制器级别）
    [Audit("创建用户", AuditOperationType.Create, EntityName = "User")]
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        // 业务逻辑
        var user = await _userService.CreateAsync(dto);
        return Ok(user);
    }

    [Audit("更新用户", AuditOperationType.Update, EntityName = "User")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(long id, UpdateUserDto dto)
    {
        // 业务逻辑
        await _userService.UpdateAsync(id, dto);
        return Ok();
    }

    [Audit("删除用户", AuditOperationType.Delete, EntityName = "User")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(long id)
    {
        // 业务逻辑
        await _userService.DeleteAsync(id);
        return Ok();
    }
}
```

### 自动操作类型推断

启用自动推断后，可以简化审计标记：

```csharp
public class ProductsController : ControllerBase
{
    // 自动推断为 Query 类型（基于GET方法和方法名）
    [Audit("获取产品列表")]
    [HttpGet]
    public async Task<IActionResult> GetProducts() { }

    // 自动推断为 Create 类型（基于POST方法和方法名）
    [Audit("创建产品")]
    [HttpPost]
    public async Task<IActionResult> CreateProduct(ProductDto dto) { }

    // 自动推断为 Update 类型（基于PUT方法和方法名）
    [Audit("更新产品")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(long id, ProductDto dto) { }
}
```

### 审计日志查询

创建查询控制器：

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// 查询审计日志
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditService.SearchAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// 获取操作统计
    /// </summary>
    [HttpGet("stats/operations")]
    public async Task<IActionResult> GetOperationStats(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        var stats = await _auditService.GetOperationStatsAsync(startTime, endTime);
        return Ok(stats);
    }

    /// <summary>
    /// 获取用户活动统计
    /// </summary>
    [HttpGet("stats/users")]
    public async Task<IActionResult> GetUserStats(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] int topN = 10)
    {
        var stats = await _auditService.GetUserStatsAsync(startTime, endTime, topN);
        return Ok(stats);
    }

    /// <summary>
    /// 获取操作趋势
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetOperationTrend(
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] int interval = 24)
    {
        var trend = await _auditService.GetOperationTrendAsync(startTime, endTime, interval);
        return Ok(trend);
    }
}
```

## 高级功能

### 敏感数据脱敏

#### 配置敏感字段

```json
{
  "Audit": {
    "SensitiveData": {
      "Enabled": true,
      "SensitiveFieldPatterns": [
        "password", "pwd", "secret", "token", "apiKey", "key", "auth",
        "credential", "creditCard", "cardNumber", "cvv", "ssn", "idCard"
      ],
      "MaskCharacter": "*",
      "KeepFirstChars": 2,
      "KeepLastChars": 2,
      "ExcludedFields": [
        "password", "newPassword", "confirmPassword", "currentPassword"
      ]
    }
  }
}
```

#### 脱敏效果示例

```json
// 原始数据
{
  "username": "john.doe",
  "password": "secret123",
  "apiKey": "ak_1234567890abcdef",
  "email": "john@example.com"
}

// 脱敏后
{
  "username": "john.doe",
  "password": "[已移除]",
  "apiKey": "ak**************ef",
  "email": "john@example.com"
}
```

### 地理位置查询

启用地理位置功能：

```json
{
  "Audit": {
    "EnableGeoLocation": true,
    "GeoLocationApiUrl": "http://ip-api.com/json/{0}?fields=status,message,country,countryCode,region,regionName,city,lat,lon,isp,org",
    "GeoLocationApiType": "ipapi"
  }
}
```

### 自定义查询

使用查询助手构建复杂查询：

```csharp
public class CustomAuditService
{
    private readonly IAuditService _auditService;

    public CustomAuditService(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IEnumerable<AuditLog>> GetFailedOperations(DateTime startTime, DateTime endTime)
    {
        // 构建失败操作查询
        var query = AuditQueryHelper.CombineQueries(
            AuditQueryHelper.CreateTimeRangeQuery(startTime, endTime),
            AuditQueryHelper.CreateFailedOperationsQuery(),
            AuditQueryHelper.CreateSortQuery("OperationTime", false)
        );

        var result = await _auditService.SearchAsync(new AuditLogQueryDto
        {
            StartTime = startTime,
            EndTime = endTime,
            IsSuccess = false,
            PageSize = 100
        });

        return result.Items;
    }

    public async Task<Dictionary<string, long>> GetOperationsByUser(string userId, DateTime startTime, DateTime endTime)
    {
        var result = await _auditService.SearchAsync(new AuditLogQueryDto
        {
            UserId = userId,
            StartTime = startTime,
            EndTime = endTime,
            PageSize = 1000
        });

        return result.Items
            .GroupBy(x => x.OperationType ?? "Unknown")
            .ToDictionary(g => g.Key, g => (long)g.Count());
    }
}
```

## 性能优化

### 启用性能监控

```csharp
// 注册性能监控服务
builder.Services.AddAuditPerformanceMonitoring();

// 使用性能监控中间件
app.UseAuditPerformanceMonitoring();
```

### 性能监控API

```csharp
[ApiController]
[Route("api/[controller]")]
public class PerformanceController : ControllerBase
{
    private readonly AuditPerformanceCounters _counters;

    public PerformanceController(AuditPerformanceCounters counters)
    {
        _counters = counters;
    }

    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        return Ok(new
        {
            TotalRequests = _counters.TotalRequests,
            AuditedRequests = _counters.AuditedRequests,
            SuccessfulAudits = _counters.SuccessfulAudits,
            FailedAudits = _counters.FailedAudits,
            AverageProcessingTime = _counters.AverageProcessingTime,
            SuccessRate = _counters.SuccessRate
        });
    }
}
```

### 性能优化建议

1. **合理配置日志级别**
   ```json
   {
     "Audit": {
       "LogResponseData": false,  // 生产环境建议关闭
       "LogAnonymousRequests": false
     }
   }
   ```

2. **优化Elasticsearch配置**
   ```json
   {
     "Audit": {
       "Elasticsearch": {
         "NumberOfShards": 3,
         "NumberOfReplicas": 1
       }
     }
   }
   ```

3. **使用索引前缀区分环境**
   ```json
   {
     "Audit": {
       "Elasticsearch": {
         "IndexPrefix": "prod"  // 生产环境
       }
     }
   }
   ```

## 监控与运维

### 健康检查

添加审计组件健康检查：

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<AuditHealthCheck>("audit");

app.MapHealthChecks("/health");
```

### 日志监控

监控关键指标：

```csharp
public class AuditMonitoringService : BackgroundService
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditMonitoringService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddMinutes(-5);

                // 检查最近5分钟的审计记录
                var result = await _auditService.SearchAsync(new AuditLogQueryDto
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    PageSize = 1
                });

                if (result.Total == 0)
                {
                    _logger.LogWarning("最近5分钟没有审计记录");
                }

                // 检查失败率
                var failedResult = await _auditService.SearchAsync(new AuditLogQueryDto
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    IsSuccess = false,
                    PageSize = 100
                });

                var failureRate = result.Total > 0 ? (double)failedResult.Total / result.Total : 0;
                if (failureRate > 0.1) // 失败率超过10%
                {
                    _logger.LogError("审计失败率过高: {FailureRate:P}", failureRate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审计监控检查失败");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### 告警配置

配置关键指标告警：

1. **Elasticsearch索引大小**
2. **RabbitMQ队列深度**
3. **审计记录失败率**
4. **响应时间异常**

## 故障排除

### 常见问题

#### 1. 审计记录不生效

**症状**：API请求没有生成审计记录

**排查步骤**：
1. 检查 `Audit.Enabled` 配置
2. 确认中间件注册顺序
3. 检查路径是否在排除列表中
4. 验证审计特性是否正确添加

```csharp
// 检查中间件顺序
app.UseRouting();        // 必须在前
app.UseAudit();          // 审计中间件
app.UseAuthorization();  // 必须在后
```

#### 2. Elasticsearch连接失败

**症状**：日志中出现Elasticsearch连接错误

**解决方案**：
1. 检查Elasticsearch服务状态
2. 验证连接字符串格式
3. 确认网络连通性
4. 检查认证信息

```bash
# 测试Elasticsearch连接
curl -X GET "localhost:9200/_cluster/health"
```

#### 3. RabbitMQ消息积压

**症状**：队列中消息数量持续增长

**解决方案**：
1. 检查消费者服务状态
2. 增加消费者实例数量
3. 优化消息处理逻辑
4. 检查Elasticsearch性能

#### 4. 敏感数据脱敏不生效

**症状**：敏感数据没有被正确脱敏

**解决方案**：
1. 检查 `SensitiveData.Enabled` 配置
2. 验证字段名称匹配规则
3. 确认字段不在排除列表中
4. 检查JSON结构是否正确

### 调试技巧

#### 启用详细日志

```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.Audit": "Debug"
    }
  }
}
```

#### 使用测试端点

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuditTestController : ControllerBase
{
    [Audit("测试审计")]
    [HttpPost("test")]
    public IActionResult TestAudit([FromBody] object data)
    {
        return Ok(new { message = "测试成功", timestamp = DateTime.UtcNow });
    }
}
```

## 最佳实践

### 1. 审计策略

- **选择性审计**：只对关键操作添加审计特性
- **合理配置**：根据业务需求配置日志级别
- **性能考虑**：避免记录大量响应数据

### 2. 安全考虑

- **敏感数据保护**：配置完整的敏感字段列表
- **访问控制**：限制审计日志的查询权限
- **数据保留**：制定合理的数据保留策略

### 3. 运维建议

- **监控告警**：设置关键指标监控
- **定期清理**：清理过期的审计数据
- **备份策略**：制定审计数据备份计划

### 4. 开发规范

```csharp
// 推荐的审计特性使用方式
[Audit("用户登录", AuditOperationType.Action, EntityName = "User")]
[HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    // 业务逻辑
}

// 避免在高频接口上使用详细审计
[HttpGet("heartbeat")]
public IActionResult Heartbeat()
{
    // 不添加审计特性
    return Ok();
}
```

### 5. 配置管理

```json
{
  "Audit": {
    "Enabled": true,
    "LogRequestParams": true,
    "LogResponseData": false,  // 生产环境建议关闭
    "ExcludedPathPrefixes": [
      "/health",
      "/metrics", 
      "/swagger",
      "/favicon.ico",
      "/api/heartbeat"  // 排除心跳检查
    ]
  }
}
```

## 总结

CodeSpirit.Audit 审计组件提供了完整的API审计解决方案，通过合理的配置和使用，可以帮助您：

- 📝 **完整记录**：捕获所有关键操作
- 🔒 **数据安全**：保护敏感信息
- 📊 **实时分析**：提供丰富的查询和统计
- 🚀 **高性能**：异步处理，不影响业务性能
- 🛠️ **易维护**：简单配置，自动化运维

遵循本指南的建议，您可以快速集成并充分利用审计组件的强大功能。 