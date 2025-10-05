using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Dtos.Department;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Controllers;

/// <summary>
/// 部门管理控制器
/// </summary>
[DisplayName("部门管理")]
[Navigation(Icon = "fa-solid fa-sitemap", PlatformType = PlatformType.Tenant)]
[Audit(EntityName = nameof(Department), LogRequestParams = true, LogResponseData = true)] // 👈 添加审计特性，设置默认行为
public class DepartmentsController : ApiControllerBase
{
    private readonly IDepartmentService _departmentService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    /// <summary>
    /// 获取部门列表（优化为非分页模式，支持树形展示）
    /// </summary>
    [HttpGet]
    [DisplayName("获取部门列表")]
    public async Task<ActionResult<ApiResponse<PageList<DepartmentDto>>>> GetDepartments([FromQuery] DepartmentQueryDto queryDto)
    {
        // 调用服务层方法获取树形结构的部门数据
        List<DepartmentDto> treeDepartments = await _departmentService.GetDepartmentsWithTreeAsync(queryDto);

        // 创建非分页的PageList，这样Amis会自动使用树形展示
        PageList<DepartmentDto> listData = new(treeDepartments, treeDepartments.Count);

        return SuccessResponse(listData);
    }

    /// <summary>
    /// 获取部门树形结构
    /// </summary>
    [HttpGet("tree")]
    [DisplayName("获取部门树形结构")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetDepartmentTree([FromQuery] long? parentId = null)
    {
        var tree = await _departmentService.GetDepartmentTreeAsync(parentId);
        return SuccessResponse(tree);
    }

    /// <summary>
    /// 获取部门详情
    /// </summary>
    [HttpGet("{id}")]
    [DisplayName("获取部门详情")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Detail(long id)
    {
        var department = await _departmentService.GetAsync(id);
        return SuccessResponse(department);
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    [HttpPost]
    [DisplayName("创建部门")]
    [Audit("创建新部门", AuditOperationType.Create)]  // 👈 添加明确的审计特性
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment(CreateDepartmentDto createDto)
    {
        var department = await _departmentService.CreateAsync(createDto);
        return SuccessResponseWithCreate<DepartmentDto>(nameof(Detail), department);
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    [HttpPut("{id}")]
    [DisplayName("更新部门")]
    [Audit("更新部门信息", AuditOperationType.Update)]  // 👈 添加明确的审计特性
    public async Task<ActionResult<ApiResponse>> UpdateDepartment(long id, UpdateDepartmentDto updateDto)
    {
        await _departmentService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    [HttpDelete("{id}")]
    [DisplayName("删除部门")]
    public async Task<ActionResult<ApiResponse>> DeleteDepartment(long id)
    {
        await _departmentService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 设置部门激活状态
    /// </summary>
    [HttpPut("{id}/setActive")]
    [DisplayName("设置部门激活状态")]
    public async Task<ActionResult<ApiResponse>> SetActiveStatus(long id, [FromQuery] bool isActive)
    {
        await _departmentService.SetActiveStatusAsync(id, isActive);
        string status = isActive ? "激活" : "禁用";
        return SuccessResponse($"部门已{status}成功！");
    }

    /// <summary>
    /// 获取部门的所有子部门
    /// </summary>
    [HttpGet("{id}/children")]
    [DisplayName("获取子部门")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetChildDepartments(long id)
    {
        var children = await _departmentService.GetChildDepartmentsAsync(id);
        return SuccessResponse(children);
    }

    /// <summary>
    /// 导出部门列表
    /// </summary>
    [HttpGet("Export")]
    [DisplayName("导出部门列表")]
    public async Task<ActionResult<ApiResponse<PageList<DepartmentDto>>>> Export([FromQuery] DepartmentQueryDto queryDto)
    {
        const int MaxExportLimit = 10000;
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        var departments = await _departmentService.GetDepartmentsAsync(queryDto);

        return departments.Items.Count == 0
            ? BadResponse<PageList<DepartmentDto>>("没有数据可供导出")
            : SuccessResponse(departments);
    }

    /// <summary>
    /// 批量导入部门
    /// </summary>
    [HttpPost("batch/import")]
    [DisplayName("批量导入部门")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<DepartmentBatchImportItemDto> importDto)
    {
        var (successCount, failedIds) = await _departmentService.BatchImportAsync(importDto.ImportData);
        return SuccessResponse($"成功批量导入了 {successCount} 个部门！");
    }

    /// <summary>
    /// 批量删除部门
    /// </summary>
    [HttpPost("batch/delete")]
    [DisplayName("批量删除部门")]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<long> request)
    {
        var (successCount, failedIds) = await _departmentService.BatchDeleteAsync(request.Ids);

        if (failedIds.Any())
        {
            string failedMessage = $"成功删除 {successCount} 个部门，但以下部门删除失败: {string.Join(", ", failedIds)}";
            return SuccessResponse(failedMessage);
        }

        return SuccessResponse($"成功删除 {successCount} 个部门！");
    }

    /// <summary>
    /// AI快速初始化组织结构
    /// </summary>
    [HttpPost("quick-init")]
    [HeaderOperation("快速创建", "form", null, null, Icon = "fa-solid fa-magic", DialogSize = DialogSize.XL)]
    [DisplayName("快速初始化组织结构")]
    public async Task<ActionResult<ApiResponse>> QuickInitOrganization([FromBody] GenerateOrganizationStructureDto request)
    {
        if (request.Departments == null || !request.Departments.Any())
        {
            return BadResponse("AI生成的部门列表为空，请重新生成");
        }

        var createdCount = await _departmentService.CreateOrganizationStructureAsync(request.Departments);
        return SuccessResponse($"成功创建组织结构，共 {createdCount} 个部门！");
    }
}

