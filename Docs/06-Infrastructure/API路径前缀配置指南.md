# API路径前缀配置指南

## 概述

CodeSpirit框架支持为各API服务配置路径前缀，以适配不同的负载转发和API网关场景。通过路径前缀功能，您可以将API服务从默认的 `/api/exam/` 路径扩展为支持 `/exam/api/exam/` 等自定义前缀路径。

## 核心特性

- **零运行时开销**：通过路由约定在应用启动时修改路由表，无需中间件处理
- **配置驱动**：支持配置文件和环境变量两种配置方式
- **灵活配置**：支持为每个API服务单独配置前缀
- **健康检查支持**：可选择是否对健康检查端点应用前缀
- **向后兼容**：默认关闭，不影响现有部署

## 配置方式

### 1. 配置文件方式

在各API服务的 `appsettings.json` 中添加 `PathPrefix` 配置节：

```json
{
  "PathPrefix": {
    "Enabled": true,
    "Prefix": "exam",
    "ApplyToHealthChecks": false
  }
}
```

### 2. 环境变量方式

通过环境变量配置（优先级高于配置文件）：

```bash
# 启用路径前缀
PATHPREFIX__ENABLED=true

# 设置前缀
PATHPREFIX__PREFIX=exam

# 是否对健康检查应用前缀
PATHPREFIX__APPLYTOHEALTHCHECKS=false
```

## 配置参数说明

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | bool | false | 是否启用路径前缀功能 |
| `Prefix` | string | null | 路径前缀字符串，只能包含字母、数字、连字符和下划线 |
| `ApplyToHealthChecks` | bool | false | 是否对健康检查端点应用前缀 |

## 使用示例

### ExamAPI服务

**配置前：**
- `/api/exam/settings`
- `/api/exam/questions`
- `/api/exam/papers`

**启用前缀 "exam" 后：**
- `/exam/api/exam/settings`
- `/exam/api/exam/questions`
- `/exam/api/exam/papers`

### 配置示例

```json
{
  "PathPrefix": {
    "Enabled": true,
    "Prefix": "exam"
  }
}
```

### 负载均衡器配置示例

#### Nginx配置

```nginx
# 转发到ExamAPI服务
location /exam/ {
    proxy_pass http://exam-api-backend/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
}

# 转发到IdentityAPI服务
location /identity/ {
    proxy_pass http://identity-api-backend/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
}
```

#### Traefik配置

```yaml
http:
  routers:
    exam-api:
      rule: "PathPrefix(`/exam/`)"
      service: exam-api-service
      
    identity-api:
      rule: "PathPrefix(`/identity/`)"
      service: identity-api-service
      
  services:
    exam-api-service:
      loadBalancer:
        servers:
          - url: "http://exam-api:8080"
          
    identity-api-service:
      loadBalancer:
        servers:
          - url: "http://identity-api:8080"
```

## 各API服务默认前缀建议

| 服务 | 建议前缀 | 说明 |
|------|----------|------|
| ExamAPI | `exam` | 考试系统相关接口 |
| IdentityAPI | `identity` | 身份认证相关接口 |
| MessagingAPI | `messaging` | 消息服务相关接口 |
| FileStorageAPI | `file` | 文件存储相关接口 |
| SurveyAPI | `survey` | 问卷调查相关接口 |
| ConfigCenter | `config` | 配置中心相关接口 |

## 注意事项

1. **前缀格式**：前缀字符串不应包含开头和结尾的斜杠，框架会自动处理
2. **字符限制**：前缀只能包含字母、数字、连字符和下划线，长度限制为1-50个字符
3. **健康检查**：默认情况下不对健康检查端点应用前缀，以便负载均衡器正常访问
4. **环境变量优先级**：环境变量配置优先级高于配置文件，便于容器化部署
5. **配置验证**：启用前缀功能时必须提供有效的前缀字符串，否则应用启动会失败

## 容器化部署

在Docker Compose或Kubernetes中使用环境变量：

### Docker Compose

```yaml
services:
  exam-api:
    image: codespirit/exam-api:latest
    environment:
      - PATHPREFIX__ENABLED=true
      - PATHPREFIX__PREFIX=exam
    ports:
      - "8080:8080"
      
  identity-api:
    image: codespirit/identity-api:latest
    environment:
      - PATHPREFIX__ENABLED=true
      - PATHPREFIX__PREFIX=identity
    ports:
      - "8081:8080"
```

### Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: exam-api
spec:
  template:
    spec:
      containers:
      - name: exam-api
        image: codespirit/exam-api:latest
        env:
        - name: PATHPREFIX__ENABLED
          value: "true"
        - name: PATHPREFIX__PREFIX
          value: "exam"
```

## 故障排除

### 常见问题

1. **路由不生效**
   - 检查 `Enabled` 是否设置为 `true`
   - 验证 `Prefix` 配置是否符合格式要求
   - 确认API服务配置类继承自 `BaseApiConfiguration` 并调用了 `base.ConfigureServices()`

2. **健康检查失败**
   - 如果负载均衡器无法访问健康检查端点，请确保 `ApplyToHealthChecks` 设置为 `false`

3. **配置验证失败**
   - 检查前缀字符串是否包含非法字符
   - 确认前缀长度在1-50个字符范围内

### 调试建议

1. 启用详细日志以查看路由配置过程
2. 使用开发工具检查实际生成的路由表
3. 验证负载均衡器配置与API服务前缀配置的一致性

## 性能影响

- **启动时间**：路由约定在应用启动时执行，对启动时间影响极小
- **运行时性能**：零运行时开销，路由在启动时已确定
- **内存使用**：配置对象占用内存极少，可忽略不计
