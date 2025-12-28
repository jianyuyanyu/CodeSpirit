# NoAuditAttribute 使用指南

## 概述

`NoAuditAttribute` 是 CodeSpirit.Audit 组件提供的一个特性，用于禁用特定控制器或方法的审计功能。当您需要跳过某些不重要或频繁调用的接口的审计记录时，可以使用此特性。

## 功能特点

- **控制器级别禁用**：可以在整个控制器上应用，禁用该控制器所有方法的审计
- **方法级别禁用**：可以在特定方法上应用，仅禁用该方法的审计
- **优先级机制**：方法级别的 `NoAuditAttribute` 优先于控制器级别
- **原因记录**：支持添加禁用审计的原因说明，便于维护和调试

## 使用方法

### 1. 控制器级别禁用审计

```csharp
using CodeSpirit.Audit.Attributes;

[NoAudit("考试客户端接口频繁调用，不需要记录审计日志")]
public class IndexController : ApiControllerBase
{
    // 该控制器的所有方法都不会记录审计日志
    
    public async Task<ActionResult<ApiResponse>> GetData()
    {
        // 此方法不会记录审计日志
        return SuccessResponse();
    }
}
```

### 2. 方法级别禁用审计

```csharp
public class UsersController : ApiControllerBase
{
    // 正常方法，会记录审计日志
    public async Task<ActionResult<ApiResponse>> GetUsers()
    {
        return SuccessResponse();
    }
    
    // 禁用审计的方法
    [NoAudit("健康检查接口，无需审计")]
    public async Task<ActionResult<ApiResponse>> HealthCheck()
    {
        // 此方法不会记录审计日志
        return SuccessResponse();
    }
}
```

### 3. 不带原因的简单用法

```csharp
[NoAudit] // 不提供原因
public class HealthController : ApiControllerBase
{
    // ...
}
```

## 工作原理

1. **中间件检查**：审计中间件在处理请求时会检查控制器和方法上的 `NoAuditAttribute`
2. **优先级处理**：如果方法上有 `NoAuditAttribute`，则优先使用方法级别的设置
3. **跳过审计**：如果发现 `NoAuditAttribute`，则完全跳过该请求的审计记录
4. **日志记录**：会在调试日志中记录跳过审计的原因（如果提供了原因）

## 适用场景

### 推荐使用场景

- **健康检查接口**：如 `/health`、`/ping` 等系统监控接口
- **高频调用接口**：如实时数据获取、心跳检测等
- **静态资源接口**：如文件下载、图片获取等
- **客户端频繁轮询接口**：如考试系统的客户端接口

### 不推荐使用场景

- **重要业务操作**：如用户登录、数据修改、权限变更等
- **敏感数据操作**：如密码重置、支付相关操作等
- **需要合规审计的操作**：根据业务需求确定

## 注意事项

1. **谨慎使用**：审计日志对于系统安全和问题排查很重要，请谨慎决定哪些接口需要禁用审计
2. **文档记录**：建议在使用 `NoAuditAttribute` 时提供清晰的原因说明
3. **定期审查**：定期审查使用了 `NoAuditAttribute` 的接口，确保仍然适用
4. **测试验证**：在测试环境中验证禁用审计的效果

## 示例：考试系统客户端控制器

```csharp
/// <summary>
/// 考试客户端接口
/// </summary>
[Authorize]
[DisplayName("考试客户端")]
[Route("api/exam/client")]
[NoAudit("考试客户端接口频繁调用，不需要记录审计日志")]
public class IndexController : ApiControllerBase
{
    /// <summary>
    /// 获取可参加的考试列表
    /// </summary>
    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<List<ClientExamDto>>>> GetAvailableExams()
    {
        // 此方法不会记录审计日志
        var result = await _clientService.GetAvailableExamsAsync(currentUserId);
        return SuccessResponse(result);
    }
}
```

这样配置后，整个 `IndexController` 的所有方法都不会记录审计日志，从而减少日志存储压力并提高系统性能。
