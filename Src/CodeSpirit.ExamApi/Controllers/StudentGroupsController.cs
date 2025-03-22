using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.StudentGroup;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考生组管理控制器
/// </summary>
[DisplayName("考生组管理")]
[Navigation(Icon = "fa-solid fa-users-rectangle")]
public class StudentGroupsController : ApiControllerBase
{
    private readonly IStudentGroupService _studentGroupService;
    private readonly ILogger<StudentGroupsController> _logger;

    /// <summary>
    /// 初始化考生组管理控制器
    /// </summary>
    /// <param name="studentGroupService">考生组服务</param>
    /// <param name="logger">日志记录器</param>
    public StudentGroupsController(
        IStudentGroupService studentGroupService,
        ILogger<StudentGroupsController> logger)
    {
        ArgumentNullException.ThrowIfNull(studentGroupService);
        ArgumentNullException.ThrowIfNull(logger);

        _studentGroupService = studentGroupService;
        _logger = logger;
    }

    /// <summary>
    /// 获取考生组列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>考生组列表分页结果</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<StudentGroupDto>>>> GetStudentGroups([FromQuery] StudentGroupQueryDto queryDto)
    {
        Expression<Func<StudentGroup, bool>> predicate = null;
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            predicate = x => x.Name.Contains(queryDto.Keywords) || x.Description.Contains(queryDto.Keywords);
        }

        PageList<StudentGroupDto> groups = await _studentGroupService.GetPagedListAsync(queryDto, predicate);
        return SuccessResponse(groups);
    }

    /// <summary>
    /// 获取考生组详情
    /// </summary>
    /// <param name="id">考生组ID</param>
    /// <returns>考生组详细信息</returns>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<StudentGroupDto>>> GetStudentGroup(long id)
    {
        StudentGroupDto group = await _studentGroupService.GetAsync(id);
        return SuccessResponse(group);
    }

    /// <summary>
    /// 创建考生组
    /// </summary>
    /// <param name="createDto">创建考生组请求数据</param>
    /// <returns>创建的考生组信息</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudentGroupDto>>> CreateStudentGroup(CreateStudentGroupDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        StudentGroupDto groupDto = await _studentGroupService.CreateAsync(createDto);
        return SuccessResponse(groupDto);
    }

    /// <summary>
    /// 更新考生组
    /// </summary>
    /// <param name="id">考生组ID</param>
    /// <param name="updateDto">更新考生组请求数据</param>
    /// <returns>更新操作结果</returns>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse>> UpdateStudentGroup(long id, UpdateStudentGroupDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);
        await _studentGroupService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除考生组
    /// </summary>
    /// <param name="id">考生组ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [Operation("删除", "ajax", null, "确定要删除此考生组吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteStudentGroup(long id)
    {
        await _studentGroupService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量删除考生组
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的考生组吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        (int successCount, List<long> failedIds) = await _studentGroupService.BatchDeleteAsync(request.Ids);

        return failedIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个考生组，但以下考生组删除失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功删除 {successCount} 个考生组！");
    }

    /// <summary>
    /// 批量导入考生组
    /// </summary>
    /// <param name="importDto">导入数据</param>
    /// <returns>导入结果</returns>
    [HttpPost("batch/import")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<StudentGroupBatchImportDto> importDto)
    {
        ArgumentNullException.ThrowIfNull(importDto);

        (int successCount, List<string> failedItems) = await _studentGroupService.BatchImportAsync(importDto.ImportData);

        return failedItems.Any()
            ? SuccessResponse($"成功导入 {successCount} 个考生组，但以下考生组导入失败: {string.Join(", ", failedItems)}")
            : SuccessResponse($"成功导入 {successCount} 个考生组！");
    }

    /// <summary>
    /// 添加考生到分组
    /// </summary>
    /// <param name="id">考生组ID</param>
    /// <param name="request">考生ID列表</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id:long}/students")]
    public async Task<ActionResult<ApiResponse>> AddStudentsToGroup(long id, [FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _studentGroupService.AddStudentsToGroupAsync(id, request.Ids);
        return SuccessResponse("成功添加考生到分组！");
    }

    /// <summary>
    /// 从分组移除考生
    /// </summary>
    /// <param name="id">考生组ID</param>
    /// <param name="request">考生ID列表</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}/students")]
    public async Task<ActionResult<ApiResponse>> RemoveStudentsFromGroup(long id, [FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _studentGroupService.RemoveStudentsFromGroupAsync(id, request.Ids);
        return SuccessResponse("成功从分组移除考生！");
    }
} 