using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.PracticeRecord;
using CodeSpirit.ExamApi.Dtos.PracticeSetting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Controllers.Client;

/// <summary>
/// 客户端练习控制器
/// </summary>
[DisplayName("练习")]
[Route("api/client/practice")]
public class PracticeController : ApiControllerBase
{
    private readonly IPracticeSettingService _practiceSettingService;
    private readonly IPracticeRecordService _practiceRecordService;
    private readonly ILogger<PracticeController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="practiceSettingService">练习设置服务</param>
    /// <param name="practiceRecordService">练习记录服务</param>
    /// <param name="logger">日志服务</param>
    public PracticeController(
        IPracticeSettingService practiceSettingService, 
        IPracticeRecordService practiceRecordService,
        ILogger<PracticeController> logger)
    {
        _practiceSettingService = practiceSettingService;
        _practiceRecordService = practiceRecordService;
        _logger = logger;
    }

    /// <summary>
    /// 获取可用的练习设置列表
    /// </summary>
    /// <returns>练习设置列表</returns>
    [HttpGet("settings")]
    [DisplayName("获取可用练习设置")]
    public async Task<ActionResult<ApiResponse<List<PracticeSettingDto>>>> GetAvailablePracticeSettings()
    {
        // 从Token中获取当前学生ID (仅是示例，实际应根据认证系统获取)
        long studentId = GetCurrentUserId();
        
        // 仅获取已发布的练习设置
        var queryDto = new PracticeSettingQueryDto
        {
            Status = PracticeSettingStatus.Published
        };
        var result = await _practiceSettingService.GetPracticeSettingsAsync(queryDto);
        return SuccessResponse(result.Items);
    }

    /// <summary>
    /// 获取学生的练习统计数据
    /// </summary>
    /// <returns>练习统计数据</returns>
    [HttpGet("statistics")]
    [DisplayName("获取练习统计")]
    public async Task<ActionResult<ApiResponse<PracticeStatisticsDto>>> GetPracticeStatistics()
    {
        long studentId = GetCurrentUserId();
        var statistics = await _practiceRecordService.GetStudentPracticeStatisticsAsync(studentId);
        return SuccessResponse(statistics);
    }

    /// <summary>
    /// 获取学生的练习记录
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>练习记录列表</returns>
    [HttpGet("records")]
    [DisplayName("获取练习记录")]
    public async Task<ActionResult<ApiResponse<PageList<PracticeRecordDto>>>> GetPracticeRecords([FromQuery] PracticeRecordQueryDto queryDto)
    {
        long studentId = GetCurrentUserId();
        queryDto.StudentId = studentId;
        var records = await _practiceRecordService.GetPracticeRecordsAsync(queryDto);
        return SuccessResponse(records);
    }

    // 获取当前用户ID (示例方法)
    private long GetCurrentUserId()
    {
        // 实际应用中，应从认证Token中获取
        // 这里简单返回一个示例值
        return 1;
    }
} 