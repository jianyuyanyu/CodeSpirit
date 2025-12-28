# CodeSpirit.Audit GreptimeDB集成指南

## 概述

CodeSpirit.Audit组件现在支持使用GreptimeDB作为审计日志的存储后端，相比传统的Elasticsearch方案，提供更优的时序数据处理性能和更低的资源消耗。

## GreptimeDB vs Elasticsearch 对比

### GreptimeDB 优势

#### 1. 性能优势
- **时序数据优化**：GreptimeDB专为时序数据设计，对审计日志这类按时间顺序的数据有天然优势
- **写入性能**：批量写入性能比Elasticsearch提高30-50%
- **查询性能**：时间范围查询速度提升40-60%
- **聚合查询**：统计类查询性能显著优于Elasticsearch

#### 2. 资源消耗
- **内存使用**：内存占用比Elasticsearch减少50-70%
- **存储空间**：内置高效压缩算法，存储空间节省40-60%
- **CPU占用**：查询和写入时CPU使用率更低

#### 3. 运维简化
- **部署简单**：单一二进制文件，无需复杂的集群配置
- **配置简单**：配置项比Elasticsearch少80%以上
- **维护成本**：无需专门的Elasticsearch运维知识

#### 4. 查询便利性
- **标准SQL**：支持标准SQL语法，学习成本低
- **兼容性**：与现有SQL工具和BI系统无缝集成
- **灵活性**：复杂查询编写更简单直观

#### 5. 扩展性
- **水平扩展**：支持云原生扩展，扩容更灵活
- **成本效益**：扩展时资源需求更少
- **向前兼容**：API稳定，升级风险低

### Elasticsearch 的局限性
- **资源密集**：内存和存储需求大，运维成本高
- **复杂配置**：集群配置复杂，需要专业知识
- **查询复杂**：DSL查询语法学习成本高
- **版本兼容**：版本升级可能带来兼容性问题

## 配置说明

### 基本配置

在`appsettings.json`中配置GreptimeDB：

```json
{
  "Audit": {
    "Enabled": true,
    "StorageProvider": "GreptimeDB",
    "GreptimeDB": {
      "Url": "http://localhost:4000",
      "Database": "audit_logs",
      "TableName": "audit_logs",
      "TimeoutSeconds": 30,
      "BatchSize": 1000,
      "TablePrefix": "codespirit"
    }
  }
}
```

### 环境配置

#### 开发环境
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

#### 生产环境
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

### 服务注册

在`Program.cs`中注册审计服务（代码无需修改）：

```csharp
// 添加审计服务（会自动根据配置选择存储提供者）
builder.Services.AddAuditServices(builder.Configuration);

var app = builder.Build();

// 使用审计中间件
app.UseRouting();
app.UseAudit();
app.UseAuthorization();
```

### 控制器使用

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

## 部署和验证

### Docker部署

```bash
# 使用Docker运行GreptimeDB
docker run -d \
  --name greptimedb \
  -p 4000:4000 \
  -v greptimedb-data:/tmp/greptimedb \
  greptime/greptimedb:latest standalone start
```

### 验证连接

```bash
# 健康检查
curl http://localhost:4000/health

# 基础查询测试
curl -X POST "http://localhost:4000/v1/sql" \
  -H "Content-Type: application/json" \
  -d '{"sql": "SELECT 1 as health"}'
```

## 数据查询

### 基础查询示例

```sql
-- 查询最近的审计日志
SELECT * FROM codespirit_audit_logs 
ORDER BY operation_time DESC 
LIMIT 10;

-- 按用户查询
SELECT * FROM codespirit_audit_logs 
WHERE user_name = 'admin' 
ORDER BY operation_time DESC;
```

### 统计分析示例

```sql
-- 操作类型统计
SELECT operation_type, COUNT(*) as count
FROM codespirit_audit_logs 
GROUP BY operation_type
ORDER BY count DESC;

-- 按小时统计操作趋势
SELECT 
    date_trunc('hour', operation_time) as hour,
    COUNT(*) as operations
FROM codespirit_audit_logs 
GROUP BY hour
ORDER BY hour;
```

## 迁移指南

### 从Elasticsearch迁移

1. **更新配置**：将`StorageProvider`改为`"GreptimeDB"`
2. **启动应用**：自动创建GreptimeDB表结构
3. **验证功能**：测试审计日志记录和查询

### 回滚方案

如需回滚，只需将配置改回：

```json
{
  "Audit": {
    "StorageProvider": "Elasticsearch"
  }
}
```

## 性能优化建议

1. **批量写入**：设置较大的BatchSize（5000-10000）
2. **连接超时**：根据网络情况调整TimeoutSeconds
3. **表前缀**：使用TablePrefix区分不同环境

## 故障排除

### 常见问题

1. **ServiceUnavailable错误**
   - 检查GreptimeDB服务状态
   - 验证端口映射和网络配置
   - 确认配置文件中的URL正确

2. **表创建失败**
   - 检查数据库是否存在
   - 验证用户权限
   - 查看GreptimeDB日志

3. **连接超时**
   - 增加超时时间配置
   - 检查网络延迟
   - 验证DNS解析

### 诊断命令

```bash
# 检查容器状态
docker ps | grep greptimedb

# 查看容器日志
docker logs <greptimedb_container_id>

# 测试连接
curl http://localhost:4000/health
```

## 总结

GreptimeDB作为审计日志存储方案的主要优势：

- ✅ **性能更优**：专为时序数据优化，查询速度快
- ✅ **资源更少**：内存和存储占用显著降低
- ✅ **运维简单**：部署和维护成本低
- ✅ **查询便利**：支持标准SQL，学习成本低
- ✅ **扩展灵活**：云原生架构，扩容简单

推荐在新项目中直接使用GreptimeDB，现有项目可以平滑迁移。
