# CodeSpirit.Audit

## 概述

CodeSpirit.Audit是一个全面的审计组件，提供操作日志记录、消息队列集成和Elasticsearch存储与分析功能。该组件可以无缝集成到ASP.NET Core应用程序中，记录API请求和用户操作，并提供强大的查询和分析功能。

## 功能特点

- 自动捕获用户操作和API请求
- 支持通过自定义特性标记需要审计的操作
- 提取控制器和方法上的特性信息以增强日志内容
- 将审计日志推送到RabbitMQ消息队列
- 使用Elasticsearch存储和索引审计日志
- 提供丰富的查询和分析功能
- 支持基于时间、用户、操作类型等多种维度的统计和趋势分析

## 安装

1. 将项目添加到解决方案中
2. 在需要使用审计组件的项目中添加引用：

```xml
<ProjectReference Include="..\Components\CodeSpirit.Audit\CodeSpirit.Audit.csproj" />
```

## 配置

在`appsettings.json`中添加以下配置：

```json
{
  "Audit": {
    "Enabled": true,
    "LogRequestParams": true,
    "LogResponseData": false,
    "LogUnauthorizedRequests": true,
    "LogAnonymousRequests": false,
    "LogHealthChecks": false,
    "ExcludedPathPrefixes": [
      "/swagger",
      "/healthz",
      "/favicon.ico"
    ],
    "RabbitMQ": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/",
      "ExchangeName": "audit.exchange",
      "QueueName": "audit.queue",
      "RoutingKey": "audit.log"
    },
    "Elasticsearch": {
      "Urls": [
        "http://localhost:9200"
      ],
      "IndexName": "auditlogs",
      "IndexPrefix": "",
      "UserName": "",
      "Password": "",
      "NumberOfShards": 1,
      "NumberOfReplicas": 1
    }
  }
}
```

## 使用方法

### 1. 配置服务

在`Program.cs`或`Startup.cs`中注册服务：

```csharp
// 添加审计服务
builder.Services.AddAuditServices(builder.Configuration);

// 配置中间件管道
var app = builder.Build();

// 使用审计中间件（请在UseRouting之后，UseAuthorization之前配置）
app.UseRouting();
app.UseAudit();
app.UseAuthorization();
```

### 2. 标记需要审计的控制器或方法

使用审计特性标记需要详细记录的控制器或方法：

```csharp
// 在控制器级别添加，将记录所有方法
[Audit]
public class UsersController : ControllerBase
{
    // 方法级别的审计特性会覆盖控制器级别的设置
    [Audit("创建用户", AuditOperationType.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        // 业务逻辑
    }
    
    [Audit("更新用户信息", AuditOperationType.Update)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(long id, UpdateUserDto dto)
    {
        // 业务逻辑
    }
}
```

### 3. 查询审计日志

使用审计日志服务查询和分析审计记录：

```csharp
public class AuditLogController : ControllerBase
{
    private readonly IAuditService _auditService;
    
    public AuditLogController(IAuditService auditService)
    {
        _auditService = auditService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditService.SearchAsync(query);
        return Ok(result);
    }
}
```

## 高级查询

组件提供了`AuditQueryHelper`类，用于构建复杂的Elasticsearch查询：

```csharp
// 创建基于时间范围的查询
var timeRangeQuery = AuditQueryHelper.CreateTimeRangeQuery(startTime, endTime);

// 创建基于用户的查询
var userQuery = AuditQueryHelper.CreateUserQuery(userId);

// 创建复杂聚合查询
var complexQuery = AuditQueryHelper.CreateComplexAggregation(startTime, endTime);
```

## 多环境索引配置

### 索引前缀配置

为了支持在同一个Elasticsearch集群中为不同环境（开发、测试、生产等）存储审计日志，组件支持配置索引前缀：

```json
{
  "Audit": {
    "Elasticsearch": {
      "IndexName": "auditlogs",
      "IndexPrefix": "dev"
    }
  }
}
```

配置说明：
- `IndexPrefix`：索引前缀，用于区分不同环境（可选）
- 如果设置了前缀，最终的索引名称格式为：`{IndexPrefix}_{IndexName}`
- 如果不设置前缀（空字符串），直接使用`IndexName`

### 环境配置示例

#### 开发环境 (appsettings.Development.json)
```json
{
  "Audit": {
    "Elasticsearch": {
      "IndexPrefix": "dev",
      "IndexName": "auditlogs"
    }
  }
}
```
最终索引名称：`dev_auditlogs`

#### 测试环境 (appsettings.Testing.json)
```json
{
  "Audit": {
    "Elasticsearch": {
      "IndexPrefix": "test",
      "IndexName": "auditlogs"
    }
  }
}
```
最终索引名称：`test_auditlogs`

#### 生产环境 (appsettings.Production.json)
```json
{
  "Audit": {
    "Elasticsearch": {
      "IndexPrefix": "prod",
      "IndexName": "auditlogs"
    }
  }
}
```
最终索引名称：`prod_auditlogs`

这样配置后，不同环境的审计日志将存储在不同的索引中，避免了数据混乱，同时可以共享同一个Elasticsearch集群。

## 架构设计

组件由以下主要部分组成：

1. **模型** - 定义审计日志结构和配置选项
2. **特性** - 用于标记需要审计的控制器和方法
3. **中间件** - 捕获HTTP请求并生成审计日志
4. **服务** - 提供日志记录、查询和分析功能
5. **消息队列集成** - 使用RabbitMQ处理异步日志记录
6. **Elasticsearch集成** - 提供日志存储和高级查询功能

## 常见问题解决

### 1. Operation属性无法访问

如果在使用`OperationAttribute`时出现属性访问错误，请确保使用`OperationAttributeHelper`类来提取属性值，该类使用反射获取所有公共属性。

### 2. 中间件获取控制器信息失败

如果中间件无法正确获取控制器和操作信息，可能是因为ASP.NET Core路由系统的变化。请确保使用最新的`FindControllerType`和`FindActionMethod`方法来获取控制器类型和方法信息。

### 3. Elasticsearch连接问题

如果出现Elasticsearch连接问题，请检查：
- Elasticsearch服务是否正在运行
- 配置中的URLs是否正确
- 如果使用了身份验证，用户名和密码是否正确

### 4. RabbitMQ消息发送失败

如果RabbitMQ消息发送失败，请确保：
- RabbitMQ服务器正在运行
- 交换机和队列已正确声明
- 连接参数（主机、端口、用户名、密码）正确

## 依赖项

- .NET 9.0
- Microsoft.AspNetCore.App
- NEST 7.17.5 (Elasticsearch客户端)
- RabbitMQ.Client 6.7.0
- Microsoft.Extensions.Hosting 