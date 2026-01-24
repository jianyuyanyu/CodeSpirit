using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.{Service}Api.Dtos.{EntityName};
using CodeSpirit.{Service}Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.{Service}Api.Controllers;

/// <summary>
/// {EntityName} 管理控制器
/// </summary>
[DisplayName("{EntityName}管理")]
[Navigation(Icon = "fa-solid fa-{icon}", PlatformType = PlatformType.Tenant)]
public class {EntityName}sController : ApiControllerBase
{
    private readonly I{EntityName}Service _{entityName}Service;
    
    public {EntityName}sController(I{EntityName}Service {entityName}Service)
    {
        _{entityName}Service = {entityName}Service;
    }
    
    /// <summary>
    /// 获取 {EntityName} 列表
    /// </summary>
    [HttpGet]
    [DisplayName("获取{EntityName}列表")]
    public async Task<ActionResult<ApiResponse<PageList<{EntityName}Dto>>>> Get{EntityName}s(
        [FromQuery] {EntityName}QueryDto queryDto)
    {
        var {entityName}s = await _{entityName}Service.Get{EntityName}sAsync(queryDto);
        return SuccessResponse({entityName}s);
    }
    
    /// <summary>
    /// 获取 {EntityName} 详情
    /// </summary>
    [HttpGet("{id}")]
    [DisplayName("获取{EntityName}详情")]
    public async Task<ActionResult<ApiResponse<{EntityName}Dto>>> Detail(long id)
    {
        var {entityName} = await _{entityName}Service.GetAsync(id);
        return SuccessResponse({entityName});
    }
    
    /// <summary>
    /// 创建 {EntityName}
    /// </summary>
    [HttpPost]
    [DisplayName("创建{EntityName}")]
    public async Task<ActionResult<ApiResponse<{EntityName}Dto>>> Create{EntityName}(
        Create{EntityName}Dto createDto)
    {
        var {entityName} = await _{entityName}Service.CreateAsync(createDto);
        return SuccessResponseWithCreate<{EntityName}Dto>(nameof(Detail), {entityName});
    }
    
    /// <summary>
    /// 更新 {EntityName}
    /// </summary>
    [HttpPut("{id}")]
    [DisplayName("更新{EntityName}")]
    public async Task<ActionResult<ApiResponse>> Update{EntityName}(
        long id, Update{EntityName}Dto updateDto)
    {
        await _{entityName}Service.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }
    
    /// <summary>
    /// 删除 {EntityName}
    /// </summary>
    [HttpDelete("{id}")]
    [DisplayName("删除{EntityName}")]
    [Operation("删除", "ajax", null, "确定要删除此{EntityName}吗？")]
    public async Task<ActionResult<ApiResponse>> Delete{EntityName}(long id)
    {
        await _{entityName}Service.DeleteAsync(id);
        return SuccessResponse();
    }
    
    /// <summary>
    /// 批量删除 {EntityName}
    /// </summary>
    [HttpPost("batch/delete")]
    [DisplayName("批量删除{EntityName}")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的{EntityName}吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDelete(
        [FromBody] BatchOperationDto<long> request)
    {
        var (successCount, failedIds) = await _{entityName}Service.BatchDeleteAsync(request.Ids);
        return SuccessResponse($"成功删除 {successCount} 个{EntityName}！");
    }
}
