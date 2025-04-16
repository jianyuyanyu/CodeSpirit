using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.ExamApi.Dtos.PracticeRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CodeSpirit.Shared.Dtos.Common;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 练习记录管理控制器
/// </summary>
[DisplayName("练习记录管理")]
[Navigation(Icon = "fa-solid fa-clipboard-check")]
public class PracticeRecordsController : ApiControllerBase
{
    private readonly IPracticeRecordService _practiceRecordService;
    private readonly ILogger<PracticeRecordsController> _logger;

    /// <summary>
    /// 初始化练习记录管理控制器
    /// </summary>
    /// <param name="practiceRecordService">练习记录服务</param>
    /// <param name="logger">日志记录器</param>
    public PracticeRecordsController(
        IPracticeRecordService practiceRecordService,
        ILogger<PracticeRecordsController> logger)
    {
        ArgumentNullException.ThrowIfNull(practiceRecordService);
        ArgumentNullException.ThrowIfNull(logger);

        _practiceRecordService = practiceRecordService;
        _logger = logger;
    }

    /// <summary>
    /// 获取练习记录列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>练习记录列表分页结果</returns>
    [HttpGet]
    [DisplayName("获取练习记录列表")]
    public async Task<ActionResult<ApiResponse<PageList<PracticeRecordDto>>>> GetPracticeRecords([FromQuery] PracticeRecordQueryDto queryDto)
    {
        PageList<PracticeRecordDto> practiceRecords = await _practiceRecordService.GetPracticeRecordsAsync(queryDto);
        return SuccessResponse(practiceRecords);
    }

    /// <summary>
    /// 获取练习记录详情
    /// </summary>
    /// <param name="id">练习记录ID</param>
    /// <returns>练习记录详细信息</returns>
    [HttpGet("{id}")]
    [DisplayName("获取练习记录详情")]
    public async Task<ActionResult<ApiResponse<PracticeRecordDto>>> GetPracticeRecord(long id)
    {
        PracticeRecordDto practiceRecord = await _practiceRecordService.GetPracticeRecordAsync(id);
        return SuccessResponse(practiceRecord);
    }

    /// <summary>
    /// 创建练习记录
    /// </summary>
    /// <param name="createDto">创建练习记录DTO</param>
    /// <returns>创建成功的练习记录</returns>
    [HttpPost]
    [DisplayName("创建练习记录")]
    public async Task<ActionResult<ApiResponse<PracticeRecordDto>>> CreatePracticeRecord([FromBody] CreatePracticeRecordDto createDto)
    {
        PracticeRecordDto practiceRecord = await _practiceRecordService.CreatePracticeRecordAsync(createDto);
        return SuccessResponse(practiceRecord);
    }

    /// <summary>
    /// 更新练习记录
    /// </summary>
    /// <param name="id">练习记录ID</param>
    /// <param name="updateDto">更新练习记录DTO</param>
    /// <returns>成功响应</returns>
    [HttpPut("{id}")]
    [DisplayName("更新练习记录")]
    public async Task<ActionResult<ApiResponse>> UpdatePracticeRecord(long id, [FromBody] UpdatePracticeRecordDto updateDto)
    {
        await _practiceRecordService.UpdatePracticeRecordAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除练习记录
    /// </summary>
    /// <param name="id">练习记录ID</param>
    /// <returns>成功响应</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除练习记录")]
    public async Task<ActionResult<ApiResponse>> DeletePracticeRecord(long id)
    {
        await _practiceRecordService.DeletePracticeRecordAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量删除练习记录
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>成功响应，包含成功删除数量和失败ID列表</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要删除选中的练习记录吗？")]
    [DisplayName("批量删除练习记录")]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        (int successCount, List<long> failedIds) = await _practiceRecordService.BatchDeleteAsync(request.Ids);
        
        return failedIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个练习记录，但以下练习记录删除失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功删除 {successCount} 个练习记录！");
    }

    /// <summary>
    /// 批量导入练习记录
    /// </summary>
    /// <param name="importDto">导入数据</param>
    /// <returns>成功响应，包含成功导入数量和失败ID列表</returns>
    [HttpPost("batch/import")]
    [Operation("批量导入", "ajax")]
    [DisplayName("批量导入练习记录")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<PracticeRecordBatchImportDto> importDto)
    {
        ArgumentNullException.ThrowIfNull(importDto);
        
        (int successCount, List<string> failedIds) = await _practiceRecordService.BatchImportAsync(importDto.ImportData);
        
        return failedIds.Any()
            ? SuccessResponse($"成功导入 {successCount} 个练习记录，但以下练习记录导入失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功导入 {successCount} 个练习记录！");
    }
} 