# Cards SDK TokenManager 集成说明

## 重要说明

Cards SDK **必须**与 `TokenManager.js` 配合使用，不支持其他认证方式。

## 依赖要求

- **TokenManager.js**: 必须在Cards SDK之前加载
- **版本要求**: TokenManager v2.1.0+

## 使用方式

### 1. 确保加载顺序

```html
<!-- 必须先加载TokenManager -->
<script src="/js/token-manager.js"></script>
<!-- 然后加载Cards SDK -->
<script src="/cards-sdk/cards-sdk.js"></script>
```

### 2. 初始化TokenManager

```javascript
// 根据应用类型选择合适的模式

// 系统平台
TokenManager.initSystemMode();
TokenManager.setToken('your-access-token', 24);

// 租户平台
TokenManager.initTenantMode('tenant-id');
TokenManager.setTokenExtended('access-token', 'refresh-token', 3600, 'tenant-id');

// 客户端平台（考试系统）
TokenManager.initClientMode('tenant-id', 'exam');
TokenManager.setTokenExtended('access-token', 'refresh-token', 3600, 'tenant-id');
```

### 3. 初始化Cards SDK

```javascript
// TokenManager初始化完成后，再初始化Cards SDK
const cardsSDK = new CodeSpiritCards.SDK({
    container: '#cards-container',
    baseUrl: '/api'
});
```

## 自动功能

Cards SDK会自动：

1. **获取认证信息**: 从TokenManager获取token和认证头
2. **添加平台信息**: 根据平台类型添加相应的请求头
3. **处理认证失败**: 自动清理过期token
4. **错误处理**: 提供详细的错误信息和日志

## 请求头

SDK会自动添加以下请求头：

```javascript
// 基础认证头
{
    'Authorization': 'Bearer your-access-token',
    'Content-Type': 'application/json',
    'X-Card-ID': 'card-id',
    'X-Card-Type': 'table',
    'X-SDK-Version': '1.0.0'
}

// 租户模式额外头
{
    'X-Tenant-ID': 'tenant-id'
}

// 客户端模式额外头
{
    'X-Tenant-ID': 'tenant-id',
    'X-Client-Type': 'exam'
}
```

## 错误处理

如果TokenManager未找到，SDK会：

1. **初始化时**: 抛出错误并停止初始化
2. **API请求时**: 抛出错误并停止请求
3. **认证失败时**: 抛出错误无法处理

## 调试信息

SDK会输出详细的调试信息：

```javascript
// 初始化成功
🔐 TokenManager已就绪 - 平台类型: system (系统)
✅ 用户已认证
📋 平台信息: { platformType: "system" }

// 认证失败
🔐 系统认证失败，清除Token
❌ API错误: { status: 401, message: "Unauthorized" }
```

## 注意事项

1. **必须依赖**: TokenManager是必需依赖，不可选
2. **加载顺序**: 必须先加载TokenManager，再加载Cards SDK
3. **初始化顺序**: 必须先初始化TokenManager，再初始化Cards SDK
4. **错误处理**: 如果TokenManager不可用，SDK会抛出错误

## 示例代码

```html
<!DOCTYPE html>
<html>
<head>
    <!-- 必须先加载TokenManager -->
    <script src="/js/token-manager.js"></script>
    <!-- 然后加载Cards SDK -->
    <script src="/cards-sdk/cards-sdk.js"></script>
</head>
<body>
    <div id="cards-container"></div>
    
    <script>
        // 1. 初始化TokenManager
        if (window.TokenManager) {
            TokenManager.initSystemMode();
            TokenManager.setToken('demo-token-' + Date.now(), 24);
            TokenManager.setUserInfo({
                id: 'demo-user',
                name: '演示用户',
                role: 'admin'
            });
        } else {
            console.error('TokenManager未找到');
        }
        
        // 2. 初始化Cards SDK
        const cardsSDK = new CodeSpiritCards.SDK({
            container: '#cards-container',
            baseUrl: '/api'
        });
        
        // 3. 渲染表格卡片
        cardsSDK.render('#cards-container', [
            {
                id: 'demo-table',
                type: 'table',
                title: '演示表格',
                data: {
                    api: '/api/demo-data',
                    columns: [
                        { name: 'id', label: 'ID', type: 'text' },
                        { name: 'name', label: '名称', type: 'text' }
                    ]
                }
            }
        ]);
    </script>
</body>
</html>
``` 