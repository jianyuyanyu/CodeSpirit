# CodeSpirit.MultiTenant 使用示例

本目录包含了 CodeSpirit.MultiTenant 组件的完整使用示例，展示了如何在实际项目中集成和使用多租户功能。

## 示例文件说明

### MultiTenantExample.cs

包含完整的多租户实现示例，包括：

#### 1. 服务配置示例
- 多租户服务注册
- 数据库上下文配置
- 业务服务注册

#### 2. 中间件配置示例
- 多租户中间件注册
- 路由配置

#### 3. 多租户实体示例 (Order)
- 实现 `IMultiTenant` 接口
- 包含租户ID字段

#### 4. 多租户数据库上下文示例 (ExampleDbContext)
- 全局查询过滤器配置
- 自动设置租户ID
- 支持多租户数据隔离

#### 5. 业务服务示例 (OrderService)
- 租户感知的业务逻辑
- 数据库操作中的租户处理
- 日志记录

#### 6. 控制器示例 (OrdersController)
- 租户信息获取
- RESTful API 实现
- 多租户数据操作

## 使用方法

### 1. 在 Program.cs 中配置服务

```csharp
// 使用示例中的配置方法
MultiTenantExample.ConfigureServices(builder.Services, builder.Configuration);
```

### 2. 配置中间件

```csharp
// 使用示例中的中间件配置
MultiTenantExample.ConfigureMiddleware(app);
```

### 3. 配置文件示例

```json
{
  "MultiTenant": {
    "Enabled": true,
    "DefaultTenantId": "default",
    "ResolveFromHeader": true,
    "TenantHeaderName": "TenantId",
    "ResolveFromQuery": true,
    "TenantQueryName": "tenantId",
    "StoreType": "Memory",
    "EnableTenantCache": true,
    "CacheExpirationMinutes": 30
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MultiTenantExample;Trusted_Connection=true;",
    "Redis": "localhost:6379"
  }
}
```

## 关键特性演示

### 1. 租户解析
- 从HTTP Header解析：`TenantId: tenant-001`
- 从Query参数解析：`?tenantId=tenant-001`
- 支持子域名和路径解析

### 2. 数据隔离
- 共享数据库模式下的自动数据过滤
- 新增数据时自动设置租户ID
- 查询时自动应用租户过滤器

### 3. 服务集成
- 依赖注入中的租户解析器使用
- 业务逻辑中的租户信息获取
- 控制器中的租户验证

## 测试示例

参考同级目录下的单元测试文件：
- `MemoryTenantStoreTests.cs` - 租户存储测试
- `TenantResolverTests.cs` - 租户解析器测试
- `MultiTenantMiddlewareTests.cs` - 中间件测试
- `ServiceCollectionExtensionsTests.cs` - 服务注册测试

## 最佳实践

1. **实体设计**：所有需要多租户隔离的实体都应实现 `IMultiTenant` 接口
2. **数据库设计**：使用全局查询过滤器确保数据隔离
3. **服务设计**：在业务服务中始终验证租户权限
4. **缓存策略**：启用租户缓存以提升性能
5. **错误处理**：合理处理租户解析失败的情况

## 扩展示例

如需更复杂的场景，可以参考：
- 自定义租户存储实现
- 多数据库租户策略
- 租户级别的配置管理
- 租户数据迁移工具 