using System.ComponentModel;
using CodeSpirit.Audit.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;

namespace CodeSpirit.Audit.Tests.Examples;

/// <summary>
/// 用户管理控制器审计使用示例
/// </summary>
[DisplayName("用户管理")]
[Navigation(Icon = "fa-solid fa-users", PlatformType = PlatformType.Both)]
[Audit("用户管理模块")]
[ApiController]
[Route("api/[controller]")]
public class UsersControllerWithAudit : ControllerBase
{
    private readonly ILogger<UsersControllerWithAudit> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public UsersControllerWithAudit(ILogger<UsersControllerWithAudit> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// 获取用户列表
    /// </summary>
    [HttpGet]
    [Audit("查询用户列表", AuditOperationType.Query)]
    public async Task<IActionResult> GetUsers([FromQuery] UserQueryDto queryDto)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Items = new List<UserDto>(), Total = 0 });
    }
    
    /// <summary>
    /// 导出用户列表
    /// </summary>
    [HttpGet("Export")]
    [Audit("导出用户列表", AuditOperationType.Export)]
    [Operation("导出", "ajax")]
    public async Task<IActionResult> Export([FromQuery] UserQueryDto queryDto)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return File(new byte[0], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "users.xlsx");
    }
    
    /// <summary>
    /// 获取用户详情
    /// </summary>
    [HttpGet("{id}")]
    [Audit("查询用户详情", AuditOperationType.Query)]
    public async Task<IActionResult> Detail(long id)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new UserDto { Id = id, UserName = "user" + id });
    }
    
    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    [Audit("创建用户", AuditOperationType.Create)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new UserDto { Id = 1, UserName = createUserDto.UserName });
    }
    
    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id}")]
    [Audit("更新用户", AuditOperationType.Update)]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserDto updateUserDto)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "更新成功" });
    }
    
    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{id}")]
    [Audit("删除用户", AuditOperationType.Delete)]
    public async Task<IActionResult> DeleteUser(long id)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "删除成功" });
    }
    
    /// <summary>
    /// 分配角色
    /// </summary>
    [HttpPost("{id}/roles")]
    [Audit("分配角色", AuditOperationType.Authorize)]
    public async Task<IActionResult> AssignRoles(long id, [FromBody] List<string> roles)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "分配角色成功" });
    }
    
    /// <summary>
    /// 移除角色
    /// </summary>
    [HttpDelete("{id}/roles")]
    [Audit("移除角色", AuditOperationType.Authorize)]
    public async Task<IActionResult> RemoveRoles(long id, [FromBody] List<string> roles)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "移除角色成功" });
    }
    
    /// <summary>
    /// 设置激活状态
    /// </summary>
    [HttpPut("{id}/setActive")]
    [Audit("设置用户激活状态", AuditOperationType.Update)]
    public async Task<IActionResult> SetActiveStatus(long id, [FromQuery] bool isActive)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "设置激活状态成功" });
    }
    
    /// <summary>
    /// 重置随机密码
    /// </summary>
    [HttpPost("{id}/resetRandomPassword")]
    [Audit("重置用户密码", AuditOperationType.Update)]
    [Operation("重置密码", "ajax", null, "确定要重置密码吗？", "isActive == true")]
    public async Task<IActionResult> ResetRandomPassword(long id)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        string newPassword = "NewPassword123";
        return Ok(new { Success = true, Message = "重置密码成功", Password = newPassword });
    }
    
    /// <summary>
    /// 解锁用户
    /// </summary>
    [HttpPut("{id}/unlock")]
    [Audit("解锁用户", AuditOperationType.Update)]
    [Operation("解锁", "ajax", null, "确定要解除用户锁定吗？", "lockoutEnd != null")]
    public async Task<IActionResult> UnlockUser(long id)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "用户已成功解锁。" });
    }
    
    /// <summary>
    /// 快速保存用户
    /// </summary>
    [HttpPatch("quickSave")]
    [Audit("快速保存用户", AuditOperationType.Update)]
    public async Task<IActionResult> QuickSaveUsers([FromBody] QuickSaveRequestDto request)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "保存成功" });
    }
    
    /// <summary>
    /// 模拟登录
    /// </summary>
    [HttpPost("{id}/impersonate")]
    [Audit("模拟用户登录", AuditOperationType.Login)]
    [Operation("模拟登录", "ajax", null, "确定要模拟此用户登录吗？", "isActive == true", Redirect = "/impersonate?token=${token}")]
    public async Task<IActionResult> ImpersonateUser(long id)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "模拟登录成功", Token = "mock-token-" + id });
    }
    
    /// <summary>
    /// 批量导入用户
    /// </summary>
    [HttpPost("batch/import")]
    [Audit("批量导入用户", AuditOperationType.Import)]
    public async Task<IActionResult> BatchImport([FromBody] BatchImportRequestDto importDto)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "导入成功", SuccessCount = importDto.Items.Count, FailCount = 0 });
    }
    
    /// <summary>
    /// 批量删除用户
    /// </summary>
    [HttpPost("batch/delete")]
    [Audit("批量删除用户", AuditOperationType.Delete)]
    [Operation("批量删除", "ajax", null, "确定要批量删除?", isBulkOperation: true)]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteRequestDto request)
    {
        // 模拟业务逻辑
        await Task.Delay(100);
        return Ok(new { Success = true, Message = "批量删除成功", request.Ids.Count });
    }
}

// 模拟数据传输对象
public class UserQueryDto
{
    public string Keyword { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UserDto
{
    public long Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LockoutEnd { get; set; }
}

public class CreateUserDto
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UpdateUserDto
{
    public string Email { get; set; }
    public bool IsActive { get; set; }
}

public class QuickSaveRequestDto
{
    public string Field { get; set; }
    public string Value { get; set; }
    public List<long> Ids { get; set; }
}

public class BatchImportRequestDto
{
    public List<CreateUserDto> Items { get; set; }
}

public class BatchDeleteRequestDto
{
    public List<long> Ids { get; set; }
} 