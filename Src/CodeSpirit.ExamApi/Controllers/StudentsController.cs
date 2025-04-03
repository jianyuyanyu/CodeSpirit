using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考生管理控制器
/// </summary>
[DisplayName("考生管理")]
[Navigation(Icon = "fa-solid fa-user-graduate")]
public class StudentsController : ApiControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentsController> _logger;
    
    /// <summary>
    /// 初始化考生管理控制器
    /// </summary>
    /// <param name="studentService">考生服务</param>
    /// <param name="logger">日志记录器</param>
    public StudentsController(
        IStudentService studentService,
        ILogger<StudentsController> logger)
    {
        ArgumentNullException.ThrowIfNull(studentService);
        ArgumentNullException.ThrowIfNull(logger);
        
        _studentService = studentService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取考生列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>考生列表分页结果</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<StudentDto>>>> GetStudents([FromQuery] StudentQueryDto queryDto)
    {
        var result = await _studentService.GetStudentsAsync(queryDto);
        return SuccessResponse(result);
    }
    
    /// <summary>
    /// 导出考生列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>导出的考生列表</returns>
    [HttpGet("Export")]
    public async Task<ActionResult<ApiResponse<PageList<StudentDto>>>> Export([FromQuery] StudentQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000; // 最大导出数量限制
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;
        
        // 获取考生数据
        var result = await _studentService.GetStudentsAsync(queryDto);
        
        // 如果数据为空则返回错误信息
        return result.Items.Count == 0 
            ? BadResponse<PageList<StudentDto>>("没有数据可供导出") 
            : SuccessResponse(result);
    }
    
    /// <summary>
    /// 获取考生详情
    /// </summary>
    /// <param name="id">考生ID</param>
    /// <returns>考生详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudent(long id)
    {
        if (id <= 0) return BadRequest("无效的ID");
        
        var result = await _studentService.GetAsync(id);
        return SuccessResponse(result);
    }
    
    /// <summary>
    /// 创建考生
    /// </summary>
    /// <param name="createDto">创建考生信息</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudentDto>>> CreateStudent([FromBody] CreateStudentDto createDto)
    {
        var result = await _studentService.CreateAsync(createDto);
        return SuccessResponse(result);
    }
    
    /// <summary>
    /// 更新考生信息
    /// </summary>
    /// <param name="id">考生ID</param>
    /// <param name="updateDto">更新信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateStudent(long id, [FromBody] UpdateStudentDto updateDto)
    {
        await _studentService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }
    
    /// <summary>
    /// 删除考生
    /// </summary>
    /// <param name="id">考生ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此考生吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteStudent(long id)
    {
        await _studentService.DeleteAsync(id);
        return SuccessResponse();
    }
    
    /// <summary>
    /// 批量导入考生
    /// </summary>
    /// <param name="importDto">考生信息导入数据</param>
    /// <returns>导入结果</returns>
    [HttpPost("batch/import")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<StudentBatchImportDto> importDto)
    {
        ArgumentNullException.ThrowIfNull(importDto);
        
        var result = await _studentService.BatchImportAsync(importDto.ImportData);
        return result.failedIds.Any()
            ? SuccessResponse($"成功导入 {result.successCount} 个考生，但以下考生导入失败: {string.Join(", ", result.failedIds)}")
            : SuccessResponse($"成功导入 {result.successCount} 个考生！");
    }
    
    /// <summary>
    /// 批量删除考生
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的考生吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        (int successCount, List<long> failedIds) = await _studentService.BatchDeleteAsync(request.Ids);
        
        return failedIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个考生，但以下考生删除失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功删除 {successCount} 个考生！");
    }
    
    
    /// <summary>
    /// 通过学号查询考生
    /// </summary>
    /// <param name="studentNumber">学号</param>
    /// <returns>考生信息</returns>
    [HttpGet("by-student-number/{studentNumber}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetByStudentNumber(string studentNumber)
    {
        if (string.IsNullOrEmpty(studentNumber)) return BadRequest("学号不能为空");
        
        var result = await _studentService.GetByStudentNumberAsync(studentNumber);
        return SuccessResponse(result);
    }
    
    /// <summary>
    /// 通过用户ID查询考生
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>考生信息</returns>
    [HttpGet("by-user-id/{userId}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetByUserId(long userId)
    {
        var result = await _studentService.GetByUserIdAsync(userId);
        return SuccessResponse(result);
    }
    
    /// <summary>
    /// 批量分配考生到考生组
    /// </summary>
    /// <param name="request">批量分配请求数据</param>
    /// <returns>分配结果</returns>
    [HttpPost("batch/assign-groups")]
    [Operation("批量分配考生组", "form", null, "确定要将选中的考生分配到指定考生组吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchAssignGroups([FromBody] BatchAssignGroupsDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        (int successCount, List<long> failedIds) = await _studentService.BatchAssignGroupsAsync(request.Ids, request.GroupIds);
        
        return failedIds.Any()
            ? SuccessResponse($"成功分配 {successCount} 个考生到考生组，但以下考生分配失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功分配 {successCount} 个考生到考生组！");
    }
} 