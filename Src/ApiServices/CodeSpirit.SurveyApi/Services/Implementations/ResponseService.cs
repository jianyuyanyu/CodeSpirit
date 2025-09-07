using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Dtos;
using CodeSpirit.SurveyApi.Dtos.Response;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 问卷回答服务实现
/// </summary>
public class ResponseService : IResponseService, IScopedDependency
{
    private readonly IRepository<SurveyResponse> _repository;
    private readonly IRepository<ResponseAnswer> _answerRepository;
    private readonly IRepository<Survey> _surveyRepository;
    private readonly IRepository<Question> _questionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ResponseService> _logger;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">回答仓储</param>
    /// <param name="answerRepository">答案仓储</param>
    /// <param name="surveyRepository">问卷仓储</param>
    /// <param name="questionRepository">题目仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志器</param>
    /// <param name="currentUser">当前用户</param>
    public ResponseService(
        IRepository<SurveyResponse> repository,
        IRepository<ResponseAnswer> answerRepository,
        IRepository<Survey> surveyRepository,
        IRepository<Question> questionRepository,
        IMapper mapper,
        ILogger<ResponseService> logger,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _answerRepository = answerRepository;
        _surveyRepository = surveyRepository;
        _questionRepository = questionRepository;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 根据查询条件获取回答分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分页结果</returns>
    public async Task<PageList<ResponseDto>> GetPagedResponsesAsync(ResponseQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<SurveyResponse>(true);

        // 问卷ID筛选
        if (queryDto.SurveyId.HasValue)
        {
            predicate = predicate.And(r => r.SurveyId == queryDto.SurveyId.Value);
        }

        // 答题者ID筛选
        if (!string.IsNullOrWhiteSpace(queryDto.RespondentId))
        {
            predicate = predicate.And(r => r.RespondentId != null && r.RespondentId.Contains(queryDto.RespondentId));
        }

        // 会话ID筛选
        if (!string.IsNullOrWhiteSpace(queryDto.SessionId))
        {
            predicate = predicate.And(r => r.SessionId.Contains(queryDto.SessionId));
        }

        // 状态筛选
        if (queryDto.Status.HasValue)
        {
            predicate = predicate.And(r => r.Status == queryDto.Status.Value);
        }

        // IP地址筛选
        if (!string.IsNullOrWhiteSpace(queryDto.IpAddress))
        {
            predicate = predicate.And(r => r.IpAddress != null && r.IpAddress.Contains(queryDto.IpAddress));
        }

        // 开始时间范围筛选
        if (queryDto.StartedAtFrom.HasValue)
        {
            predicate = predicate.And(r => r.StartedAt >= queryDto.StartedAtFrom.Value);
        }
        if (queryDto.StartedAtTo.HasValue)
        {
            predicate = predicate.And(r => r.StartedAt <= queryDto.StartedAtTo.Value);
        }

        // 完成时间范围筛选
        if (queryDto.CompletedAtFrom.HasValue)
        {
            predicate = predicate.And(r => r.CompletedAt >= queryDto.CompletedAtFrom.Value);
        }
        if (queryDto.CompletedAtTo.HasValue)
        {
            predicate = predicate.And(r => r.CompletedAt <= queryDto.CompletedAtTo.Value);
        }

        // 问卷标题筛选
        if (!string.IsNullOrWhiteSpace(queryDto.SurveyTitle))
        {
            predicate = predicate.And(r => r.Survey.Title.Contains(queryDto.SurveyTitle));
        }

        var query = _repository.Find(predicate);

        if (queryDto.IncludeAnswers)
        {
            query = query.Include(r => r.Survey)
                .Include(r => r.Answers)
                .ThenInclude(a => a.Question);
        }
        else
        {
            query = query.Include(r => r.Survey);
        }

        // 排序
        query = !string.IsNullOrWhiteSpace(queryDto.OrderBy) ? queryDto.OrderBy.ToLower() switch
        {
            "surveyid" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.SurveyId) : query.OrderByDescending(r => r.SurveyId),
            "respondentid" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.RespondentId) : query.OrderByDescending(r => r.RespondentId),
            "sessionid" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.SessionId) : query.OrderByDescending(r => r.SessionId),
            "startedat" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.StartedAt) : query.OrderByDescending(r => r.StartedAt),
            "completedat" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.CompletedAt) : query.OrderByDescending(r => r.CompletedAt),
            "status" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
            "ipaddress" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.IpAddress) : query.OrderByDescending(r => r.IpAddress),
            "createdat" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
            "updatedat" => queryDto.OrderDir == "asc" ? query.OrderBy(r => r.UpdatedAt) : query.OrderByDescending(r => r.UpdatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        } : query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        var dtos = _mapper.Map<List<ResponseDto>>(items);

        // 计算答题用时
        foreach (var dto in dtos)
        {
            if (dto.CompletedAt.HasValue)
            {
                dto.DurationMinutes = (int)(dto.CompletedAt.Value - dto.StartedAt).TotalMinutes;
            }
        }

        return new PageList<ResponseDto>(dtos, totalCount);
    }

    /// <summary>
    /// 根据ID获取回答详情
    /// </summary>
    /// <param name="id">回答ID</param>
    /// <returns>回答详情</returns>
    public async Task<ResponseDetailDto?> GetResponseDetailAsync(int id)
    {
        var response = await _repository.Find(r => r.Id == id)
            .Include(r => r.Survey)
            .Include(r => r.Answers)
            .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync();

        if (response == null)
        {
            return null;
        }

        var dto = _mapper.Map<ResponseDetailDto>(response);

        // 计算答题用时
        if (dto.CompletedAt.HasValue)
        {
            dto.DurationMinutes = (int)(dto.CompletedAt.Value - dto.StartedAt).TotalMinutes;
        }

        return dto;
    }

    /// <summary>
    /// 根据问卷ID获取回答列表
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="includeAnswers">是否包含答案详情</param>
    /// <returns>回答列表</returns>
    public async Task<List<ResponseDto>> GetResponsesBySurveyIdAsync(int surveyId, bool includeAnswers = false)
    {
        var query = _repository.Find(r => r.SurveyId == surveyId);

        if (includeAnswers)
        {
            query = query.Include(r => r.Survey)
                .Include(r => r.Answers)
                .ThenInclude(a => a.Question);
        }
        else
        {
            query = query.Include(r => r.Survey);
        }

        var responses = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var dtos = _mapper.Map<List<ResponseDto>>(responses);

        // 计算答题用时
        foreach (var dto in dtos)
        {
            if (dto.CompletedAt.HasValue)
            {
                dto.DurationMinutes = (int)(dto.CompletedAt.Value - dto.StartedAt).TotalMinutes;
            }
        }

        return dtos;
    }

    /// <summary>
    /// 获取问卷回答统计信息
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>统计信息</returns>
    public async Task<ResponseStatisticsDto?> GetResponseStatisticsAsync(int surveyId)
    {
        var survey = await _surveyRepository.Find(s => s.Id == surveyId)
            .FirstOrDefaultAsync();

        if (survey == null)
        {
            return null;
        }

        var responses = await _repository.Find(r => r.SurveyId == surveyId)
            .ToListAsync();

        if (!responses.Any())
        {
            return new ResponseStatisticsDto
            {
                SurveyId = surveyId,
                SurveyTitle = survey.Title
            };
        }

        var completedResponses = responses.Where(r => r.Status == ResponseStatus.Completed).ToList();
        var inProgressResponses = responses.Where(r => r.Status == ResponseStatus.InProgress).ToList();
        var abandonedResponses = responses.Where(r => r.Status == ResponseStatus.Abandoned).ToList();

        var now = DateTime.UtcNow;
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        var statistics = new ResponseStatisticsDto
        {
            SurveyId = surveyId,
            SurveyTitle = survey.Title,
            TotalResponses = responses.Count,
            CompletedResponses = completedResponses.Count,
            InProgressResponses = inProgressResponses.Count,
            AbandonedResponses = abandonedResponses.Count,
            CompletionRate = responses.Count > 0 ? (decimal)completedResponses.Count / responses.Count * 100 : 0,
            Last7DaysResponses = responses.Count(r => r.CreatedAt >= last7Days),
            Last30DaysResponses = responses.Count(r => r.CreatedAt >= last30Days),
            FirstResponseAt = responses.Min(r => r.CreatedAt),
            LastResponseAt = responses.Max(r => r.CreatedAt)
        };

        // 计算答题用时统计
        var completedWithDuration = completedResponses
            .Where(r => r.CompletedAt.HasValue)
            .Select(r => (int)(r.CompletedAt!.Value - r.StartedAt).TotalMinutes)
            .Where(d => d > 0)
            .ToList();

        if (completedWithDuration.Any())
        {
            statistics.AverageDurationMinutes = (decimal)completedWithDuration.Average();
            statistics.MinDurationMinutes = completedWithDuration.Min();
            statistics.MaxDurationMinutes = completedWithDuration.Max();
        }

        return statistics;
    }

    /// <summary>
    /// 批量删除回答
    /// </summary>
    /// <param name="ids">回答ID列表</param>
    /// <returns>删除结果</returns>
    public async Task<bool> BatchDeleteResponsesAsync(List<int> ids)
    {
        try
        {
            // 先删除相关的答案
            var answers = await _answerRepository.Find(a => ids.Contains(a.ResponseId))
                .ToListAsync();

            if (answers.Any())
            {
                await _answerRepository.DeleteRangeAsync(answers);
            }

            // 再删除回答
            var responses = await _repository.Find(r => ids.Contains(r.Id))
                .ToListAsync();

            if (responses.Any())
            {
                await _repository.DeleteRangeAsync(responses);
            }

            _logger.LogInformation("批量删除回答成功，删除数量: {Count}", responses.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除回答失败，ID列表: {Ids}", string.Join(",", ids));
            return false;
        }
    }

    /// <summary>
    /// 根据问卷ID删除所有回答
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>删除结果</returns>
    public async Task<bool> DeleteResponsesBySurveyIdAsync(int surveyId)
    {
        try
        {
            // 先删除相关的答案
            var answers = await _answerRepository.Find(a => a.Response.SurveyId == surveyId)
                .ToListAsync();

            if (answers.Any())
            {
                await _answerRepository.DeleteRangeAsync(answers);
            }

            // 再删除回答
            var responses = await _repository.Find(r => r.SurveyId == surveyId)
                .ToListAsync();

            if (responses.Any())
            {
                await _repository.DeleteRangeAsync(responses);
            }

            _logger.LogInformation("根据问卷ID删除所有回答成功，问卷ID: {SurveyId}, 删除数量: {Count}", surveyId, responses.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据问卷ID删除所有回答失败，问卷ID: {SurveyId}", surveyId);
            return false;
        }
    }

    /// <summary>
    /// 导出问卷回答数据
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="format">导出格式（excel、csv）</param>
    /// <returns>导出文件字节数组</returns>
    public async Task<byte[]> ExportResponsesAsync(int surveyId, string format = "excel")
    {
        var survey = await _surveyRepository.Find(s => s.Id == surveyId)
            .Include(s => s.Questions.OrderBy(q => q.OrderIndex))
            .FirstOrDefaultAsync();

        if (survey == null)
        {
            throw new ArgumentException($"问卷不存在，ID: {surveyId}");
        }

        var responses = await _repository.Find(r => r.SurveyId == surveyId && r.Status == ResponseStatus.Completed)
            .Include(r => r.Answers)
            .ThenInclude(a => a.Question)
            .OrderBy(r => r.CompletedAt)
            .ToListAsync();

        if (format.ToLower() == "excel")
        {
            return await ExportToExcelAsync(survey, responses);
        }
        else
        {
            return await ExportToCsvAsync(survey, responses);
        }
    }

    /// <summary>
    /// 检查回答是否存在
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <returns>是否存在</returns>
    public async Task<bool> CheckResponseExistsAsync(int surveyId, string sessionId)
    {
        return await _repository.Find(r => r.SurveyId == surveyId && r.SessionId == sessionId)
            .AnyAsync();
    }

    /// <summary>
    /// 获取用户的回答历史
    /// </summary>
    /// <param name="respondentId">答题者ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>回答历史分页结果</returns>
    public async Task<PageList<ResponseDto>> GetUserResponseHistoryAsync(string respondentId, int pageIndex = 1, int pageSize = 20)
    {
        var query = _repository.Find(r => r.RespondentId == respondentId)
            .Include(r => r.Survey)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<ResponseDto>>(items);

        // 计算答题用时
        foreach (var dto in dtos)
        {
            if (dto.CompletedAt.HasValue)
            {
                dto.DurationMinutes = (int)(dto.CompletedAt.Value - dto.StartedAt).TotalMinutes;
            }
        }

        return new PageList<ResponseDto>(dtos, totalCount);
    }

    /// <summary>
    /// 标记回答为已放弃
    /// </summary>
    /// <param name="id">回答ID</param>
    /// <returns>操作结果</returns>
    public async Task<bool> MarkAsAbandonedAsync(int id)
    {
        try
        {
            var response = await _repository.GetByIdAsync(id);
            if (response == null)
            {
                return false;
            }

            response.Status = ResponseStatus.Abandoned;
            response.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(response);
            _logger.LogInformation("标记回答为已放弃成功，回答ID: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记回答为已放弃失败，回答ID: {Id}", id);
            return false;
        }
    }

    /// <summary>
    /// 获取回答的答案详情
    /// </summary>
    /// <param name="responseId">回答ID</param>
    /// <returns>答案详情列表</returns>
    public async Task<List<ResponseAnswerDto>> GetResponseAnswersAsync(int responseId)
    {
        var answers = await _answerRepository.Find(a => a.ResponseId == responseId)
            .Include(a => a.Question)
            .OrderBy(a => a.Question.OrderIndex)
            .ToListAsync();

        return _mapper.Map<List<ResponseAnswerDto>>(answers);
    }

    /// <summary>
    /// 获取问卷的所有回答统计（按日期分组）
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="days">统计天数</param>
    /// <returns>按日期分组的统计数据</returns>
    public async Task<Dictionary<string, int>> GetResponseTrendAsync(int surveyId, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var responses = await _repository.Find(r => r.SurveyId == surveyId && r.CreatedAt >= startDate)
            .ToListAsync();

        var trend = new Dictionary<string, int>();

        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var dateKey = date.ToString("yyyy-MM-dd");
            var count = responses.Count(r => r.CreatedAt.Date == date);
            trend[dateKey] = count;
        }

        return trend;
    }

    /// <summary>
    /// 导出到Excel
    /// </summary>
    /// <param name="survey">问卷</param>
    /// <param name="responses">回答列表</param>
    /// <returns>Excel文件字节数组</returns>
    private async Task<byte[]> ExportToExcelAsync(Survey survey, List<SurveyResponse> responses)
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("问卷回答数据");

        // 设置表头
        var headers = new List<string>
        {
            "回答ID", "答题者ID", "会话ID", "开始时间", "完成时间", "状态", "IP地址", "答题用时(分钟)"
        };

        // 添加题目标题作为列头
        foreach (var question in survey.Questions.OrderBy(q => q.OrderIndex))
        {
            headers.Add(question.Title);
        }

        // 写入表头
        for (int i = 0; i < headers.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        // 写入数据
        for (int i = 0; i < responses.Count; i++)
        {
            var response = responses[i];
            var row = i + 2;

            worksheet.Cells[row, 1].Value = response.Id;
            worksheet.Cells[row, 2].Value = response.RespondentId ?? "";
            worksheet.Cells[row, 3].Value = response.SessionId;
            worksheet.Cells[row, 4].Value = response.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cells[row, 5].Value = response.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            worksheet.Cells[row, 6].Value = response.Status.ToString();
            worksheet.Cells[row, 7].Value = response.IpAddress ?? "";
            worksheet.Cells[row, 8].Value = response.CompletedAt.HasValue 
                ? (int)(response.CompletedAt.Value - response.StartedAt).TotalMinutes 
                : 0;

            // 写入答案数据
            var answerDict = response.Answers.ToDictionary(a => a.QuestionId, a => a.AnswerText ?? a.AnswerValue ?? "");
            for (int j = 0; j < survey.Questions.Count; j++)
            {
                var question = survey.Questions.OrderBy(q => q.OrderIndex).ElementAt(j);
                var answer = answerDict.GetValueOrDefault(question.Id, "");
                worksheet.Cells[row, 9 + j].Value = answer;
            }
        }

        // 自动调整列宽
        worksheet.Cells.AutoFitColumns();

        return await Task.FromResult(package.GetAsByteArray());
    }

    /// <summary>
    /// 导出到CSV
    /// </summary>
    /// <param name="survey">问卷</param>
    /// <param name="responses">回答列表</param>
    /// <returns>CSV文件字节数组</returns>
    private async Task<byte[]> ExportToCsvAsync(Survey survey, List<SurveyResponse> responses)
    {
        var csv = new StringBuilder();

        // 设置表头
        var headers = new List<string>
        {
            "回答ID", "答题者ID", "会话ID", "开始时间", "完成时间", "状态", "IP地址", "答题用时(分钟)"
        };

        // 添加题目标题作为列头
        foreach (var question in survey.Questions.OrderBy(q => q.OrderIndex))
        {
            headers.Add($"\"{question.Title}\"");
        }

        csv.AppendLine(string.Join(",", headers));

        // 写入数据
        foreach (var response in responses)
        {
            var row = new List<string>
            {
                response.Id.ToString(),
                $"\"{response.RespondentId ?? ""}\"",
                $"\"{response.SessionId}\"",
                $"\"{response.StartedAt:yyyy-MM-dd HH:mm:ss}\"",
                $"\"{response.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}\"",
                $"\"{response.Status}\"",
                $"\"{response.IpAddress ?? ""}\"",
                response.CompletedAt.HasValue 
                    ? ((int)(response.CompletedAt.Value - response.StartedAt).TotalMinutes).ToString()
                    : "0"
            };

            // 写入答案数据
            var answerDict = response.Answers.ToDictionary(a => a.QuestionId, a => a.AnswerText ?? a.AnswerValue ?? "");
            foreach (var question in survey.Questions.OrderBy(q => q.OrderIndex))
            {
                var answer = answerDict.GetValueOrDefault(question.Id, "");
                row.Add($"\"{answer.Replace("\"", "\"\"")}\""); // 转义双引号
            }

            csv.AppendLine(string.Join(",", row));
        }

        return await Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
    }
}
