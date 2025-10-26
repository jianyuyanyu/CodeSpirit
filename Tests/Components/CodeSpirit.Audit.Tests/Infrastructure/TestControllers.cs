using CodeSpirit.Audit.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Audit.Tests.Infrastructure;

/// <summary>
/// 测试数据传输对象
/// </summary>
public class TestDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int Value { get; set; }
    public string? Id { get; set; }
}

/// <summary>
/// 测试控制器 - 方法级别审计
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MethodLevelAuditController : ControllerBase
{
    /// <summary>
    /// 测试GET请求
    /// </summary>
    [HttpGet]
    [Audit("测试获取操作", AuditOperationType.Query)]
    public ActionResult<TestDto> Get()
    {
        return Ok(new TestDto
        {
            Name = "测试对象",
            Description = "这是一个测试对象",
            Value = 42
        });
    }

    /// <summary>
    /// 测试POST请求
    /// </summary>
    [HttpPost]
    [Audit("测试创建操作", AuditOperationType.Create)]
    public ActionResult<TestDto> Post([FromBody] TestDto dto)
    {
        return Ok(new TestDto
        {
            Name = $"已创建: {dto.Name}",
            Description = dto.Description,
            Value = dto.Value
        });
    }

    /// <summary>
    /// 测试PUT请求
    /// </summary>
    [HttpPut("{id}")]
    [Audit("测试更新操作", AuditOperationType.Update)]
    public ActionResult<TestDto> Put(int id, [FromBody] TestDto dto)
    {
        return Ok(new TestDto
        {
            Name = $"已更新ID={id}: {dto.Name}",
            Description = dto.Description,
            Value = dto.Value
        });
    }
}

/// <summary>
/// 测试控制器 - 控制器级别审计
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Audit("控制器级别审计", AuditOperationType.Action)]
public class ControllerLevelAuditController : ControllerBase
{
    /// <summary>
    /// 测试GET请求
    /// </summary>
    [HttpGet]
    public ActionResult<TestDto> Get()
    {
        return Ok(new TestDto
        {
            Name = "控制器级别审计对象",
            Description = "这是一个控制器级别审计测试对象",
            Value = 100
        });
    }
}

/// <summary>
/// 测试控制器 - 无审计
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NoAuditController : ControllerBase
{
    /// <summary>
    /// 测试GET请求
    /// </summary>
    [HttpGet]
    public ActionResult<TestDto> Get()
    {
        return Ok(new TestDto
        {
            Name = "无审计对象",
            Description = "这个对象不应该被审计",
            Value = 0
        });
    }
}

/// <summary>
/// 测试控制器 - 自定义审计配置
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomAuditController : ControllerBase
{
    /// <summary>
    /// 测试GET请求 - 自定义审计配置
    /// </summary>
    [HttpGet]
    [Audit("自定义审计配置-不记录响应", AuditOperationType.Query, LogResponseData = true)]
    public ActionResult<TestDto> Get()
    {
        return Ok(new TestDto
        {
            Name = "自定义审计对象",
            Description = "这个不应该被记录",
            Value = 999
        });
    }
}

/// <summary>
/// 用于测试的控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// 带有审计特性的GET方法
    /// </summary>
    [HttpGet("{id}")]
    [Audit("测试操作", AuditOperationType.Create)]
    public IActionResult GetWithAttribute(int id)
    {
        return Ok(new { Id = id, Name = "Test Item" });
    }

    /// <summary>
    /// 不带审计特性的GET方法
    /// </summary>
    [HttpGet("noaudit/{id}")]
    public IActionResult GetWithoutAttribute(int id)
    {
        return Ok(new { Id = id, Name = "Test Item" });
    }

    /// <summary>
    /// 带有审计特性的POST方法
    /// </summary>
    [HttpPost]
    [Audit("创建操作", AuditOperationType.Create)]
    public IActionResult Post([FromBody] TestDto model)
    {
        return Created($"/api/test/{model.Id}", model);
    }

    /// <summary>
    /// 带有审计特性的PUT方法
    /// </summary>
    [HttpPut("{id}")]
    [Audit("更新操作", AuditOperationType.Update)]
    public IActionResult Put(int id, [FromBody] TestDto model)
    {
        return Ok(model);
    }

    /// <summary>
    /// 带有审计特性的DELETE方法
    /// </summary>
    [HttpDelete("{id}")]
    [Audit("删除操作", AuditOperationType.Delete)]
    public IActionResult Delete(int id)
    {
        return NoContent();
    }
} 