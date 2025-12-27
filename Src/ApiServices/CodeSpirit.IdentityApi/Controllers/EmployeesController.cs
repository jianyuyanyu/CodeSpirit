using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Dtos.Employee;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Navigation.Resources;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers;

/// <summary>
/// 职工管理控制器
/// </summary>
[DisplayName("职工管理")]
[Navigation(Icon = "fa-solid fa-user-tie", PlatformType = PlatformType.Tenant, TitleResourceKey = "Controller.Employees", TitleResourceType = typeof(NavigationResources))]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employeeService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// 获取职工列表
    /// </summary>
    [HttpGet]
    [DisplayName("获取职工列表")]
    public async Task<ActionResult<ApiResponse<PageList<EmployeeDto>>>> GetEmployees([FromQuery] EmployeeQueryDto queryDto)
    {
        var employees = await _employeeService.GetEmployeesAsync(queryDto);
        return SuccessResponse(employees);
    }

    /// <summary>
    /// 根据部门获取职工列表
    /// </summary>
    [HttpGet("department/{departmentId}")]
    [DisplayName("根据部门获取职工")]
    public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetEmployeesByDepartment(
        long departmentId, 
        [FromQuery] bool includeSubDepartments = false)
    {
        var employees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId, includeSubDepartments);
        return SuccessResponse(employees);
    }

    /// <summary>
    /// 获取职工详情
    /// </summary>
    [HttpGet("{id}")]
    [DisplayName("获取职工详情")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Detail(long id)
    {
        var employee = await _employeeService.GetAsync(id);
        return SuccessResponse(employee);
    }

    /// <summary>
    /// 创建职工
    /// </summary>
    [HttpPost]
    [DisplayName("创建职工")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee(CreateEmployeeDto createDto)
    {
        var employee = await _employeeService.CreateAsync(createDto);
        return SuccessResponseWithCreate<EmployeeDto>(nameof(Detail), employee);
    }

    /// <summary>
    /// 更新职工
    /// </summary>
    [HttpPut("{id}")]
    [DisplayName("更新职工")]
    public async Task<ActionResult<ApiResponse>> UpdateEmployee(long id, UpdateEmployeeDto updateDto)
    {
        await _employeeService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除职工
    /// </summary>
    [HttpDelete("{id}")]
    [DisplayName("删除职工")]
    public async Task<ActionResult<ApiResponse>> DeleteEmployee(long id)
    {
        await _employeeService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 设置职工激活状态
    /// </summary>
    [HttpPut("{id}/setActive")]
    [DisplayName("设置职工激活状态")]
    public async Task<ActionResult<ApiResponse>> SetActiveStatus(long id, [FromQuery] bool isActive)
    {
        await _employeeService.SetActiveStatusAsync(id, isActive);
        string status = isActive ? "激活" : "禁用";
        return SuccessResponse($"职工已{status}成功！");
    }

    /// <summary>
    /// 转移职工到新部门
    /// </summary>
    [HttpPut("{id}/transfer")]
    [DisplayName("转移职工")]
    public async Task<ActionResult<ApiResponse>> TransferEmployee(long id, [FromQuery] long? newDepartmentId)
    {
        await _employeeService.TransferEmployeeAsync(id, newDepartmentId);
        return SuccessResponse("职工转移成功！");
    }

    /// <summary>
    /// 办理职工离职
    /// </summary>
    [HttpPost("{id}/terminate")]
    [DisplayName("办理离职")]
    public async Task<ActionResult<ApiResponse>> TerminateEmployee(long id, [FromBody] DateTime terminationDate)
    {
        await _employeeService.TerminateEmployeeAsync(id, terminationDate);
        return SuccessResponse("离职办理成功！");
    }

    /// <summary>
    /// 关联职工与用户
    /// </summary>
    [HttpPost("{id}/linkUser")]
    [DisplayName("关联用户")]
    public async Task<ActionResult<ApiResponse>> LinkUser(long id, [FromQuery] long userId)
    {
        await _employeeService.LinkEmployeeToUserAsync(id, userId);
        return SuccessResponse("用户关联成功！");
    }

    /// <summary>
    /// 取消职工与用户的关联
    /// </summary>
    [HttpPost("{id}/unlinkUser")]
    [DisplayName("取消关联用户")]
    public async Task<ActionResult<ApiResponse>> UnlinkUser(long id)
    {
        await _employeeService.UnlinkEmployeeFromUserAsync(id);
        return SuccessResponse("用户关联已取消！");
    }

    /// <summary>
    /// 导出职工列表
    /// </summary>
    [HttpGet("Export")]
    [DisplayName("导出职工列表")]
    public async Task<ActionResult<ApiResponse<PageList<EmployeeDto>>>> Export([FromQuery] EmployeeQueryDto queryDto)
    {
        const int MaxExportLimit = 10000;
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        var employees = await _employeeService.GetEmployeesAsync(queryDto);

        return employees.Items.Count == 0 
            ? BadResponse<PageList<EmployeeDto>>("没有数据可供导出") 
            : SuccessResponse(employees);
    }

    /// <summary>
    /// 批量导入职工
    /// </summary>
    [HttpPost("batch/import")]
    [DisplayName("批量导入职工")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<EmployeeBatchImportItemDto> importDto)
    {
        var (successCount, failedIds) = await _employeeService.BatchImportAsync(importDto.ImportData);
        return SuccessResponse($"成功批量导入了 {successCount} 个职工！");
    }

    /// <summary>
    /// 批量删除职工
    /// </summary>
    [HttpPost("batch/delete")]
    [DisplayName("批量删除职工")]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<long> request)
    {
        var (successCount, failedIds) = await _employeeService.BatchDeleteAsync(request.Ids);

        if (failedIds.Any())
        {
            string failedMessage = $"成功删除 {successCount} 个职工，但以下职工删除失败: {string.Join(", ", failedIds)}";
            return SuccessResponse(failedMessage);
        }

        return SuccessResponse($"成功删除 {successCount} 个职工！");
    }
}

