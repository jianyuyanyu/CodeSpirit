# 租户管理功能说明

## 概述

CodeSpirit.IdentityApi 现已集成完整的租户管理功能，支持多租户架构下的用户和角色隔离。

## 功能特性

### 1. 租户管理
- ✅ 租户创建、更新、删除
- ✅ 租户启用/禁用
- ✅ 租户信息查询和分页
- ✅ 租户统计信息
- ✅ 租户过期检查和续期

### 2. 多租户策略支持
- **共享数据库** (SharedDatabase): 所有租户共享同一数据库，通过 TenantId 字段隔离数据
- **独立数据库** (SeparateDatabase): 每个租户使用独立的数据库

### 3. 数据隔离
- 用户数据按租户隔离
- 角色数据按租户隔离
- 自动过滤查询，确保数据安全

## API 接口

### 租户管理接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/tenants` | 获取租户列表（支持分页和筛选） |
| GET | `/api/tenants/{id}` | 获取租户详情 |
| POST | `/api/tenants` | 创建租户 |
| PUT | `/api/tenants/{id}` | 更新租户 |
| DELETE | `/api/tenants/{id}` | 删除租户 |
| PUT | `/api/tenants/{id}/enable` | 启用租户 |
| PUT | `/api/tenants/{id}/disable` | 禁用租户 |
| GET | `/api/tenants/{id}/statistics` | 获取租户统计信息 |
| GET | `/api/tenants/{id}/expired` | 检查租户是否过期 |
| PUT | `/api/tenants/{id}/renew` | 续期租户 |
| DELETE | `/api/tenants/batch` | 批量删除租户 |

### 请求示例

#### 创建租户
```json
POST /api/tenants
{
  "tenantId": "company-a",
  "name": "公司A",
  "displayName": "公司A有限公司",
  "description": "这是公司A的租户",
  "strategy": "SharedDatabase",
  "domain": "company-a.example.com",
  "maxUsers": 100,
  "storageLimit": 5120,
  "expiresAt": "2025-12-31T23:59:59Z"
}
```

#### 查询租户列表
```
GET /api/tenants?pageIndex=1&pageSize=20&name=公司&isActive=true
```

#### 获取租户统计
```json
GET /api/tenants/company-a/statistics

响应:
{
  "status": 0,
  "msg": "操作成功！",
  "data": {
    "tenantId": "company-a",
    "tenantName": "公司A",
    "userCount": 25,
    "roleCount": 5,
    "maxUsers": 100,
    "storageUsed": 1024,
    "storageLimit": 5120,
    "isActive": true,
    "isExpired": false,
    "expiresAt": "2025-12-31T23:59:59Z",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

## 数据模型

### TenantInfo 实体
```csharp
public class TenantInfo
{
    public string Id { get; set; }              // 主键
    public string TenantId { get; set; }        // 租户ID（业务标识）
    public string Name { get; set; }            // 租户名称
    public string DisplayName { get; set; }     // 显示名称
    public string Description { get; set; }     // 描述
    public TenantStrategy Strategy { get; set; } // 租户策略
    public string ConnectionString { get; set; } // 连接字符串（独立数据库时使用）
    public string TablePrefix { get; set; }     // 表前缀
    public bool IsActive { get; set; }          // 是否启用
    public string Configuration { get; set; }   // 租户配置（JSON）
    public string Domain { get; set; }          // 租户域名
    public string LogoUrl { get; set; }         // Logo URL
    public string ThemeConfig { get; set; }     // 主题配置
    public int MaxUsers { get; set; }           // 最大用户数
    public long StorageLimit { get; set; }      // 存储限制（MB）
    public DateTime? ExpiresAt { get; set; }    // 过期时间
    public DateTime CreatedAt { get; set; }     // 创建时间
    // ... 审计字段
}
```

### 租户策略枚举
```csharp
public enum TenantStrategy
{
    SharedDatabase = 1,    // 共享数据库
    SeparateDatabase = 2   // 独立数据库
}
```

## 多租户数据隔离

### 用户实体
```csharp
public class ApplicationUser : IdentityUser<long>, IMultiTenant
{
    public string TenantId { get; set; }  // 租户ID
    // ... 其他属性
}
```

### 角色实体
```csharp
public class ApplicationRole : IdentityRole<long>, IMultiTenant
{
    public string TenantId { get; set; }  // 租户ID
    // ... 其他属性
}
```

## 使用说明

### 1. 租户解析
系统支持多种租户解析方式：
- **HTTP Header**: 通过 `TenantId` 请求头传递
- **Query 参数**: 通过 `tenantId` 查询参数传递
- **子域名**: 通过子域名解析（可配置）
- **路径**: 通过 URL 路径解析（可配置）

### 2. 配置示例
```json
{
  "MultiTenant": {
    "Enabled": true,
    "DefaultTenantId": "default",
    "ResolveFromHeader": true,
    "TenantHeaderName": "TenantId",
    "ResolveFromQuery": true,
    "TenantQueryName": "tenantId",
    "ResolveFromSubdomain": false,
    "ResolveFromPath": false,
    "StoreType": "Memory",
    "EnableTenantCache": true,
    "CacheExpirationMinutes": 30
  }
}
```

### 3. 数据库迁移
在启用多租户功能后，需要执行数据库迁移以添加租户相关字段：

```bash
dotnet ef migrations add AddMultiTenant
dotnet ef database update
```

## 注意事项

1. **数据安全**: 所有查询都会自动添加租户过滤条件，确保数据隔离
2. **性能优化**: 建议在 TenantId 字段上创建索引以提高查询性能
3. **缓存策略**: 租户信息会被缓存，默认缓存30分钟
4. **权限控制**: 租户管理功能需要相应的权限才能访问

## 开发指南

### 添加新的多租户实体
1. 实现 `IMultiTenant` 接口
2. 添加 `TenantId` 属性
3. 在 DbContext 中配置全局查询过滤器

```csharp
public class MyEntity : IMultiTenant
{
    public string TenantId { get; set; }
    // ... 其他属性
}
```

### 服务层开发
继承 `BaseCRUDService` 并实现相应的接口：

```csharp
public class MyService : BaseCRUDService<MyEntity, MyDto, string, MyCreateDto, MyUpdateDto>, IMyService
{
    // 实现业务逻辑
}
```

## 故障排除

### 常见问题
1. **租户ID为空**: 检查请求头或查询参数是否正确传递
2. **数据查询为空**: 确认当前租户下是否有数据
3. **权限错误**: 检查用户是否有租户管理权限

### 日志调试
启用多租户相关日志：
```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.MultiTenant": "Debug"
    }
  }
}
``` 