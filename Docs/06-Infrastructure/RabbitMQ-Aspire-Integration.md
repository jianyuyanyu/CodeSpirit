# RabbitMQ Aspire 集成重构文档

## 概述

本文档描述了CodeSpirit项目中RabbitMQ的重构，从直接使用RabbitMQ.Client迁移到基于.NET Aspire RabbitMQ集成的架构。

## 重构目标

1. **统一管理**: 使用Aspire.RabbitMQ.Client统一管理所有RabbitMQ连接
2. **资源隔离**: 为不同用途（事件总线、审计、消息传递）提供独立的连接
3. **配置简化**: 通过Aspire集成简化配置和连接管理
4. **可观测性**: 利用Aspire内置的健康检查、跟踪和指标功能
5. **向后兼容**: 保持现有API不变，逐步迁移（✅ 已完成）

## 架构设计

### 1. RabbitMQ服务工厂

```csharp
// 位置：CodeSpirit.ServiceDefaults.Messaging
IRabbitMQServiceFactory
├── GetEventBusConnection()    // 事件总线专用连接
├── GetAuditConnection()       // 审计系统专用连接
├── GetMessagingConnection()   // 通用消息专用连接
└── GetConnection(key)         // 按键获取连接
```

### 2. 键控客户端配置

在ServiceDefaults中配置多个键控RabbitMQ客户端：

```csharp
// 事件总线专用客户端
builder.AddKeyedRabbitMQClient("eventbus", settings =>
{
    settings.DisableHealthChecks = true;
    settings.DisableTracing = false; // 事件总线需要跟踪
});

// 审计服务专用客户端  
builder.AddKeyedRabbitMQClient("audit", settings =>
{
    settings.DisableHealthChecks = true;
    settings.DisableTracing = true; // 避免审计跟踪循环
});

// 通用消息服务专用客户端
builder.AddKeyedRabbitMQClient("messaging", settings =>
{
    settings.DisableHealthChecks = true;
    settings.DisableTracing = false;
});
```

### 3. 连接字符串配置

在appsettings.json中配置不同的连接字符串：

```json
{
  "ConnectionStrings": {
    "rabbitmq": "amqp://admin:Password123@rabbitmq:5672",
    "eventbus": "amqp://admin:Password123@rabbitmq:5672",
    "audit": "amqp://admin:Password123@rabbitmq:5672",
    "messaging": "amqp://admin:Password123@rabbitmq:5672"
  }
}
```

## 重构后的组件

### 1. 事件总线 (CodeSpirit.Shared.EventBus)

**重构后**:
```csharp
using CodeSpirit.ServiceDefaults.Messaging;

public RabbitMQEventBus(IRabbitMQServiceFactory factory, ...)
{
    var connection = factory.GetEventBusConnection();
    // ...
}
```

### 2. 审计服务 (CodeSpirit.Audit)

**重构后**:
```csharp
using CodeSpirit.ServiceDefaults.Messaging;

public RabbitMQService(
    ILogger logger,
    IConfiguration config, 
    IRabbitMQServiceFactory factory)
{
    var connection = factory.GetAuditConnection();
    // ...
}
```

### 3. 通用消息服务

可以使用`IRabbitMQServiceFactory.GetMessagingConnection()`获取专用连接。

## 配置说明

### 1. 基础配置

所有RabbitMQ客户端都使用相同的基础配置：
- 禁用健康检查（避免性能影响）
- 根据用途配置跟踪功能
- 使用连接字符串指定不同的连接参数

### 2. 不同用途的配置差异

| 用途 | 跟踪 | 健康检查 | 特点 |
|------|------|----------|------|
| 事件总线 | 启用 | 禁用 | 需要跟踪事件流转 |
| 审计系统 | 禁用 | 禁用 | 避免审计跟踪循环 |
| 通用消息 | 启用 | 禁用 | 需要跟踪消息传递 |

### 3. 扩展配置

如需添加新的RabbitMQ用途，在ServiceDefaults中添加：

```csharp
builder.AddKeyedRabbitMQClient("newpurpose", settings =>
{
    // 根据需要配置
});
```

并在IRabbitMQServiceFactory中添加对应方法。

## 使用指南

### 1. 注入依赖

```csharp
// 注入工厂
using CodeSpirit.ServiceDefaults.Messaging;

public class MyService
{
    private readonly IRabbitMQServiceFactory _rabbitMQFactory;
    
    public MyService(IRabbitMQServiceFactory rabbitMQFactory)
    {
        _rabbitMQFactory = rabbitMQFactory;
    }
    
    public void DoWork()
    {
        var connection = _rabbitMQFactory.GetMessagingConnection();
        // 使用连接
    }
}
```

### 2. 事件总线使用

事件总线的使用方式保持不变：

```csharp
// 发布事件
await _eventBus.PublishAsync(new MyEvent());

// 订阅事件  
_services.AddEventHandler<MyEvent, MyEventHandler>();
```

### 3. 审计服务使用

审计服务的使用方式保持不变：

```csharp
// 发送审计消息
await _auditService.SendAuditAsync(auditData);
```

## 迁移指南

### 1. 迁移完成状态 ✅

- ✅ 更新ServiceDefaults配置
- ✅ 更新依赖注入配置
- ✅ 替换组件构造函数
- ✅ 移除过时的构造函数
- ✅ 修复项目引用关系

### 2. 项目引用关系

- `CodeSpirit.ServiceDefaults` 包含 `IRabbitMQServiceFactory`
- `CodeSpirit.Shared` 引用 `ServiceDefaults`
- `CodeSpirit.Audit` 引用 `ServiceDefaults`
- 所有业务项目通过 `ServiceDefaults` 获得统一的RabbitMQ配置

### 3. 命名空间变更

- **旧**: `CodeSpirit.Shared.Messaging.IRabbitMQServiceFactory`
- **新**: `CodeSpirit.ServiceDefaults.Messaging.IRabbitMQServiceFactory`

## 优势

1. **资源隔离**: 不同用途使用独立连接，避免相互影响
2. **配置统一**: 通过Aspire集成统一管理配置
3. **可观测性**: 内置健康检查、跟踪和指标
4. **扩展性**: 易于添加新的RabbitMQ用途
5. **维护性**: 统一的工厂模式简化代码管理
6. **清洁架构**: ServiceDefaults层级分离，依赖关系清晰

## 注意事项

1. **连接复用**: 同一用途的多个服务实例会复用连接
2. **资源管理**: Aspire会自动管理连接生命周期
3. **配置优先级**: 连接字符串优先于Aspire配置节
4. **错误处理**: 工厂会处理连接获取失败的情况
5. **日志记录**: 所有操作都有详细的日志记录
6. **项目引用**: 需要引用ServiceDefaults项目以使用IRabbitMQServiceFactory

## 相关链接

- [.NET Aspire RabbitMQ集成文档](https://learn.microsoft.com/zh-cn/dotnet/aspire/messaging/rabbitmq-integration)
- [RabbitMQ .NET Client文档](https://www.rabbitmq.com/dotnet-api-guide.html)
- [.NET Aspire概述](https://learn.microsoft.com/zh-cn/dotnet/aspire/get-started/aspire-overview) 