# GreptimeDB 审计存储故障排除指南

## 快速修复

如果您遇到以下错误信息：

```
fail: CodeSpirit.Audit.Services.Implementation.GreptimeDbAuditStorageService[0] GreptimeDB SQL执行失败: ServiceUnavailable
fail: CodeSpirit.Audit.Services.Implementation.GreptimeDbAuditStorageService[0] GreptimeDB表创建失败: web_audit_logs
```

**立即执行以下步骤**:

### 步骤 1: 运行自动修复脚本

```powershell
# 进入项目根目录
cd D:\repos\code-spirit

# 运行修复脚本
.\Scripts\fix-greptimedb-connection.ps1 -Verbose
```

### 步骤 2: 确保 Aspire 应用正在运行

```bash
cd Src/CodeSpirit.AppHost
dotnet run
```

等待所有容器启动完成，特别是 GreptimeDB 容器。

### 步骤 3: 验证修复结果

检查应用程序日志，应该看到：
```
info: CodeSpirit.Audit.Services.Implementation.GreptimeDbAuditStorageService[0] GreptimeDB健康检查通过
info: CodeSpirit.Audit.Services.Implementation.GreptimeDbAuditStorageService[0] GreptimeDB表创建成功: web_audit_logs
```

## 详细问题分析

### 问题 1: ServiceUnavailable 错误

**根本原因**: GreptimeDB 服务不可用

**可能的具体原因**:
1. GreptimeDB 容器未启动
2. 端口映射配置错误
3. 网络连接问题
4. 容器启动顺序问题

**解决方案**:

#### 检查容器状态
```bash
docker ps | grep greptimedb
```

如果没有看到运行中的 GreptimeDB 容器：
```bash
# 启动 Aspire 协调程序
cd Src/CodeSpirit.AppHost
dotnet run
```

#### 检查端口访问
```bash
curl http://localhost:4000/health
```

期望响应: HTTP 200 状态码

#### 检查容器日志
```bash
docker logs $(docker ps -q --filter ancestor=greptime/greptimedb)
```

### 问题 2: 表创建失败

**根本原因**: 数据库或权限问题

**解决步骤**:

#### 手动创建数据库
```bash
curl -X POST "http://localhost:4000/v1/sql" \
  -H "Content-Type: application/json" \
  -d '{"sql": "CREATE DATABASE IF NOT EXISTS audit_logs"}'
```

#### 验证数据库存在
```bash
curl -X POST "http://localhost:4000/v1/sql" \
  -H "Content-Type: application/json" \
  -d '{"sql": "SHOW DATABASES"}'
```

#### 手动创建表
```bash
curl -X POST "http://localhost:4000/v1/sql?db=audit_logs" \
  -H "Content-Type: application/json" \
  -d '{
    "sql": "CREATE TABLE IF NOT EXISTS web_audit_logs (
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
    )"
  }'
```

## 自动初始化机制

### v1.1 更新（已修复数据库初始化问题）

从 v1.1 版本开始，GreptimeDB 审计存储服务已经实现了自动初始化机制：

1. **启动时初始化**：应用启动时会自动创建数据库和表
2. **运行时检测**：如果检测到数据库不存在错误，会自动重新初始化
3. **重试机制**：包含指数退避的重试逻辑
4. **错误隔离**：初始化失败不会影响应用启动

### 初始化流程

1. 检查基础连接（不指定数据库）
2. 执行 `SHOW DATABASES` 检查数据库是否存在
3. 如果不存在，执行 `CREATE DATABASE IF NOT EXISTS audit_logs`
4. 创建审计日志表（包含所有必要字段）
5. 进行健康检查确认初始化成功

### 日志输出示例

成功初始化时的日志：
```
info: GreptimeDB初始化服务开始
info: 检测到GreptimeDB审计存储服务，开始初始化  
info: 开始GreptimeDB连接检查，URL: http://localhost:4000
info: 开始创建GreptimeDB数据库: audit_logs
info: GreptimeDB数据库创建成功: audit_logs
info: 开始创建GreptimeDB表: web_audit_logs
info: GreptimeDB表创建成功: web_audit_logs
info: GreptimeDB初始化完成并通过健康检查
info: GreptimeDB健康检查通过，服务就绪
```

## 配置验证

### 检查 appsettings.json

确保配置正确：

```json
{
  "Audit": {
    "Enabled": true,
    "StorageProvider": "GreptimeDB",
    "GreptimeDB": {
      "Url": "http://greptimedb:4000",  // 容器内部使用服务名
      "Database": "audit_logs",
      "TableName": "audit_logs",
      "TablePrefix": "web",            // 根据服务调整
      "TimeoutSeconds": 30,
      "BatchSize": 1000
    }
  }
}
```

### 不同环境的配置

#### 开发环境 (本地测试)
```json
{
  "Audit": {
    "GreptimeDB": {
      "Url": "http://localhost:4000"  // 直接访问宿主机端口
    }
  }
}
```

#### 容器环境 (Aspire)
```json
{
  "Audit": {
    "GreptimeDB": {
      "Url": "http://greptimedb:4000"  // 使用容器服务名
    }
  }
}
```

## 性能优化

### 减少连接超时
```json
{
  "Audit": {
    "GreptimeDB": {
      "TimeoutSeconds": 60  // 增加超时时间
    }
  }
}
```

### 批量写入优化
```json
{
  "Audit": {
    "GreptimeDB": {
      "BatchSize": 5000  // 增加批次大小
    }
  }
}
```

## 监控和诊断

### 启用详细日志
在 `appsettings.Development.json` 中：

```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.Audit": "Debug",
      "CodeSpirit.Audit.Services.Implementation.GreptimeDbAuditStorageService": "Debug"
    }
  }
}
```

### 使用诊断工具

应用程序已内置诊断工具，会在连接失败时自动运行健康检查：

```csharp
// 检查日志中是否有类似输出
info: GreptimeDB健康检查通过
warn: GreptimeDB健康状态: False
error: GreptimeDB服务不健康，建议检查:
error: 1. GreptimeDB服务是否正在运行
error: 2. 网络连接是否正常
error: 3. 配置URL是否正确
error: 4. 数据库是否存在
```

## 常见错误码说明

| 错误码 | 含义 | 解决方案 |
|--------|------|----------|
| ServiceUnavailable (503) | 服务不可用 | 检查 GreptimeDB 容器状态 |
| NotFound (404) | 端点不存在 | 验证 URL 配置 |
| BadRequest (400) | 请求格式错误 | 检查 SQL 语法 |
| Unauthorized (401) | 认证失败 | 验证用户名密码配置 |
| InternalServerError (500) | 服务器内部错误 | 查看 GreptimeDB 日志 |

## 联系支持

如果问题仍然存在，请提供以下信息：

1. **错误日志** - 完整的错误堆栈跟踪
2. **配置文件** - appsettings.json 相关部分（隐藏敏感信息）
3. **容器状态** - `docker ps` 输出
4. **GreptimeDB 日志** - 容器日志
5. **网络测试** - `curl` 命令的输出结果

执行以下命令收集诊断信息：

```bash
# 收集系统信息
echo "=== 容器状态 ===" > diagnostic.log
docker ps >> diagnostic.log

echo "=== GreptimeDB 日志 ===" >> diagnostic.log
docker logs $(docker ps -q --filter ancestor=greptime/greptimedb) >> diagnostic.log 2>&1

echo "=== 连接测试 ===" >> diagnostic.log
curl -v http://localhost:4000/health >> diagnostic.log 2>&1

echo "=== 端口监听 ===" >> diagnostic.log
netstat -an | grep 4000 >> diagnostic.log
```

然后将 `diagnostic.log` 文件提供给技术支持团队。
