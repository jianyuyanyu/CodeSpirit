# 多租户登录页面使用指南

## 概述

CodeSpirit 系统现已支持独立登录页面（Custom Login URL）的多租户方案。每个租户都有独立的登录URL，支持租户品牌化定制和主题配置。

## 功能特性

### 1. 独立登录URL
- 每个租户有独立的登录入口
- 支持路径格式：`/{tenantId}/login`
- 自动租户识别和验证

### 2. 租户品牌化
- 支持租户Logo定制
- 支持主题颜色配置
- 支持自定义CSS样式
- 支持租户描述信息

### 3. 安全特性
- 路径级别的租户验证
- 用户租户归属验证
- 完全的数据隔离
- 登录日志记录

### 4. 管理功能
- 租户登录页面跳转功能
- 租户登录配置获取
- 活跃租户列表查询

## 使用方法

### 1. 访问租户登录页面

用户可以通过以下URL格式访问特定租户的登录页面：

```
https://yourdomain.com/{tenantId}/login
```

例如：
- `https://app.com/company-a/login` - 公司A的登录页
- `https://app.com/school-b/login` - 学校B的登录页
- `https://app.com/org-c/login` - 组织C的登录页

### 2. 租户配置

#### 基本信息配置
```json
{
  "tenantId": "company-a",
  "name": "公司A",
  "displayName": "公司A有限公司",
  "description": "这是公司A的企业门户",
  "logoUrl": "https://company-a.com/logo.png",
  "domain": "company-a.example.com",
  "isActive": true
}
```

#### 主题配置
```json
{
  "themeConfig": {
    "primaryColor": "#1890ff",
    "backgroundColor": "#f0f2f5",
    "textColor": "#333333",
    "borderColor": "#d9d9d9",
    "customCss": ".tenant-title { font-family: 'Arial', sans-serif; }"
  }
}
```

#### 功能配置
```json
{
  "configuration": {
    "enableSsoLogin": true,
    "enableSmsLogin": false,
    "enableRememberMe": true,
    "sessionTimeout": 30
  }
}
```

### 3. API接口

#### 获取活跃租户列表
```http
GET /identity/api/identity/tenants/active
```

响应示例：
```json
{
  "status": 0,
  "msg": "操作成功",
  "data": [
    {
      "tenantId": "company-a",
      "name": "公司A",
      "displayName": "公司A有限公司",
      "description": "企业门户",
      "logoUrl": "https://company-a.com/logo.png"
    }
  ]
}
```

#### 跳转到租户登录页面
```http
POST /identity/api/identity/tenants/{tenantId}/redirect-login
```

响应示例：
```json
{
  "status": 0,
  "msg": "租户验证成功，即将跳转到登录页面",
  "data": {
    "tenantId": "company-a",
    "loginUrl": "/company-a/login",
    "tenantName": "公司A有限公司",
    "description": "企业门户",
    "logoUrl": "https://company-a.com/logo.png"
  }
}
```

#### 获取租户登录配置
```http
GET /identity/api/identity/tenants/{tenantId}/login-config
```

响应示例：
```json
{
  "status": 0,
  "msg": "操作成功",
  "data": {
    "tenantId": "company-a",
    "name": "公司A",
    "displayName": "公司A有限公司",
    "description": "企业门户",
    "logoUrl": "https://company-a.com/logo.png",
    "domain": "company-a.example.com",
    "themeConfig": {
      "primaryColor": "#1890ff",
      "backgroundColor": "#f0f2f5"
    },
    "configuration": {
      "enableSsoLogin": true,
      "enableRememberMe": true
    },
    "isActive": true,
    "expiresAt": "2025-12-31T23:59:59Z"
  }
}
```

#### 租户登录
```http
POST /identity/api/identity/auth/login
Headers:
  TenantId: company-a
  Content-Type: application/json

Body:
{
  "userName": "user@company-a.com",
  "password": "password123",
  "tenantId": "company-a"
}
```

## 技术实现

### 1. 路由配置

在 `Program.cs` 中配置租户路由：

```csharp
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/TenantLogin", "/{tenantId}/login");
});
```

### 2. 中间件配置

租户路径解析中间件自动从URL中提取租户信息：

```csharp
app.UseMiddleware<TenantPathMiddleware>();
```

### 3. 前端集成

在租户登录页面中，JavaScript会自动：
- 应用租户主题配置
- 构建品牌化界面
- 处理登录请求
- 设置租户上下文

### 4. 后端验证

AuthService会验证：
- 租户是否存在且活跃
- 用户是否属于指定租户
- 登录凭据是否正确

### 5. 管理功能

TenantsController提供：
- `RedirectToTenantLogin` - 管理端跳转到租户登录页
- `GetTenantLoginConfig` - 获取租户登录配置
- `GetActiveTenants` - 获取活跃租户列表

## 部署配置

### 1. 多租户配置

在 `appsettings.json` 中配置：

```json
{
  "MultiTenant": {
    "Enabled": true,
    "DefaultTenantId": "default",
    "ResolveFromPath": true,
    "ResolveFromHeader": true,
    "TenantHeaderName": "TenantId",
    "EnableTenantCache": true,
    "CacheExpirationMinutes": 30
  }
}
```

### 2. 数据库配置

确保租户表已创建并包含必要的配置字段：
- TenantId（租户标识）
- ThemeConfig（主题配置）
- Configuration（功能配置）
- LogoUrl（Logo地址）

## 最佳实践

### 1. 租户ID命名规范
- 使用小写字母和连字符
- 避免特殊字符和空格
- 保持简短且有意义
- 例如：`company-a`, `school-beijing`, `org-finance`

### 2. 主题配置建议
- 使用品牌标准色彩
- 确保对比度符合可访问性要求
- 测试在不同设备上的显示效果
- 提供备用Logo和颜色方案

### 3. 安全考虑
- 定期审查租户配置
- 监控异常登录尝试
- 实施租户级别的访问控制
- 备份租户配置数据

### 4. 性能优化
- 启用租户信息缓存
- 优化Logo图片大小
- 使用CDN加速静态资源
- 监控页面加载性能

## 故障排除

### 1. 常见问题

**问题：访问租户登录页面显示404错误**
- 检查租户ID是否正确
- 确认租户是否存在且处于活跃状态
- 验证路由配置是否正确

**问题：登录失败提示"用户不属于指定租户"**
- 检查用户的TenantId字段
- 确认用户是否分配给正确的租户
- 验证租户ID大小写是否匹配

**问题：主题配置不生效**
- 检查ThemeConfig JSON格式是否正确
- 确认CSS变量名称是否正确
- 清除浏览器缓存重新测试

**问题：管理端跳转功能不工作**
- 确认租户状态为活跃
- 检查租户是否过期
- 验证Operation配置是否正确

### 2. 调试方法

启用详细日志记录：
```json
{
  "Logging": {
    "LogLevel": {
      "CodeSpirit.MultiTenant": "Debug",
      "CodeSpirit.Web.Middlewares": "Debug",
      "CodeSpirit.IdentityApi.Controllers.TenantsController": "Debug"
    }
  }
}
```

检查浏览器开发者工具：
- 网络请求是否包含正确的TenantId头
- 控制台是否有JavaScript错误
- 样式是否正确应用

## 扩展功能

### 1. 单点登录(SSO)集成
- 配置SAML或OAuth提供商
- 在租户配置中启用SSO
- 自定义SSO登录流程

### 2. 多语言支持
- 根据租户配置显示不同语言
- 支持RTL语言布局
- 动态加载语言资源

### 3. 高级主题定制
- 支持自定义CSS文件上传
- 提供主题预览功能
- 实现主题版本管理

### 4. 管理界面集成
- 在租户管理页面添加"进入登录"按钮
- 支持批量操作和预览功能
- 提供登录页面模板

## 联系支持

如果您在使用过程中遇到问题，请：
1. 查看系统日志获取详细错误信息
2. 参考本文档的故障排除部分
3. 联系技术支持团队获取帮助

---

*最后更新：2024年12月* 