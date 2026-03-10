using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Extensions;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 答题日志服务实现
/// </summary>
public class ExamAnswerLogService : IExamAnswerLogService, IScopedDependency
{
    private readonly IRepository<ExamAnswerOperationLog> _operationLogRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="operationLogRepository">答题操作日志仓储</param>
    public ExamAnswerLogService(IRepository<ExamAnswerOperationLog> operationLogRepository)
    {
        _operationLogRepository = operationLogRepository;
    }

    /// <inheritdoc />
    public async Task<PageList<ExamAnswerLogDto>> GetPagedListAsync(ExamAnswerLogQueryDto queryDto)
    {
        // 考试记录ID或考试设置ID至少提供一个
        bool hasValidFilter = (queryDto.ExamRecordId.HasValue && queryDto.ExamRecordId > 0) || queryDto.ExamSettingId > 0;
        if (!hasValidFilter)
        {
            return new PageList<ExamAnswerLogDto>([], 0);
        }

        var query = _operationLogRepository.CreateQuery()
            .AsNoTracking()
            .Include(l => l.ExamRecord)
                .ThenInclude(r => r!.ExamSetting)
            .Include(l => l.ExamRecord)
                .ThenInclude(r => r!.Student)
            .Include(l => l.QuestionVersion)
                .ThenInclude(qv => qv!.Question)
            .Where(l => l.ExamRecord != null);

        // 考试记录ID筛选（优先）
        if (queryDto.ExamRecordId is > 0)
        {
            query = query.Where(l => l.ExamRecordId == queryDto.ExamRecordId);
        }
        else
        {
            query = query.Where(l => l.ExamRecord!.ExamSettingId == queryDto.ExamSettingId);
        }

        // 关键字搜索（题目内容、考生答案）
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            var kw = queryDto.Keywords.Trim();
            query = query.Where(l =>
                (l.QuestionVersion != null && l.QuestionVersion.Content != null && l.QuestionVersion.Content.Contains(kw)) ||
                (l.Answer != null && l.Answer.Contains(kw)));
        }

        // 考生姓名搜索
        if (!string.IsNullOrWhiteSpace(queryDto.StudentName))
        {
            var name = queryDto.StudentName.Trim();
            query = query.Where(l => l.ExamRecord != null && l.ExamRecord.Student != null
                && l.ExamRecord.Student.Name != null && l.ExamRecord.Student.Name.Contains(name));
        }

        // 准考证号搜索
        if (!string.IsNullOrWhiteSpace(queryDto.AdmissionTicket))
        {
            var ticket = queryDto.AdmissionTicket.Trim();
            query = query.Where(l => l.ExamRecord != null && l.ExamRecord.Student != null
                && l.ExamRecord.Student.StudentNumber != null && l.ExamRecord.Student.StudentNumber.Contains(ticket));
        }

        var total = await query.CountAsync();

        // 排序
        query = query
            .OrderBy(l => l.ExamRecord!.StartTime)
            .ThenBy(l => l.OperationTime)
            .ThenBy(l => l.OrderNumber);

        var skip = (queryDto.Page - 1) * queryDto.PerPage;
        var logs = await query
            .Skip(skip)
            .Take(queryDto.PerPage)
            .ToListAsync();

        var items = logs.Select(log => new ExamAnswerLogDto
        {
            Id = log.Id,
            ExamRecordId = log.ExamRecordId,
            ExamName = log.ExamRecord?.ExamSetting?.Name ?? "",
            StudentName = log.ExamRecord?.Student?.Name ?? "",
            AdmissionTicket = log.ExamRecord?.Student?.StudentNumber ?? "",
            OrderNumber = log.OrderNumber,
            QuestionContent = log.QuestionVersion?.Content ?? "",
            QuestionType = log.QuestionVersion?.Question != null
                ? log.QuestionVersion.Question.Type.GetDisplayName() ?? log.QuestionVersion.Question.Type.ToString()
                : "",
            OperationType = log.OperationType.GetDisplayName() ?? log.OperationType.ToString(),
            OperationTime = log.OperationTime,
            Answer = log.Answer ?? ""
        }).ToList();

        return new PageList<ExamAnswerLogDto>(items, total);
    }
}
