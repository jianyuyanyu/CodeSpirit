using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using System.Globalization;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考试统计服务实现
/// </summary>
public class ExamStatisticsService : IExamStatisticsService
{
    private readonly ExamDbContext _dbContext;
    private readonly ILogger<ExamStatisticsService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="dataAnalyzer">数据分析器</param>
    public ExamStatisticsService(
        ExamDbContext dbContext,
        ILogger<ExamStatisticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 获取考试成绩统计数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>考试成绩统计数据</returns>
    public async Task<object> GetScoreStatisticsAsync(long? examSettingId, DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        var query = _dbContext.Set<ExamRecord>().AsQueryable();
        
        // 应用筛选条件
        if (examSettingId.HasValue && examSettingId.Value > 0)
        {
            query = query.Where(r => r.ExamSettingId == examSettingId.Value);
        }
        
        if (startDate.HasValue)
        {
            query = query.Where(r => r.StartTime >= startDate.Value.DateTime);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(r => r.StartTime <= endDate.Value.DateTime);
        }
        
        // 只查询已提交或已批改的考试
        query = query.Where(r => r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded);
        
        // 获取统计数据
        var statistics = await query
            .GroupBy(r => 1)
            .Select(g => new
            {
                // 考试人数
                TotalCount = g.Count(),
                // 平均分
                AverageScore = g.Average(r => r.Score ?? 0),
                // 最高分
                MaxScore = g.Max(r => r.Score ?? 0),
                // 最低分
                MinScore = g.Min(r => r.Score ?? 0),
                // 及格人数
                PassCount = g.Count(r => r.IsPassed),
                // 及格率
                PassRate = g.Count(r => r.IsPassed) * 100.0 / g.Count()
            })
            .FirstOrDefaultAsync() ?? new
            {
                TotalCount = 0,
                AverageScore = 0.0,
                MaxScore = 0.0,
                MinScore = 0.0,
                PassCount = 0,
                PassRate = 0.0
            };
            
        // 将统计数据转换为图表所需的集合格式
        var result = new List<object>
        {
            new { Category = "考试人数", Value = statistics.TotalCount },
            new { Category = "平均分", Value = Math.Round(statistics.AverageScore, 2) },
            new { Category = "最高分", Value = statistics.MaxScore },
            new { Category = "最低分", Value = statistics.MinScore },
            new { Category = "及格人数", Value = statistics.PassCount },
            new { Category = "及格率(%)", Value = Math.Round(statistics.PassRate, 2) }
        };
            
        return result;
    }

    /// <summary>
    /// 获取考试及格率分析数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="groupBy">分组方式: Day, Week, Month, Year</param>
    /// <returns>及格率分析数据</returns>
    public async Task<object> GetPassRateAnalysisAsync(long? examSettingId, DateTimeOffset? startDate, DateTimeOffset? endDate, string groupBy = "Day")
    {
        var query = _dbContext.Set<ExamRecord>().AsQueryable();
        
        // 应用筛选条件
        if (examSettingId.HasValue && examSettingId.Value > 0)
        {
            query = query.Where(r => r.ExamSettingId == examSettingId.Value);
        }
        
        startDate ??= DateTimeOffset.Now.AddMonths(-1);
        endDate ??= DateTimeOffset.Now;
        
        query = query.Where(r => r.StartTime >= startDate.Value.DateTime && r.StartTime <= endDate.Value.DateTime);
        
        // 只查询已提交或已批改的考试
        query = query.Where(r => r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded);
        
        // 获取所有记录
        var records = await query.Select(r => new { 
            r.StartTime, 
            r.Score, 
            r.IsPassed 
        }).ToListAsync();
        
        // 按时间分组
        var groupedData = GroupDataByTime(records, r => new DateTimeOffset(r.StartTime), groupBy);
        
        // 计算每组的及格率
        var result = groupedData.Select(g => new
        {
            TimePeriod = g.Key,
            TotalCount = g.Count(),
            PassCount = g.Count(r => r.IsPassed),
            PassRate = g.Any() ? g.Count(r => r.IsPassed) * 100.0 / g.Count() : 0
        }).ToList();
        
        return result;
    }

    /// <summary>
    /// 获取分数段分布数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="segments">分数段数量</param>
    /// <returns>分数段分布数据</returns>
    public async Task<object> GetScoreDistributionAsync(long? examSettingId, DateTimeOffset? startDate, DateTimeOffset? endDate, int segments = 10)
    {
        var query = _dbContext.Set<ExamRecord>().AsQueryable();
        
        // 应用筛选条件
        if (examSettingId.HasValue && examSettingId.Value > 0)
        {
            query = query.Where(r => r.ExamSettingId == examSettingId.Value);
        }
        
        if (startDate.HasValue)
        {
            query = query.Where(r => r.StartTime >= startDate.Value.DateTime);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(r => r.StartTime <= endDate.Value.DateTime);
        }
        
        // 只查询已提交或已批改的考试
        query = query.Where(r => r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded);
        
        // 获取所有考试记录的分数
        var scores = await query.Where(r => r.Score.HasValue).Select(r => r.Score.Value).ToListAsync();
        
        if (!scores.Any())
        {
            return Enumerable.Range(0, segments).Select(i => new { 
                ScoreRange = $"{i * 100 / segments}-{(i + 1) * 100 / segments}", 
                Count = 0 
            }).ToList();
        }
        
        // 计算最高分和段间隔
        double maxScore = 100; // 假设考试满分是100分
        double segmentSize = maxScore / segments;
        
        // 计算每个分数段的人数
        var distribution = new List<object>();
        for (int i = 0; i < segments; i++)
        {
            double minScore = i * segmentSize;
            double maxSegmentScore = (i + 1) * segmentSize;
            
            // 最后一个区间包含最高分
            bool isLastSegment = i == segments - 1;
            
            int count = isLastSegment
                ? scores.Count(s => s >= minScore && s <= maxSegmentScore)
                : scores.Count(s => s >= minScore && s < maxSegmentScore);
                
            distribution.Add(new
            {
                ScoreRange = $"{minScore:F0}-{maxSegmentScore:F0}",
                Count = count
            });
        }
        
        return distribution;
    }

    /// <summary>
    /// 获取题目正确率分析数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="questionType">题目类型</param>
    /// <param name="topCount">获取数量</param>
    /// <returns>题目正确率分析数据</returns>
    public async Task<object> GetQuestionCorrectRateAsync(long? examSettingId, int? questionType, int topCount = 10)
    {
        // 查询条件
        var query = _dbContext.Set<ExamAnswerRecord>()
            .Include(d => d.ExamRecord)
            .Include(d => d.Question)
            .Where(d => d.ExamRecord.Status == ExamRecordStatus.Submitted || d.ExamRecord.Status == ExamRecordStatus.Graded);
            
        if (examSettingId.HasValue && examSettingId.Value > 0)
        {
            query = query.Where(d => d.ExamRecord.ExamSettingId == examSettingId.Value);
        }
        
        if (questionType.HasValue)
        {
            query = query.Where(d => d.Question.Type == (QuestionType)questionType.Value);
        }
        
        // 按题目分组，计算每个题目的正确率
        var correctRates = await query
            .GroupBy(d => new { d.QuestionId, QuestionTitle = d.Question.Content })
            .Select(g => new
            {
                g.Key.QuestionId,
                g.Key.QuestionTitle,
                TotalCount = g.Count(),
                CorrectCount = g.Count(d => d.IsCorrect == true),
                CorrectRate = g.Count(d => d.IsCorrect == true) * 100.0 / g.Count()
            })
            .OrderByDescending(x => x.CorrectRate)
            .Take(topCount)
            .ToListAsync();
            
        return correctRates;
    }

    /// <summary>
    /// 获取错题分析数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="questionType">题目类型</param>
    /// <param name="topCount">获取数量</param>
    /// <returns>错题分析数据</returns>
    public async Task<object> GetWrongQuestionAnalysisAsync(long? examSettingId, int? questionType, int topCount = 10)
    {
        // 查询条件
        var query = _dbContext.Set<ExamAnswerRecord>()
            .Include(d => d.ExamRecord)
            .Include(d => d.Question)
            .Where(d => (d.ExamRecord.Status == ExamRecordStatus.Submitted || d.ExamRecord.Status == ExamRecordStatus.Graded) && d.IsCorrect == false); // 只查询错题
            
        if (examSettingId.HasValue && examSettingId.Value > 0)
        {
            query = query.Where(d => d.ExamRecord.ExamSettingId == examSettingId.Value);
        }
        
        if (questionType.HasValue)
        {
            query = query.Where(d => d.Question.Type == (QuestionType)questionType.Value);
        }
        
        // 按题目分组，计算每个题目的错误次数
        var wrongQuestions = await query
            .GroupBy(d => new { d.QuestionId, QuestionTitle = d.Question.Content })
            .Select(g => new
            {
                g.Key.QuestionId,
                g.Key.QuestionTitle,
                WrongCount = g.Count()
            })
            .OrderByDescending(x => x.WrongCount)
            .Take(topCount)
            .ToListAsync();
            
        return wrongQuestions;
    }
    
    #region 辅助方法
    
    /// <summary>
    /// 按时间分组数据
    /// </summary>
    private IEnumerable<IGrouping<string, T>> GroupDataByTime<T>(IEnumerable<T> data, Func<T, DateTimeOffset> timeSelector, string groupBy)
    {
        return groupBy.ToLower() switch
        {
            "day" => data.GroupBy(r => timeSelector(r).ToString("yyyy-MM-dd")),
            "week" => data.GroupBy(r => $"{timeSelector(r).Year}-W{GetIso8601WeekOfYear(timeSelector(r).DateTime)}"),
            "month" => data.GroupBy(r => timeSelector(r).ToString("yyyy-MM")),
            "year" => data.GroupBy(r => timeSelector(r).ToString("yyyy")),
            _ => data.GroupBy(r => timeSelector(r).ToString("yyyy-MM-dd"))
        };
    }
    
    /// <summary>
    /// 获取日期在一年中的ISO周数
    /// </summary>
    private int GetIso8601WeekOfYear(DateTime time)
    {
        // 用 DayOfYear 和 DayOfWeek 计算 ISO 8601 周数
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            time = time.AddDays(3);
        }
        
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            time, 
            CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday);
    }    
    #endregion
} 