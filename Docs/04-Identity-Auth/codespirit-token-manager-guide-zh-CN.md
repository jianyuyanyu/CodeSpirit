# CodeSpirit.TokenManager 前端认证管理器使用指南

## 概述

`TokenManager` 是 CodeSpirit 项目的统一前端认证管理器，提供了系统平台和租户平台的双模式Token管理功能。它负责处理用户认证token的存储、获取、刷新和清除，确保不同平台间的Token完全隔离，避免冲突和安全问题。

### 核心特性

- ✅ **双平台支持**：系统平台和租户平台完全隔离
- 🔒 **安全隔离**：不同平台使用独立的存储key
- 📦 **向后兼容**：完全兼容旧版本API
- 🎯 **智能模式切换**：根据平台类型自动使用对应存储
- 🔄 **自动刷新**：支持Token自动刷新机制
- 📱 **现代化API**：支持ES6+、AMD、CommonJS等模块化规范

## 文件位置

```
Src/CodeSpirit.Web/wwwroot/js/token-manager.js
```

## 快速开始

### 1. 系统平台使用

```javascript
// 初始化为系统模式
TokenManager.initSystemMode();

// 设置token（24小时过期）
TokenManager.setToken('your-access-token', 24);

// 获取token
const token = TokenManager.getToken();

// 检查是否已登录
if (TokenManager.hasToken()) {
    console.log('用户已登录');
}
```

### 2. 租户平台使用

```javascript
// 初始化为租户模式
TokenManager.initTenantMode('tenant-001');

// 设置完整token信息
TokenManager.setTokenExtended(
    'access-token',
    'refresh-token', 
    3600, // 过期时间（秒）
    'tenant-001'
);

// 获取token（自动使用租户存储）
const token = TokenManager.getToken();

// 获取认证头（自动包含租户信息）
const headers = TokenManager.getAuthHeaders();
```

## API 文档

### 平台模式管理

#### `initSystemMode()`
初始化为系统模式。

```javascript
TokenManager.initSystemMode();
```

#### `initTenantMode(tenantId)`
初始化为租户模式。

**参数：**
- `tenantId` (string) - 租户ID

```javascript
TokenManager.initTenantMode('tenant-001');
```

### Token 管理（兼容API）

#### `setToken(token, expiryInHours)`
设置认证token（兼容旧版本API）。

**参数：**
- `token` (string) - 访问token
- `expiryInHours` (number, 可选) - 过期时间（小时），默认24小时

```javascript
TokenManager.setToken('your-token', 24);
```

#### `getToken()`
获取当前有效的认证token。

**返回值：**
- `string|null` - 访问token，如果不存在或已过期返回null

```javascript
const token = TokenManager.getToken();
```

#### `clearToken()`
清除所有认证信息。

```javascript
TokenManager.clearToken();
```

#### `hasToken()`
检查是否有有效token。

**返回值：**
- `boolean` - 是否有有效token

```javascript
if (TokenManager.hasToken()) {
    // 用户已登录
}
```

#### `isTokenExpired()`
检查token是否已过期。

**返回值：**
- `boolean` - 是否已过期

```javascript
if (TokenManager.isTokenExpired()) {
    // Token已过期，需要重新登录
}
```

#### `refreshTokenExpiry(expiryInHours)`
刷新token的过期时间。

**参数：**
- `expiryInHours` (number, 可选) - 过期时间（小时），默认24小时

```javascript
TokenManager.refreshTokenExpiry(48); // 延长48小时
```

### Token 管理（扩展API）

#### `setTokenExtended(token, refreshToken, expiresIn, tenantId)`
设置认证token（扩展版本，支持刷新token等）。

**参数：**
- `token` (string) - 访问token
- `refreshToken` (string, 可选) - 刷新token
- `expiresIn` (number, 可选) - 过期时间（秒）
- `tenantId` (string, 可选) - 租户ID

```javascript
TokenManager.setTokenExtended(
    'access-token',
    'refresh-token',
    3600,
    'tenant-001'
);
```

#### `getRefreshToken()`
获取刷新token。

**返回值：**
- `string|null` - 刷新token

```javascript
const refreshToken = TokenManager.getRefreshToken();
```

#### `refreshToken(refreshUrl)`
使用刷新token获取新的访问token。

**参数：**
- `refreshUrl` (string, 可选) - 刷新token的API地址

**返回值：**
- `Promise<boolean>` - 刷新是否成功

```javascript
const success = await TokenManager.refreshToken();
if (success) {
    console.log('Token刷新成功');
} else {
    console.log('Token刷新失败，需要重新登录');
}
```

#### `startAutoRefresh()`
启动自动刷新机制（在token过期前5分钟自动刷新）。

```javascript
TokenManager.startAutoRefresh();
```

### 用户信息管理

#### `setUserInfo(userInfo)`
设置用户信息。

**参数：**
- `userInfo` (Object) - 用户信息对象

```javascript
TokenManager.setUserInfo({
    id: 1,
    name: 'John Doe',
    email: 'john@example.com'
});
```

#### `getUserInfo()`
获取用户信息。

**返回值：**
- `Object|null` - 用户信息对象

```javascript
const userInfo = TokenManager.getUserInfo();
```

### 平台信息管理

#### `getPlatformInfo()`
获取平台信息（租户信息或系统信息）。

**返回值：**
- `Object|null` - 平台信息对象

```javascript
const platformInfo = TokenManager.getPlatformInfo();
```

#### `getTenantInfo()`
获取租户信息（兼容方法，仅在租户模式下有效）。

**返回值：**
- `Object|null` - 租户信息对象

```javascript
const tenantInfo = TokenManager.getTenantInfo();
```

### 认证相关

#### `isAuthenticated()`
检查用户是否已认证。

**返回值：**
- `boolean` - 是否已认证

```javascript
if (TokenManager.isAuthenticated()) {
    // 用户已认证且token有效
}
```

#### `getAuthHeaders()`
获取认证头信息。

**返回值：**
- `Object` - 包含Authorization头的对象，租户模式下自动包含X-Tenant-Id头

```javascript
const headers = TokenManager.getAuthHeaders();
// 系统模式: { 'Authorization': 'Bearer token' }
// 租户模式: { 'Authorization': 'Bearer token', 'X-Tenant-Id': 'tenant-001' }
```

### 跨平台管理

#### `hasOtherPlatformToken()`
检查是否存在其他平台的Token。

**返回值：**
- `boolean` - 是否存在其他平台Token

```javascript
if (TokenManager.hasOtherPlatformToken()) {
    console.log('检测到其他平台的Token');
}
```

#### `clearOtherPlatformToken()`
清除其他平台的Token。

```javascript
TokenManager.clearOtherPlatformToken();
```

### 属性访问

#### `platformType`
获取当前平台类型。

**返回值：**
- `string` - 'system' 或 'tenant'

```javascript
console.log('当前平台:', TokenManager.platformType);
```

#### `currentTenantId`
获取当前租户ID。

**返回值：**
- `string|null` - 当前租户ID

```javascript
console.log('当前租户ID:', TokenManager.currentTenantId);
```

#### `TOKEN_KEY`
获取当前平台的Token存储key。

```javascript
console.log('Token存储key:', TokenManager.TOKEN_KEY);
```

#### `TOKEN_EXPIRY_KEY`
获取当前平台的Token过期时间存储key。

```javascript
console.log('Token过期时间存储key:', TokenManager.TOKEN_EXPIRY_KEY);
```

## 存储Key映射

### 系统平台
| 数据类型 | 存储Key | 说明 |
|----------|---------|------|
| 访问Token | `token` | 系统平台访问token |
| 刷新Token | `refresh_token` | 系统平台刷新token |
| 用户信息 | `user_info` | 系统平台用户信息 |
| 过期时间 | `token_expiry` | 系统平台token过期时间 |
| 平台信息 | `system_info` | 系统平台相关信息 |

### 租户平台
| 数据类型 | 存储Key | 说明 |
|----------|---------|------|
| 访问Token | `tenant_auth_token` | 租户平台访问token |
| 刷新Token | `tenant_refresh_token` | 租户平台刷新token |
| 用户信息 | `tenant_user_info` | 租户平台用户信息 |
| 过期时间 | `tenant_token_expiry` | 租户平台token过期时间 |
| 租户信息 | `tenant_info` | 租户相关信息 |

## 使用场景

### 1. 系统后台管理

```javascript
// admin.js
(function () {
    // 初始化为系统模式
    TokenManager.initSystemMode();
    
    // 在请求适配器中使用
    const amisHandlers = {
        requestAdaptor: (api) => {
            const token = TokenManager.getToken();
            return {
                ...api,
                headers: {
                    ...api.headers,
                    'Authorization': token ? 'Bearer ' + token : '',
                    'X-Forwarded-With': 'CodeSpirit'
                }
            };
        }
    };
})();
```

### 2. 系统登录页面

```javascript
// login.js
(function () {
    // 初始化为系统模式
    TokenManager.initSystemMode();
    
    // 清除旧token
    TokenManager.clearToken();
    
    // 登录成功处理
    function onLoginSuccess(payload) {
        if (payload.data && payload.data.token) {
            TokenManager.setToken(payload.data.token, 24);
            window.location.href = '/';
        }
    }
})();
```

### 3. 租户后台管理

```javascript
// tenant-admin.js
(function () {
    const tenantId = window.tenantId;
    
    // 初始化为租户模式
    TokenManager.initTenantMode(tenantId);
    
    // 在请求适配器中使用（自动包含租户头）
    const amisHandlers = {
        requestAdaptor: (api) => {
            return {
                ...api,
                headers: {
                    ...api.headers,
                    ...TokenManager.getAuthHeaders(), // 自动包含Authorization和X-Tenant-Id
                    'X-Forwarded-With': 'CodeSpirit'
                }
            };
        }
    };
})();
```

### 4. 租户登录页面

```javascript
// tenant-login.js
(function () {
    const tenantId = window.tenantId;
    
    // 初始化为租户模式
    TokenManager.initTenantMode(tenantId);
    
    // 清除旧token
    TokenManager.clearToken();
    
    // 登录成功处理
    function onLoginSuccess(payload, tenant) {
        if (payload.data && payload.data.token) {
            TokenManager.setTokenExtended(
                payload.data.token,
                payload.data.refreshToken,
                payload.data.expiresIn,
                tenant.tenantId
            );
            
            if (payload.data.user) {
                TokenManager.setUserInfo(payload.data.user);
            }
            
            window.location.href = `/${tenant.tenantId}/admin`;
        }
    }
})();
```

### 5. 自动Token刷新

```javascript
// 启动自动刷新（在应用初始化时）
TokenManager.startAutoRefresh();

// 手动刷新
async function refreshTokenIfNeeded() {
    if (TokenManager.isTokenExpired()) {
        const success = await TokenManager.refreshToken();
        if (!success) {
            // 刷新失败，跳转到登录页
            window.location.href = '/login';
        }
    }
}
```

## 页面集成

### 在HTML页面中引用

所有页面都应该在主布局文件中引用 token-manager.js：

```html
<!-- _Layout.cshtml -->
<body>
    <div class="page-container">
        @RenderBody()
    </div>
    <resource path="js/token-manager.js" type="js" />
    @await RenderSectionAsync("Scripts", required: false)
</body>
```

### 系统平台页面

```html
<!-- 系统后台页面 -->
@section Scripts
{
    <script>
        window.webHost = "@(HttpContext.Request.Scheme + "://" + HttpContext.Request.Host)";
    </script>
    <resource path="js/admin.js" type="js" />
}
```

### 租户平台页面

```html
<!-- 租户后台页面 -->
@section Scripts
{
    <script>
        window.webHost = "@(HttpContext.Request.Scheme + "://" + HttpContext.Request.Host)";
        window.tenantId = "@Model.TenantId";
        window.platformType = "tenant";
    </script>
    <resource path="js/tenant-admin.js" type="js" />
}
```

## 最佳实践

### 1. 平台模式初始化

```javascript
// ✅ 在每个JavaScript文件的开头明确初始化平台模式
(function () {
    // 系统平台
    TokenManager.initSystemMode();
    
    // 或者租户平台
    // TokenManager.initTenantMode(window.tenantId);
    
    // 其他业务逻辑...
})();
```

### 2. 错误处理

```javascript
// ✅ 在API请求中处理认证错误
const amisHandlers = {
    responseAdaptor: function (api, payload, query, request, response) {
        if (response.status === 401) {
            // token过期或无效
            TokenManager.clearToken();
            
            if (TokenManager.platformType === 'tenant') {
                window.location.href = `/${TokenManager.currentTenantId}/login`;
            } else {
                window.location.href = '/login';
            }
            return { msg: '登录过期！' };
        }
        return payload;
    }
};
```

### 3. 安全性考虑

```javascript
// ✅ 在切换平台前清除其他平台的token
function switchToTenantMode(tenantId) {
    TokenManager.clearOtherPlatformToken(); // 清除系统平台token
    TokenManager.initTenantMode(tenantId);
}

function switchToSystemMode() {
    TokenManager.clearOtherPlatformToken(); // 清除租户平台token
    TokenManager.initSystemMode();
}
```

### 4. 调试和监控

```javascript
// ✅ 添加调试信息
console.log('当前平台类型:', TokenManager.platformType);
console.log('是否已认证:', TokenManager.isAuthenticated());
console.log('Token是否过期:', TokenManager.isTokenExpired());

if (TokenManager.platformType === 'tenant') {
    console.log('当前租户ID:', TokenManager.currentTenantId);
}
```

## 注意事项

### 1. 平台模式切换
- 必须在使用任何Token操作前调用 `initSystemMode()` 或 `initTenantMode()`
- 不要在运行时频繁切换平台模式
- 切换平台时建议清除其他平台的Token

### 2. 存储安全
- Token存储在localStorage中，注意XSS攻击防护
- 不要在URL参数或页面源码中暴露Token
- 考虑在生产环境中使用更安全的存储方式

### 3. 浏览器兼容性
- 需要支持localStorage的浏览器
- 需要支持Promise的浏览器或相应的polyfill
- 建议在现代浏览器中使用

### 4. 性能考虑
- localStorage操作是同步的，避免频繁读取
- 考虑缓存Token值，减少localStorage访问
- 自动刷新机制会定期检查Token状态

## 故障排除

### 常见问题

#### 1. Token丢失
```javascript
// 检查是否正确初始化平台模式
console.log('平台类型:', TokenManager.platformType);
console.log('存储Key:', TokenManager.TOKEN_KEY);

// 检查localStorage中的实际值
console.log('实际存储值:', localStorage.getItem(TokenManager.TOKEN_KEY));
```

#### 2. 跨平台Token冲突
```javascript
// 检查是否存在其他平台的Token
if (TokenManager.hasOtherPlatformToken()) {
    console.log('发现其他平台Token，建议清除');
    TokenManager.clearOtherPlatformToken();
}
```

#### 3. Token过期处理
```javascript
// 检查Token状态
console.log('Token是否存在:', TokenManager.hasToken());
console.log('Token是否过期:', TokenManager.isTokenExpired());
console.log('是否已认证:', TokenManager.isAuthenticated());

// 尝试刷新Token
if (TokenManager.isTokenExpired() && TokenManager.getRefreshToken()) {
    TokenManager.refreshToken().then(success => {
        console.log('刷新结果:', success);
    });
}
```

## 版本历史

### v2.0.0 (当前版本)
- ✨ 新增双平台模式支持
- ✨ 新增自动Token刷新机制
- ✨ 新增扩展API支持
- ✅ 完全向后兼容v1.0
- 🔒 增强安全性和隔离性

### v1.0.0 (旧版本)
- 基础Token管理功能
- 单一存储模式
- 简单的过期时间管理

## 相关文档

- [CodeSpirit.IdentityApi身份认证服务](./codespirit-identity-api-zh-CN.md)
- [多租户登录页面使用指南](../05-Multi-Tenancy/multi-tenant-login-page-guide-zh-CN.md)
- [CodeSpirit多租户组件整改计划](../05-Multi-Tenancy/codespirit-multi-tenant-refactor-plan-zh-CN.md)
- [项目整体架构设计](../01-Core-Docs/01-project-architecture-zh-CN.md)

## 联系支持

如有问题或建议，请通过以下方式联系：

- 提交Issue到项目仓库
- 联系开发团队
- 查看项目Wiki获取更多信息 