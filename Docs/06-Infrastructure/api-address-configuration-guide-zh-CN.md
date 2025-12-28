# API地址配置指南

## 概述

CodeSpirit考试系统支持通过配置API基础地址来控制前端API请求的路由方式。这个功能特别适用于生产环境，可以让考试客户端直接请求API服务，绕过Web应用的代理处理，提升性能。

## 配置说明

### 站点设置配置

在 `appsettings.json` 中的 `SiteSettings` 节点添加 `ApiBaseUrl` 配置：

```json
{
  "SiteSettings": {
    "SiteName": "CodeSpirit",
    "TopSiteName": "CodeSpirit", 
    "LogoUrl": "/favicon.ico",
    "EnableCdn": false,
    "CdnUrl": "",
    "ApiBaseUrl": "",  // API基础地址配置
    "ResourceVersion": ""
  }
}
```

### 配置选项

#### 开发环境（使用代理）
```json
{
  "SiteSettings": {
    "ApiBaseUrl": ""  // 空值，使用Web应用代理
  }
}
```

#### 生产环境（直连API）
```json
{
  "SiteSettings": {
    "ApiBaseUrl": "https://api.example.com"  // API服务地址
  }
}
```

## URL转换规则

### 转换逻辑

当设置了 `ApiBaseUrl` 时，系统会自动转换考试相关的API请求：

- **原始URL**: `/exam/api/exam/client/profile`
- **转换后URL**: `https://api.example.com/api/exam/client/profile`

转换规则：
1. 移除URL中的 `/exam` 前缀
2. 在API基础地址后拼接剩余路径
3. 保持查询参数不变

### 示例对比

| 场景 | ApiBaseUrl | 原始URL | 转换后URL |
|------|------------|---------|-----------|
| 开发环境 | `""` | `/exam/api/exam/client/profile` | `/exam/api/exam/client/profile` |
| 生产环境 | `"https://api.example.com"` | `/exam/api/exam/client/profile` | `https://api.example.com/api/exam/client/profile` |

## 技术实现

### 前端实现

系统在前端实现了 `ExamApiManager` 模块来处理API地址转换：

```javascript
// 自动转换API URL
const transformedUrl = window.ExamApiManager.transformUrl('/exam/api/exam/client/profile');

// 统一的API请求
const data = await window.ExamApiManager.request('/exam/api/exam/client/profile');
```

### 影响范围

以下考试相关的JavaScript文件已经集成了API地址转换功能：

- `application.js` - 考试应用主页面
- `start.js` - 考试开始页面  
- `exam.js` - 考试答题页面
- `result.js` - 考试结果页面
- `practice.js` - 练习模式
- `practice-start.js` - 练习开始页面
- `practice-result.js` - 练习结果页面
- `practice-history.js` - 练习历史记录
- `screenSwitchDetector.js` - 切屏检测
- `monitor-dashboard.js` - 监考大屏
- `exam-monitor2.js` - 考试监控

## 部署建议

### Nginx配置示例

在生产环境中，通常使用Nginx作为反向代理来转发API请求到不同的微服务：

```nginx
# API请求转发配置
location /api/exam/ {
    proxy_pass http://exam-service/api/exam/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}

location /api/identity/ {
    proxy_pass http://identity-service/api/identity/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

### 环境配置

#### 开发环境
- 保持 `ApiBaseUrl` 为空
- 使用Web应用的代理中间件转发请求
- 便于本地调试和开发

#### 测试环境
```json
{
  "SiteSettings": {
    "ApiBaseUrl": "https://test-api.example.com"
  }
}
```

#### 生产环境
```json
{
  "SiteSettings": {
    "ApiBaseUrl": "https://api.example.com"
  }
}
```

## 优势

### 性能提升
- 减少代理转发开销
- 直连API服务，降低延迟
- 减轻Web应用服务器压力

### 架构优化
- 前后端分离更彻底
- 支持CDN加速静态资源
- 便于微服务架构部署

### 运维友好
- 配置简单，一个开关控制
- 支持不同环境灵活配置
- 便于负载均衡和故障转移

## 注意事项

1. **CORS配置**: 确保API服务正确配置了CORS策略
2. **认证传递**: 系统会自动传递认证Token和租户信息
3. **错误处理**: 保持与代理模式相同的错误处理逻辑
4. **监控**: 建议在生产环境中监控API请求的成功率和响应时间

## 故障排除

### 常见问题

1. **API请求失败**
   - 检查 `ApiBaseUrl` 配置是否正确
   - 确认API服务是否可访问
   - 检查网络连接和防火墙设置

2. **认证失败**
   - 确认Token管理器正常工作
   - 检查API服务的认证配置
   - 验证租户信息传递是否正确

3. **CORS错误**
   - 配置API服务的CORS策略
   - 允许前端域名的跨域请求
   - 检查预检请求的处理

### 调试方法

1. 打开浏览器开发者工具
2. 查看Network面板中的API请求
3. 检查请求URL是否正确转换
4. 查看响应状态码和错误信息
5. 检查Console中的JavaScript错误

通过合理配置API地址，可以显著提升考试系统在生产环境中的性能和用户体验。
