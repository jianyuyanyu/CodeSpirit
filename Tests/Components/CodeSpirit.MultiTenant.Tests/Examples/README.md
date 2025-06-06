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

# API租户存储配置示例

本目录包含了使用 API 存储作为租户数据源的配置示例。

## 配置文件

### appsettings.development.example.json

开发环境配置示例，使用内存存储，简化配置便于快速开发。

### appsettings.api-store.example.json

基于 `CodeSpirit.IdentityApi.Controllers.TenantsController` 的端点配置示例，适用于集成测试和开发环境。

### appsettings.aspire.example.json

.NET Aspire 环境配置示例，使用服务发现进行内部通信。

### appsettings.k8s.example.json

Kubernetes 环境配置示例，使用集群内服务发现。

### appsettings.production.example.json

生产环境配置示例，包含性能优化配置。

## 端点映射

基于 `TenantsController` 中的实际端点，API租户存储的端点映射如下：

| 功能 | HTTP方法 | 端点 | Controller方法 |
|------|----------|------|----------------|
| 获取租户详情 | GET | `/api/tenants/{tenantId}` | `GetTenant` |
| 获取活跃租户列表 | GET | `/api/tenants/active` | `GetActiveTenants` |
| 创建租户 | POST | `/api/tenants` | `CreateTenant` |
| 更新租户 | PUT | `/api/tenants/{tenantId}` | `UpdateTenant` |
| 删除租户 | DELETE | `/api/tenants/{tenantId}` | `DeleteTenant` |
| 检查租户是否存在 | HEAD | `/api/tenants/{tenantId}` | `GetTenant` (使用HEAD请求) |

## 响应格式

所有端点都使用标准的 `ApiResponse<T>` 格式：

```json
{
  "status": 0,
  "msg": "操作成功！",
  "data": {
    // 租户数据
  }
}
```

- `status`: 0表示成功，非0表示错误
- `msg`: 响应消息
- `data`: 实际数据

## 租户数据格式

获取租户详情返回的数据格式：

```json
{
  "tenantId": "tenant-001",
  "name": "租户名称",
  "displayName": "租户显示名称",
  "description": "租户描述",
  "strategy": "SharedDatabase",
  "isActive": true,
  "domain": "tenant001.example.com",
  "maxUsers": 100,
  "storageLimit": 1073741824,
  "expiresAt": "2024-12-31T23:59:59Z",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

获取活跃租户列表返回的数据格式：

```json
[
  {
    "tenantId": "tenant-001",
    "name": "租户名称",
    "displayName": "租户显示名称",
    "description": "租户描述",
    "logoUrl": "https://example.com/logo.png"
  }
]
```

## 使用说明

1. 将示例配置复制到你的应用程序的 `appsettings.json` 中
2. 根据部署环境选择合适的 `BaseUrl` 配置：
   - **开发环境**: `http://localhost:5001` 或 `https://localhost:5001`
   - **.NET Aspire**: `http://identityapi` (使用服务名称)
   - **Kubernetes**: `http://identity-api.default.svc.cluster.local` (使用FQDN)
   - **Docker Compose**: `http://identity-api` (使用服务名称)
3. 根据需要调整其他配置项

## 内网部署配置

API存储专门为内部网络通信设计，无需API密钥认证：

- **网络隔离**: 通过容器网络或Kubernetes网络策略实现安全隔离
- **服务发现**: 支持.NET Aspire、Kubernetes、Docker Compose等服务发现机制
- **简化配置**: 无需管理API密钥，降低配置复杂性
- **高性能**: 内网通信延迟低，适合高频租户信息查询

## 故障处理

API租户存储具有以下故障处理机制：

1. **网络超时**: 可配置超时时间，默认30秒
2. **API错误**: 会记录详细的错误日志
3. **缓存机制**: 启用缓存可减少API调用次数
4. **失败策略**: 可配置失败时的处理策略（使用默认租户、抛出异常、返回404）

## 性能优化

1. **启用缓存**: 设置 `EnableTenantCache: true`
2. **合理的缓存过期时间**: 根据租户数据变更频率设置
3. **连接池**: HttpClient会自动使用连接池
4. **压缩**: 如果API支持，可以启用GZIP压缩 