# RabbitMQ 审计日志消费者故障排除指南

## 概述

本指南包含RabbitMQ审计日志消费者的完整故障排除流程，涵盖从基础配置到复杂问题的诊断和解决方案。

## 目录

1. [基础诊断步骤](#1-基础诊断步骤)
2. [配置检查](#2-配置检查)
3. [RabbitMQ实体验证](#3-rabbitmq实体验证)
4. [消息路由验证](#4-消息路由验证)
5. [常见问题及解决方案](#5-常见问题及解决方案)
6. [关键故障案例](#6-关键故障案例)
7. [调试技巧](#7-调试技巧)
8. [监控和预防](#8-监控和预防)
9. [命令参考](#9-命令参考)

## 1. 基础诊断步骤

### 1.1 检查服务启动状态

确保以下服务已正确启动：
- RabbitMQ 服务容器
- 审计日志消费者后台服务 (AuditLogConsumerService)

查看日志中是否包含：
```
审计日志消费者服务正在启动...
Elasticsearch索引检查完成
审计RabbitMQ消费者已创建，队列: audit.queue, 消费者标签: {ConsumerTag}
审计日志消费者已启动，消费者标签: {ConsumerTag}
```

### 1.2 检查容器状态

```bash
# 检查所有相关容器状态
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# 检查RabbitMQ容器
docker ps --filter "name=rabbitmq" --format "table {{.Names}}\t{{.Status}}"

# 检查Elasticsearch容器
docker ps --filter "name=elasticsearch" --format "table {{.Names}}\t{{.Status}}"
```

## 2. 配置检查

### 2.1 检查键控客户端注册

确保在 `CodeSpirit.ServiceDefaults/Extensions.cs` 中已启用审计服务专用客户端：

```csharp
// 审计服务专用客户端
builder.AddKeyedRabbitMQClient("audit", settings =>
{
    settings.DisableHealthChecks = true;
    settings.DisableTracing = true; // 审计不需要跟踪以避免循环
});
```

### 2.2 检查连接字符串

在 `appsettings.json` 中确保有正确的连接字符串：

```json
{
  "ConnectionStrings": {
    "audit": "amqp://admin:Password123@rabbitmq:5672"
  }
}
```

### 2.3 检查审计配置

在 `appsettings.json` 中确保有完整的审计配置：

```json
{
  "Audit": {
    "IsEnabled": true,
    "RabbitMQ": {
      "ExchangeName": "audit.exchange",
      "QueueName": "audit.queue", 
      "RoutingKey": "audit.log"
    },
    "Elasticsearch": {
      "Urls": ["http://localhost:51809"],
      "IndexName": "auditlogs",
      "UserName": "elastic",
      "Password": "Password123"
    }
  }
}
```

## 3. RabbitMQ实体验证

### 3.1 通过管理界面检查

访问 RabbitMQ 管理界面：`http://localhost:15672` (用户名: admin, 密码: Password123)

#### 检查Exchanges
1. 点击 "Exchanges" 选项卡
2. 查找 `audit.exchange`
3. 确认：
   - Type: `topic`
   - Durability: `Durable`
   - Auto delete: `No`

#### 检查Queues  
1. 点击 "Queues" 选项卡
2. 查找 `audit.queue`
3. 确认：
   - Durability: `Durable` 
   - Messages: 查看是否有消息积压
   - Consumers: 应该显示 `1` (有消费者连接)

#### 检查Bindings
1. 点击 `audit.exchange` 进入详情
2. 查看 "Bindings" 部分
3. 确认绑定信息：
   - To: `audit.queue` 
   - Routing key: `audit.log`
   - Arguments: 空

### 3.2 通过命令行检查

```bash
# 检查队列状态
docker exec <rabbitmq-container-name> rabbitmqctl list_queues name messages consumers durable auto_delete

# 检查交换机状态
docker exec <rabbitmq-container-name> rabbitmqctl list_exchanges name type

# 检查绑定关系
docker exec <rabbitmq-container-name> rabbitmqctl list_bindings source_name destination_name routing_key

# 检查消费者连接
docker exec <rabbitmq-container-name> rabbitmqctl list_consumers
```

**正常输出示例**：
```
# 队列状态
audit.queue        0    1       true    false

# 交换机状态  
audit.exchange     topic

# 绑定关系
audit.exchange    audit.queue    audit.log    []
```

## 4. 消息路由验证

### 4.1 检查消息发送

查看发送端日志：
```
开始记录审计日志: {Id}
准备发送消息，交换机: audit.exchange, 路由键: audit.log, 消息大小: {Size} bytes
审计消息已发送到RabbitMQ: 交换机=audit.exchange, 路由键=audit.log, 消息ID={MessageId}
审计日志已推送到消息队列: {Id}
```

### 4.2 手动测试消息发送

在 RabbitMQ 管理界面手动发送测试消息：

1. 进入 `audit.exchange` 详情页
2. 展开 "Publish message" 部分
3. 设置：
   - Routing key: `audit.log`
   - Delivery mode: `2 - Persistent`
   - Headers: 
     ```
     content-type: application/json
     content-encoding: utf-8
     ```
   - Payload: 
     ```json
     {
       "id": "test-12345",
       "operationTime": "2024-01-01T12:00:00Z",
       "userId": "test-user",
       "userName": "测试用户",
       "ipAddress": "127.0.0.1",
       "requestMethod": "GET",
       "requestPath": "/test",
       "operationType": "Read",
       "operationName": "测试操作"
     }
     ```
4. 点击 "Publish message"

### 4.3 检查消息处理

查看消费端日志（正常情况下应该看到）：
```
=== 收到RabbitMQ消息 ===
DeliveryTag: {DeliveryTag}
Exchange: audit.exchange  
RoutingKey: audit.log
消息大小: {Size} bytes
=== 开始处理审计日志消息 === ID: {Id}
=== 审计日志处理完成 === ID: {Id}
=== 消息处理完成 === DeliveryTag: {DeliveryTag}
```

## 5. 常见问题及解决方案

### 5.1 无法获取审计连接
**错误**: `未找到键为 audit 的RabbitMQ连接`

**解决方案**: 
- 检查 `Extensions.cs` 中是否已启用审计客户端注册
- 重启应用程序

### 5.2 队列未创建
**错误**: 队列不存在或无法访问

**解决方案**:
- 检查RabbitMQ服务是否运行
- 验证连接字符串和认证信息
- 手动创建队列和交换机进行测试

### 5.3 消息未路由到队列
**现象**: 发送成功但队列中没有消息

**解决方案**:
- 检查交换机类型是否为 `topic`
- 验证队列绑定的路由键是否正确
- 确保发送时使用正确的路由键

### 5.4 消息在队列中大量积压
**现象**: 
- Queues 页面显示消息数量持续增长
- 消费者已连接但不处理消息
- 日志显示消息发送成功但没有消费日志

**排查命令**:
```bash
# 查看队列详细状态
docker exec <rabbitmq-container> rabbitmqctl list_queues name messages consumers message_stats

# 查看消费者详细信息
docker exec <rabbitmq-container> rabbitmqctl list_consumers
```

**解决方案**: 
- 检查消费者应用程序的异常日志
- 验证依赖服务（Elasticsearch等）状态
- 增加异常处理和重试机制
- 参考第6节的关键故障案例

### 5.5 配置绑定问题
**现象**: 使用默认配置而不是自定义配置

**解决方案**:
- 确保配置文件路径正确
- 验证配置绑定代码中避免双重嵌套：
  ```csharp
  // 错误：双重嵌套
  configuration.GetSection("Audit").GetSection("Audit").Bind(options);
  
  // 正确：智能处理
  if (configuration.GetSection("Audit").Exists())
  {
      configuration.GetSection("Audit").Bind(options);
  }
  else
  {
      configuration.Bind(options);
  }
  ```
- 重启应用程序以重新加载配置

## 6. 关键故障案例

### 6.1 案例：AsyncEventingBasicConsumer事件处理失效

#### **问题描述**
- ✅ 消费者创建成功并连接到队列
- ✅ 消息成功发送到队列（数量增长）
- ✅ 队列状态正常（consumers = 1）
- ❌ 消息处理事件从未触发（没有 `=== 收到RabbitMQ消息 ===` 日志）
- ❌ 消息大量积压（200+ 条未处理）

#### **症状表现**
```
# 消费者创建日志正常
info: CodeSpirit.Audit.Services.Implementation.RabbitMQService[0]
      审计RabbitMQ消费者创建完成
      队列: audit.queue, 消费者标签: amq.ctag-xxx, 路由键: audit.log

# 消息发送日志正常  
info: CodeSpirit.Audit.Services.Implementation.RabbitMQService[0]
      审计消息已发送到RabbitMQ: 交换机=audit.exchange, 路由键=audit.log, 消息ID=xxx

# 但是从未出现消息接收日志：
# === 收到RabbitMQ消息 === (缺失)

# 队列状态显示消息积压
$ docker exec rabbitmq-xxx rabbitmqctl list_queues name messages consumers
audit.queue     211     1
```

#### **根本原因**
在 .NET 9 + RabbitMQ.Client 环境下，`AsyncEventingBasicConsumer` 的异步事件处理机制存在兼容性问题，导致 `Received` 事件处理器无法正确触发。

#### **解决方案**
将 `AsyncEventingBasicConsumer` 替换为同步的 `EventingBasicConsumer`：

**修复前（问题代码）**：
```csharp
// 创建异步消费者
var consumer = new AsyncEventingBasicConsumer(consumerChannel);

// 注册异步事件处理器
consumer.Received += async (sender, e) =>
{
    var body = e.Body.ToArray();
    var json = Encoding.UTF8.GetString(body);
    
    try
    {
        var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);
        if (message != null)
        {
            await handler(message);  // 异步调用
            consumerChannel.BasicAck(e.DeliveryTag, false);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "处理消息失败");
        consumerChannel.BasicNack(e.DeliveryTag, false, true);
    }
};
```

**修复后（正确代码）**：
```csharp
// 创建同步消费者
var consumer = new EventingBasicConsumer(consumerChannel);

// 注册同步事件处理器
consumer.Received += (sender, e) =>
{
    var body = e.Body.ToArray();
    var json = Encoding.UTF8.GetString(body);
    
    _logger.LogInformation("=== 收到RabbitMQ消息 ===");
    
    try
    {
        var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);
        if (message != null)
        {
            handler(message).Wait();  // 同步等待异步方法
            consumerChannel.BasicAck(e.DeliveryTag, false);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "处理消息失败");
        consumerChannel.BasicNack(e.DeliveryTag, false, true);
    }
};
```

#### **验证修复效果**
修复后的正常日志：
```
info: CodeSpirit.Audit.Services.Implementation.RabbitMQService[0]
      === 收到RabbitMQ消息 ===
info: CodeSpirit.Audit.Services.Implementation.RabbitMQService[0]
      DeliveryTag: 286
info: CodeSpirit.Audit.Services.Implementation.RabbitMQService[0]
      消息反序列化成功，开始处理...
info: CodeSpirit.Audit.Extensions.AuditLogConsumerService[0]
      === 开始处理审计日志消息 === ID: xxx
info: CodeSpirit.Audit.Extensions.AuditLogConsumerService[0]
      === 审计日志处理完成 === ID: xxx
info: CodeSpirit.Audit.Services.Implementation.RabbitMQService[0]
      === 消息处理完成 === DeliveryTag: 286
```

队列状态恢复正常：
```bash
$ docker exec rabbitmq-xxx rabbitmqctl list_queues name messages consumers
audit.queue     0       0    # 所有积压消息已处理完成
```

#### **预防措施**
1. 优先使用同步的 `EventingBasicConsumer`
2. 在代码注释中明确说明异步版本的潜在问题
3. 添加消息处理监控，及时发现事件处理失效问题
4. 在集成测试中验证消息的端到端处理

### 6.2 案例：配置双重嵌套导致服务初始化失败

#### **问题描述**
- ❌ 所有审计相关服务初始化失败
- ❌ RabbitMQ连接配置为空
- ❌ Elasticsearch配置为空  
- ❌ 消费者无法创建

#### **根本原因**
配置绑定时发生双重嵌套：
```csharp
// Program.cs 中传入 Audit 配置节
builder.Services.AddAuditServices(builder.Configuration.GetSection("Audit"));

// 服务内部又尝试获取 Audit 子节
configuration.GetSection("Audit").Bind(options); // 错误：在Audit节中找Audit子节
```

#### **解决方案**
实现智能配置绑定：
```csharp
public AuditService(IConfiguration configuration, ...)
{
    var options = new AuditOptions();
    if (configuration.GetSection("Audit").Exists())
    {
        // 传入的是完整配置，获取Audit节
        configuration.GetSection("Audit").Bind(options);
    }
    else
    {
        // 传入的就是Audit配置节
        configuration.Bind(options);
    }
    _options = options;
}
```

## 7. 调试技巧

### 7.1 启用详细日志

在 `appsettings.json` 中设置：

```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.Audit": "Debug",
      "CodeSpirit.Audit.Services.Implementation.RabbitMQService": "Information",
      "CodeSpirit.Audit.Extensions.AuditLogConsumerService": "Information"
    }
  }
}
```

### 7.2 手动测试RabbitMQ连接

创建简单的测试代码验证连接：

```csharp
public async Task TestRabbitMQConnection()
{
    var factory = serviceProvider.GetService<IRabbitMQServiceFactory>();
    var connection = factory.GetAuditConnection();
    
    if (connection.IsOpen)
    {
        Console.WriteLine("RabbitMQ连接正常");
    }
    else
    {
        Console.WriteLine("RabbitMQ连接失败");
    }
}
```

### 7.3 实时监控队列状态

```bash
# 持续监控队列状态
watch -n 5 'docker exec <rabbitmq-container> rabbitmqctl list_queues name messages consumers'

# 监控绑定关系
watch -n 10 'docker exec <rabbitmq-container> rabbitmqctl list_bindings'
```

### 7.4 验证消息序列化

确认发送的消息格式与消费者期望的格式一致：

```csharp
// 发送端：记录消息内容
_logger.LogDebug("消息JSON: {Json}", JsonSerializer.Serialize(message, _jsonOptions));

// 消费端：记录接收内容  
_logger.LogDebug("接收到JSON: {Json}", json);
```

## 8. 监控和预防

### 8.1 设置关键指标监控

建议监控以下指标：
- RabbitMQ连接状态
- 队列消息积压数量（告警阈值：> 100）
- 消费者处理速度（messages/second）
- 错误率和异常频率
- 依赖服务健康状态

### 8.2 健康检查配置

```csharp
// 添加RabbitMQ健康检查
builder.Services.AddHealthChecks()
    .AddRabbitMQ(connectionString: "amqp://admin:Password123@rabbitmq:5672")
    .AddElasticsearch(options =>
    {
        options.UseServer("http://localhost:51809");
        options.UseBasicAuthentication("elastic", "Password123");
    });
```

### 8.3 自动告警脚本

```bash
#!/bin/bash
# 检查队列积压并发送告警
QUEUE_DEPTH=$(docker exec rabbitmq-xxx rabbitmqctl list_queues name messages | grep audit.queue | awk '{print $2}')

if [ "$QUEUE_DEPTH" -gt 100 ]; then
    echo "警告：audit.queue 消息积压达到 $QUEUE_DEPTH 条" | mail -s "RabbitMQ告警" admin@company.com
fi
```

## 9. 命令参考

### 9.1 容器管理

```bash
# 检查容器状态
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# 重启相关服务
docker restart rabbitmq-xxx
docker restart elasticsearch-xxx

# 查看容器日志
docker logs rabbitmq-xxx --tail 100
docker logs elasticsearch-xxx --tail 100
```

### 9.2 RabbitMQ管理

```bash
# 基础状态检查
docker exec <rabbitmq-container> rabbitmqctl status
docker exec <rabbitmq-container> rabbitmqctl list_queues name messages consumers durable auto_delete
docker exec <rabbitmq-container> rabbitmqctl list_exchanges name type
docker exec <rabbitmq-container> rabbitmqctl list_bindings

# 连接和消费者检查
docker exec <rabbitmq-container> rabbitmqctl list_connections
docker exec <rabbitmq-container> rabbitmqctl list_consumers

# 队列管理（谨慎使用）
docker exec <rabbitmq-container> rabbitmqctl purge_queue audit.queue  # 清空队列
```

### 9.3 Elasticsearch检查

```bash
# 检查Elasticsearch状态
curl -X GET "localhost:51809/_cluster/health?pretty" -u elastic:Password123

# 检查审计索引
curl -X GET "localhost:51809/_cat/indices?v" -u elastic:Password123

# 查看最近的审计记录
curl -X GET "localhost:51809/auditlogs/_search?size=5&sort=operationTime:desc" -u elastic:Password123
```

### 9.4 紧急处理

```bash
# 停止所有应用程序进程
taskkill /f /im dotnet.exe

# 清空积压队列（紧急情况）
docker exec <rabbitmq-container> rabbitmqctl purge_queue audit.queue

# 重建队列和绑定
docker exec <rabbitmq-container> rabbitmqctl delete_queue audit.queue
# 重启应用程序让代码重新创建队列
```

## 10. 故障排除检查清单

### 10.1 基础检查

- [ ] RabbitMQ 容器正在运行
- [ ] Elasticsearch 容器正在运行  
- [ ] 应用程序成功启动
- [ ] 配置文件中连接字符串正确

### 10.2 RabbitMQ检查

- [ ] `audit.exchange` 交换机存在且类型为 `topic`
- [ ] `audit.queue` 队列存在且为持久化
- [ ] 队列正确绑定到交换机，路由键为 `audit.log`
- [ ] 消费者已连接（Consumers = 1）
- [ ] 队列中消息数量合理（无大量积压）

### 10.3 消息流检查

- [ ] 发送消息使用正确的交换机和路由键
- [ ] 消息格式符合预期的 JSON 结构
- [ ] 消费者能够接收消息（有 `=== 收到RabbitMQ消息 ===` 日志）
- [ ] 消息处理逻辑无异常
- [ ] 消息确认机制正常工作

### 10.4 配置检查

- [ ] 键控RabbitMQ客户端正确注册
- [ ] 审计配置绑定无双重嵌套问题
- [ ] 日志级别设置为 Debug 或 Information
- [ ] 依赖服务配置正确

### 10.5 代码检查

- [ ] 使用 `EventingBasicConsumer` 而非 `AsyncEventingBasicConsumer`
- [ ] 异常处理和重试机制完善
- [ ] 资源管理和连接清理正确
- [ ] 消息序列化/反序列化一致

完成以上检查后，应该能够确定并解决大部分RabbitMQ消费者相关问题。如问题仍未解决，请收集完整的错误日志、配置文件和RabbitMQ管理界面截图进行进一步分析。 