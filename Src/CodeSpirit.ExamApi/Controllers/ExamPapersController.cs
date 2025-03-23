using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 试卷管理
/// </summary>
[DisplayName("试卷管理")]
[Navigation(Icon = "fa-solid fa-file-lines")]
public class ExamPapersController : ApiControllerBase
{
    private readonly IExamPaperService _examPaperService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamPapersController(IExamPaperService examPaperService)
    {
        _examPaperService = examPaperService;
    }

    /// <summary>
    /// 获取试卷分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>试卷分页列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<ExamPaperDto>>>> GetExamPapers([FromQuery] ExamPaperQueryDto queryDto)
    {
        var result = await _examPaperService.GetExamPapersAsync(queryDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取试卷详情
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>试卷详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> GetExamPaper(long id)
    {
        var result = await _examPaperService.GetAsync(id);
        if (result == null)
        {
            return NotFound("试卷不存在");
        }
        return SuccessResponse(result);
    }

    /// <summary>
    /// 创建试卷
    /// </summary>
    /// <param name="createDto">创建试卷DTO</param>
    /// <returns>创建的试卷</returns>
    [HttpPost]
    [HeaderOperation("生成固定试卷", "form")]
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> CreateExamPaper(CreateExamPaperDto createDto)
    {
        var result = await _examPaperService.CreateAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 更新试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <param name="updateDto">更新试卷DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateExamPaper(long id, UpdateExamPaperDto updateDto)
    {
        await _examPaperService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此试卷吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteExamPaper(long id)
    {
        await _examPaperService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/publish")]
    [Operation("发布", "ajax", null, "确定要发布此试卷吗？")]
    public async Task<ActionResult<ApiResponse>> PublishExamPaper(long id)
    {
        await _examPaperService.PublishExamPaperAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 取消发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/unpublish")]
    [Operation("取消发布", "ajax", null, "确定要取消发布此试卷吗？")]
    public async Task<ActionResult<ApiResponse>> UnpublishExamPaper(long id)
    {
        await _examPaperService.UnpublishExamPaperAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量删除试卷
    /// </summary>
    /// <param name="ids">试卷ID列表</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch-delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的试卷吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDeleteExamPapers([FromBody] IEnumerable<long> ids)
    {
        var result = await _examPaperService.BatchDeleteAsync(ids);
        return SuccessResponse($"成功删除{result.successCount}个试卷，失败{result.failedIds.Count}个");
    }

    /// <summary>
    /// 生成随机试卷
    /// </summary>
    /// <param name="createDto">随机试卷生成DTO</param>
    /// <returns>生成的试卷</returns>
    [HttpPost("generate-random")]
    [HeaderOperation("生成随机试卷", "form")]
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> GenerateRandomExamPaper(GenerateRandomExamPaperDto createDto)
    {
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 复制试卷
    /// </summary>
    /// <param name="id">源试卷ID</param>
    /// <returns>复制的新试卷</returns>
    [HttpPost("{id}/copy")]
    [Operation("复制", "ajax", null, "确定要复制此试卷吗？")]
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> CopyExamPaper(long id)
    {
        var result = await _examPaperService.CopyExamPaperAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取试卷下拉列表
    /// </summary>
    /// <returns>试卷下拉列表</returns>
    [HttpGet("select-published")]
    public async Task<ActionResult<ApiResponse<List<OptionDto<long>>>>> GetSelectList()
    {
        var papers = await _examPaperService.GetAllExamPapersByStatusAsync(ExamPaperStatus.Published);
        var result = papers.Select(p => new OptionDto<long>
        {
            Id = p.Id,
            Name = p.Name
        }).ToList();
        return SuccessResponse(result);
    }
} 