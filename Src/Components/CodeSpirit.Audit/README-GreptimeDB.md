# CodeSpirit.Audit GreptimeDB集成指南

## 概述

CodeSpirit.Audit组件现在支持使用GreptimeDB作为审计日志的存储后端，提供更好的时序数据处理性能和资源利用率。

## 配置

### 1. 基本配置

在`appsettings.json`中配置GreptimeDB：

```json
{
  "Audit": {
    "Enabled": true,
    "StorageProvider": "GreptimeDB",
    "LogRequestParams": true,
    "LogResponseData": false,
    "GreptimeDB": {
      "Url": "http://localhost:4000",
      "Database": "audit_logs",
      "TableName": "audit_logs",
      "Username": "",
      "Password": "",
      "TimeoutSeconds": 30,
      "BatchSize": 1000,
      "TablePrefix": "codespirit"
    },
    "RabbitMQ": {
      "ExchangeName": "audit.exchange",
      "QueueName": "audit.queue",
      "RoutingKey": "audit.log"
    }
  }
}
```

### 2. 环境配置示例

#### 开发环境 (appsettings.Development.json)
```json
{
  "Audit": {
    "StorageProvider": "GreptimeDB",
    "GreptimeDB": {
      "Url": "http://localhost:4000",
      "Database": "audit_logs_dev",
      "TablePrefix": "dev"
    }
  }
}
```

#### 生产环境 (appsettings.Production.json)
```json
{
  "Audit": {
    "StorageProvider": "GreptimeDB",
    "GreptimeDB": {
      "Url": "https://greptimedb.production.com:4000",
      "Database": "audit_logs",
      "Username": "audit_user",
      "Password": "your_secure_password",
      "TablePrefix": "prod",
      "TimeoutSeconds": 60,
      "BatchSize": 5000
    }
  }
}
```

## 使用方法

### 1. 服务注册

在`Program.cs`中注册审计服务（无需修改）：

```csharp
// 添加审计服务（会自动根据配置选择存储提供者）
builder.Services.AddAuditServices(builder.Configuration);

var app = builder.Build();

// 使用审计中间件
app.UseRouting();
app.UseAudit();
app.UseAuthorization();
```

### 2. 控制器使用

控制器使用方式保持不变：

```csharp
[Audit]
public class UsersController : ControllerBase
{
    [Audit("创建用户", AuditOperationType.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        // 业务逻辑
    }
}
```

### 3. 查询审计日志

查询API保持不变：

```csharp
public class AuditLogController : ControllerBase
{
    private readonly IAuditService _auditService;
    
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditService.SearchAsync(query);
        return Ok(result);
    }
    
    [HttpGet("stats/operations")]
    public async Task<IActionResult> GetOperationStats(
        [FromQuery] DateTime startTime, 
        [FromQuery] DateTime endTime)
    {
        var stats = await _auditService.GetOperationStatsAsync(startTime, endTime);
        return Ok(stats);
    }
}
```

## GreptimeDB部署

### 1. Docker部署

```bash
# 使用Docker运行GreptimeDB
docker run -d \
  --name greptimedb \
  -p 4000:4000 \
  -p 4001:4001 \
  -p 4002:4002 \
  -p 4003:4003 \
  -v greptimedb-data:/tmp/greptimedb \
  greptime/greptimedb:latest standalone start
```

### 2. 验证连接

```sql
-- 连接到GreptimeDB并验证
curl -X POST "http://localhost:4000/v1/sql" \
  -H "Content-Type: application/json" \
  -d '{"sql": "SELECT 1 as health"}'
```

## 数据结构

GreptimeDB中的审计日志表结构：

```sql
CREATE TABLE IF NOT EXISTS codespirit_audit_logs (
    id STRING,
    user_id STRING,
    user_name STRING,
    ip_address STRING,
    operation_time TIMESTAMP TIME INDEX,
    service_name STRING,
    controller_name STRING,
    action_name STRING,
    operation_type STRING,
    description STRING,
    request_path STRING,
    request_method STRING,
    request_params TEXT,
    entity_name STRING,
    entity_id STRING,
    execution_duration BIGINT,
    is_success BOOLEAN,
    error_message TEXT,
    status_code INT,
    before_data TEXT,
    after_data TEXT,
    user_agent TEXT,
    operation_name STRING,
    tenant_id STRING,
    PRIMARY KEY (id)
)
```

## 查询示例

### 1. 基础查询

```sql
-- 查询最近的审计日志
SELECT * FROM codespirit_audit_logs 
ORDER BY operation_time DESC 
LIMIT 10;

-- 按用户查询
SELECT * FROM codespirit_audit_logs 
WHERE user_name = 'admin' 
AND operation_time >= '2024-01-01 00:00:00'
ORDER BY operation_time DESC;
```

### 2. 聚合统计

```sql
-- 操作类型统计
SELECT operation_type, COUNT(*) as count
FROM codespirit_audit_logs 
WHERE operation_time >= '2024-01-01 00:00:00'
GROUP BY operation_type
ORDER BY count DESC;

-- 用户活跃度统计
SELECT user_name, COUNT(*) as count
FROM codespirit_audit_logs 
WHERE operation_time >= '2024-01-01 00:00:00'
  AND user_name IS NOT NULL
GROUP BY user_name
ORDER BY count DESC
LIMIT 10;
```

### 3. 时间趋势分析

```sql
-- 按小时统计操作趋势
SELECT 
    date_trunc('hour', operation_time) as hour,
    COUNT(*) as operations
FROM codespirit_audit_logs 
WHERE operation_time >= '2024-01-01 00:00:00'
GROUP BY hour
ORDER BY hour;

-- 按天统计操作趋势
SELECT 
    date_trunc('day', operation_time) as day,
    COUNT(*) as operations
FROM codespirit_audit_logs 
WHERE operation_time >= '2024-01-01 00:00:00'
GROUP BY day
ORDER BY day;
```

## 性能优化

### 1. 批量写入

GreptimeDB支持高效的批量写入，配置较大的批次大小可以提高写入性能：

```json
{
  "GreptimeDB": {
    "BatchSize": 5000
  }
}
```

### 2. 索引优化

GreptimeDB自动为时间字段创建索引，对于频繁查询的字段可以考虑创建额外索引：

```sql
-- 为用户ID创建索引（如果需要）
ALTER TABLE codespirit_audit_logs ADD INDEX idx_user_id (user_id);
```

## 迁移指南

### 从Elasticsearch迁移到GreptimeDB

1. **备份现有数据**（如果需要）
2. **更新配置**：将`StorageProvider`改为`"GreptimeDB"`
3. **启动应用**：自动创建GreptimeDB表结构
4. **验证功能**：测试审计日志记录和查询

### 回滚到Elasticsearch

如需回滚，只需将配置改回：

```json
{
  "Audit": {
    "StorageProvider": "Elasticsearch"
  }
}
```

## 监控和维护

### 1. 健康检查

```csharp
// 检查GreptimeDB连接健康状态
public async Task<bool> CheckHealthAsync()
{
    var healthSql = "SELECT 1 as health";
    // 执行查询检查连接状态
}
```

### 2. 存储监控

定期监控GreptimeDB的：
- 存储空间使用
- 查询性能
- 连接数
- 内存使用

## 故障排除

### 常见问题

#### 1. ServiceUnavailable 错误

**错误信息**: `fail: CodeSpirit.Audit.Services.Implementation.GreptimeDbAuditStorageService[0] GreptimeDB SQL执行失败: ServiceUnavailable`

**可能原因及解决方案**:

- **GreptimeDB服务未启动**
  ```bash
  # 检查容器状态
  docker ps | grep greptimedb
  
  # 如果没有运行，启动服务
  cd Src/CodeSpirit.AppHost
  dotnet run
  ```

- **端口映射问题**
  - 确保端口4000已正确映射
  - 检查防火墙设置
  - 验证容器网络配置

- **配置错误**
  ```json
  // 检查 appsettings.json 中的配置
  {
    "Audit": {
      "GreptimeDB": {
        "Url": "http://greptimedb:4000",  // 容器内部使用服务名
        "Database": "audit_logs"
      }
    }
  }
  ```

#### 2. 表创建失败

**错误信息**: `fail: CodeSpirit.Audit.Services.Implementation.GreptimeDbAuditStorageService[0] GreptimeDB表创建失败: web_audit_logs`

**解决步骤**:

1. **检查数据库是否存在**
   ```bash
   # 使用 HTTP API 检查
   curl -X POST "http://localhost:4000/v1/sql" \
     -H "Content-Type: application/json" \
     -d '{"sql": "SHOW DATABASES"}'
   ```

2. **手动创建数据库**
   ```bash
   curl -X POST "http://localhost:4000/v1/sql" \
     -H "Content-Type: application/json" \
     -d '{"sql": "CREATE DATABASE IF NOT EXISTS audit_logs"}'
   ```

3. **检查表结构**
   ```bash
   curl -X POST "http://localhost:4000/v1/sql?db=audit_logs" \
     -H "Content-Type: application/json" \
     -d '{"sql": "SHOW TABLES"}'
   ```

#### 3. 连接超时

**解决方案**:
- 增加超时时间配置
- 检查网络延迟
- 验证DNS解析

```json
{
  "Audit": {
    "GreptimeDB": {
      "TimeoutSeconds": 60
    }
  }
}
```

### 诊断工具

#### 使用内置诊断工具

我们提供了专门的诊断工具来检查GreptimeDB连接状态：

```csharp
// 在代码中使用
var diagnosticTool = new GreptimeDbDiagnosticTool(logger, configuration);
var result = await diagnosticTool.RunFullDiagnosticAsync();

if (!result.AllTestsPassed)
{
    // 查看具体失败的测试
    logger.LogError("连接测试: {Success}", result.ConnectionTest.Success);
    logger.LogError("SQL测试: {Success}", result.SqlQueryTest.Success);
}
```

#### 手动检查步骤

1. **验证服务状态**
   ```bash
   # 检查 GreptimeDB 容器
   docker logs <greptimedb_container_id>
   
   # 检查端口监听
   netstat -an | grep 4000
   ```

2. **测试基础连接**
   ```bash
   # 健康检查
   curl http://localhost:4000/health
   
   # 基础查询
   curl -X POST "http://localhost:4000/v1/sql" \
     -H "Content-Type: application/json" \
     -d '{"sql": "SELECT 1"}'
   ```

3. **检查日志**
   - 应用程序日志
   - GreptimeDB 容器日志
   - Aspire 协调程序日志

### 性能优化建议

1. **批量写入**
   ```json
   {
     "GreptimeDB": {
       "BatchSize": 5000
     }
   }
   ```

2. **连接池配置**
   ```json
   {
     "GreptimeDB": {
       "TimeoutSeconds": 30
     }
   }
   ```

3. **重试机制**
   - 系统已内置指数退避重试
   - 最多重试3次
   - 重试间隔: 2^retry_count 秒

## 总结

GreptimeDB作为审计日志存储提供了：

- **更好的性能**：专为时序数据优化
- **更低的资源消耗**：内存使用更少
- **更简单的查询**：支持标准SQL
- **更好的压缩**：数据存储更高效

推荐在新项目中使用GreptimeDB，现有项目可以逐步迁移。
