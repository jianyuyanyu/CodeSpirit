using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考试设置服务实现
/// </summary>
public class ExamSettingService : BaseCRUDService<ExamSetting, ExamSettingDto, long, CreateExamSettingDto, UpdateExamSettingDto>, IExamSettingService
{
    private readonly IRepository<ExamSetting> _repository;
    private readonly IRepository<ExamPaper> _examPaperRepository;
    private readonly IRepository<StudentGroup> _studentGroupRepository;
    private readonly IRepository<ExamSettingStudentGroup> _examSettingStudentGroupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ExamSettingService> _logger;
    private readonly ExamDbContext _context;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamSettingService(
        IRepository<ExamSetting> repository,
        IRepository<ExamPaper> examPaperRepository,
        IRepository<StudentGroup> studentGroupRepository,
        IRepository<ExamSettingStudentGroup> examSettingStudentGroupRepository,
        IMapper mapper,
        ILogger<ExamSettingService> logger,
        ExamDbContext context)
        : base(repository, mapper)
    {
        _repository = repository;
        _examPaperRepository = examPaperRepository;
        _studentGroupRepository = studentGroupRepository;
        _examSettingStudentGroupRepository = examSettingStudentGroupRepository;
        _mapper = mapper;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 获取考试设置分页列表
    /// </summary>
    public async Task<PageList<ExamSettingDto>> GetExamSettingsAsync(ExamSettingQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<ExamSetting>(true);

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            predicate = predicate.And(x => x.Name.Contains(queryDto.Keywords) || 
                                         x.Description.Contains(queryDto.Keywords));
        }

        // 试卷ID筛选
        if (queryDto.ExamPaperId.HasValue)
        {
            predicate = predicate.And(x => x.ExamPaperId == queryDto.ExamPaperId.Value);
        }

        // 开始时间范围筛选
        if (queryDto.StartTimeFrom.HasValue)
        {
            predicate = predicate.And(x => x.StartTime >= queryDto.StartTimeFrom.Value);
        }
        if (queryDto.StartTimeTo.HasValue)
        {
            predicate = predicate.And(x => x.StartTime <= queryDto.StartTimeTo.Value);
        }

        // 结束时间范围筛选
        if (queryDto.EndTimeFrom.HasValue)
        {
            predicate = predicate.And(x => x.EndTime >= queryDto.EndTimeFrom.Value);
        }
        if (queryDto.EndTimeTo.HasValue)
        {
            predicate = predicate.And(x => x.EndTime <= queryDto.EndTimeTo.Value);
        }

        var query = _repository.CreateQuery()
            .Include(x => x.ExamPaper)
            .Include(x => x.StudentGroups)
                .ThenInclude(x => x.StudentGroup)
            .Where(predicate);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        var dtos = _mapper.Map<List<ExamSettingDto>>(items);
        return new PageList<ExamSettingDto>(dtos, total);
    }

    /// <summary>
    /// 获取考试设置详情
    /// </summary>
    public async Task<ExamSettingDto> GetExamSettingDetailAsync(long id)
    {
        var examSetting = await _repository.CreateQuery()
            .Include(x => x.ExamPaper)
            .Include(x => x.StudentGroups)
                .ThenInclude(x => x.StudentGroup)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (examSetting == null)
        {
            return null;
        }

        return _mapper.Map<ExamSettingDto>(examSetting);
    }

    /// <summary>
    /// 创建考试设置
    /// </summary>
    public override async Task<ExamSettingDto> CreateAsync(CreateExamSettingDto createDto)
    {
        // 验证试卷是否存在
        var examPaper = await _examPaperRepository.GetByIdAsync(createDto.ExamPaperId);
        if (examPaper == null)
        {
            throw new AppServiceException(404, "试卷不存在");
        }

        // 验证学生分组是否存在
        var studentGroups = await _studentGroupRepository.CreateQuery()
            .Where(x => createDto.StudentGroupIds.Contains(x.Id))
            .ToListAsync();

        if (studentGroups.Count != createDto.StudentGroupIds.Count)
        {
            throw new AppServiceException(404, "部分学生分组不存在");
        }

        // 创建考试设置
        var examSetting = _mapper.Map<ExamSetting>(createDto);
        examSetting.Status = ExamSettingStatus.Draft;
        await _repository.AddAsync(examSetting);

        // 创建考试设置-学生分组关联
        var examSettingStudentGroups = createDto.StudentGroupIds.Select(groupId => new ExamSettingStudentGroup
        {
            ExamSettingId = examSetting.Id,
            StudentGroupId = groupId
        }).ToList();

        await _examSettingStudentGroupRepository.AddRangeAsync(examSettingStudentGroups);

        return await GetExamSettingDetailAsync(examSetting.Id);
    }

    /// <summary>
    /// 更新考试设置
    /// </summary>
    public override async Task UpdateAsync(long id, UpdateExamSettingDto updateDto)
    {
        var examSetting = await _repository.CreateQuery()
            .Include(x => x.StudentGroups)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (examSetting == null)
        {
            throw new AppServiceException(404, "考试设置不存在");
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的考试设置可以更新");
        }

        // 验证试卷是否存在
        var examPaper = await _examPaperRepository.GetByIdAsync(updateDto.ExamPaperId);
        if (examPaper == null)
        {
            throw new AppServiceException(404, "试卷不存在");
        }

        // 验证学生分组是否存在
        var studentGroups = await _studentGroupRepository.CreateQuery()
            .Where(x => updateDto.StudentGroupIds.Contains(x.Id))
            .ToListAsync();

        if (studentGroups.Count != updateDto.StudentGroupIds.Count)
        {
            throw new AppServiceException(404, "部分学生分组不存在");
        }

        // 更新基本信息
        _mapper.Map(updateDto, examSetting);

        // 更新学生分组关联
        await _repository.ExecuteInTransactionAsync(async () =>
        {
            // 删除原有关联
            foreach (var group in examSetting.StudentGroups)
            {
                await _examSettingStudentGroupRepository.DeleteAsync(group);
            }

            // 添加新关联
            var examSettingStudentGroups = updateDto.StudentGroupIds.Select(groupId => new ExamSettingStudentGroup
            {
                ExamSettingId = examSetting.Id,
                StudentGroupId = groupId
            }).ToList();

            await _examSettingStudentGroupRepository.AddRangeAsync(examSettingStudentGroups);
        });

        await _repository.UpdateAsync(examSetting);
    }

    /// <summary>
    /// 删除考试设置
    /// </summary>
    public override async Task DeleteAsync(long id)
    {
        var examSetting = await _repository.CreateQuery()
            .Include(x => x.StudentGroups)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (examSetting == null)
        {
            return;
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的考试设置可以删除");
        }

        await _repository.DeleteAsync(examSetting);
    }

    /// <summary>
    /// 发布考试设置
    /// </summary>
    public async Task PublishExamSettingAsync(long id)
    {
        var examSetting = await _repository.GetByIdAsync(id);
        if (examSetting == null)
        {
            throw new AppServiceException(404, "考试设置不存在");
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的考试设置可以发布");
        }

        // 检查试卷状态
        var examPaper = await _examPaperRepository.GetByIdAsync(examSetting.ExamPaperId);
        if (examPaper == null || examPaper.Status != ExamPaperStatus.Published)
        {
            throw new AppServiceException(400, "试卷未发布，不能发布考试设置");
        }

        //// 检查时间设置
        //if (examSetting.StartTime <= DateTime.UtcNow)
        //{
        //    throw new AppServiceException(400, "考试开始时间必须大于当前时间");
        //}

        if (examSetting.EndTime <= examSetting.StartTime)
        {
            throw new AppServiceException(400, "考试结束时间必须大于开始时间");
        }

        examSetting.Status = ExamSettingStatus.Published;
        await _repository.UpdateAsync(examSetting);
    }

    /// <summary>
    /// 取消发布考试设置
    /// </summary>
    public async Task UnpublishExamSettingAsync(long id)
    {
        var examSetting = await _repository.CreateQuery()
            .Include(x => x.ExamRecords)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (examSetting == null)
        {
            throw new AppServiceException(404, "考试设置不存在");
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Published)
        {
            throw new AppServiceException(400, "只有已发布状态的考试设置可以取消发布");
        }

        // 检查是否有学生已经开始考试
        if (examSetting.ExamRecords.Any())
        {
            throw new AppServiceException(400, "已有学生开始考试，不能取消发布");
        }

        examSetting.Status = ExamSettingStatus.Draft;
        await _repository.UpdateAsync(examSetting);
    }

    /// <summary>
    /// 获取用户可参加的考试列表
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>可参加的考试列表</returns>
    public async Task<List<ClientExamDto>> GetAvailableExamsForClientAsync(long studentId)
    {
        try
        {
            // 查询当前用户所属的学生组
            var studentGroups = await _context.StudentGroupMappings
                .Where(m => m.StudentId == studentId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            // 获取可参加的考试
            var now = DateTime.UtcNow;
            // 定义预展示时间，开考前半小时（30分钟）可见
            var previewTime = now.AddMinutes(30);
            
            var availableExams = await _context.ExamSettings
                .Include(e => e.StudentGroups)
                .Include(e => e.ExamPaper)
                .Where(e => e.Status == ExamSettingStatus.Published)
                .Where(e => 
                    // 正在进行中的考试或即将开始的考试（开考前半小时）
                    ((e.StartTime <= now && e.EndTime >= now) || 
                     (e.StartTime > now && e.StartTime <= previewTime))
                )
                .Where(e => e.StudentGroups.Any() == false || e.StudentGroups.Any(g => studentGroups.Contains(g.StudentGroupId)))
                .Select(e => new ClientExamDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Duration = e.Duration,
                    TotalScore = e.ExamPaper.TotalScore,
                    Status = _context.ExamRecords.Any(r =>
                        r.ExamSettingId == e.Id &&
                        r.StudentId == studentId &&
                        r.Status == ExamRecordStatus.InProgress)
                        ? "进行中"
                        : (_context.ExamRecords.Any(r =>
                            r.ExamSettingId == e.Id &&
                            r.StudentId == studentId &&
                            (r.Status == ExamRecordStatus.Graded || r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.InProgress))
                            ? "已完成"
                            : (e.StartTime <= now && e.EndTime >= now ? "进行中" :
                               (e.StartTime > now ? "未开始" : "已结束"))),
                    // 检查是否已参加并获取成绩
                    HasResult = _context.ExamRecords.Any(r =>
                        r.ExamSettingId == e.Id &&
                        r.StudentId == studentId &&
                        (r.Status == ExamRecordStatus.Graded || r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.InProgress))
                })
                .ToListAsync();

            return availableExams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可参加的考试列表时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试详情（客户端视图）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试详情</returns>
    public async Task<ClientExamDetailDto> GetExamDetailForClientAsync(long examId, long recordId)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new ArgumentException("考试不存在", nameof(examId));
            }

            // 加载试卷题目
            await _context.Entry(examSetting.ExamPaper)
                .Collection(p => p.ExamPaperQuestions)
                .LoadAsync();

            // 获取考试记录
            var examRecord = await _context.ExamRecords
                .Where(r => r.Id == recordId)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new InvalidOperationException("考试记录不存在");
            }

            // 预先加载ExamPaperQuestions的关联对象
            foreach (var question in examSetting.ExamPaper.ExamPaperQuestions)
            {
                await _context.Entry(question)
                    .Reference(q => q.Question)
                    .LoadAsync();

                await _context.Entry(question)
                    .Reference(q => q.QuestionVersion)
                    .LoadAsync();
            }

            // 处理题目乱序
            var questions = examSetting.ExamPaper.ExamPaperQuestions.ToList();
            if (examSetting.EnableRandomQuestionOrder)
            {
                Random rnd = new Random();
                questions = questions.OrderBy(q => rnd.Next()).ToList();
            }

            // 处理选项乱序
            if (examSetting.EnableRandomOptionOrder)
            {
                var randomGenerator = new Random();
                foreach (var question in questions)
                {
                    // 只对单选题和多选题进行选项乱序处理
                    if (question.Question.Type == QuestionType.SingleChoice ||
                        question.Question.Type == QuestionType.MultipleChoice)
                    {
                        var options = question.QuestionVersion.Options.OrderBy(o => randomGenerator.Next()).ToList();
                        question.QuestionVersion.Options = options;
                    }
                }
            }

            // 组装考试详情
            var examDetail = new ClientExamDetailDto
            {
                Id = examSetting.Id,
                RecordId = examRecord.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = examRecord.StartTime,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                AttemptNumber = examRecord.AttemptNumber,
                AllowedAttempts = examSetting.AllowedAttempts,
                Questions = questions.Select(q => new ClientExamQuestionDto
                {
                    Id = q.Id,
                    QuestionId = q.QuestionId,
                    QuestionVersionId = q.QuestionVersionId,
                    Content = q.QuestionVersion.Content,
                    Type = q.Question.Type.ToString(),
                    Options = string.Join(",", q.QuestionVersion.Options),
                    Score = q.Score,
                    SequenceNumber = q.OrderNumber,
                    IsRequired = q.IsRequired
                })
                .OrderBy(q => q.Type)
                .ToList()
            };

            return examDetail;
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "获取考试详情时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试基本信息（客户端视图）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试基本信息</returns>
    public async Task<ClientExamBasicInfoDto> GetExamBasicInfoForClientAsync(long examId, long studentId, long? recordId = null)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new ArgumentException("考试不存在", nameof(examId));
            }

            // 检查考试时间
            var now = DateTime.UtcNow;
            if (examSetting.StartTime > now || examSetting.EndTime < now)
            {
                throw new InvalidOperationException("不在考试时间范围内");
            }

            // 检查是否有权限参加考试
            var studentGroups = await _context.StudentGroupMappings
                .Where(m => m.StudentId == studentId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            var hasPermission = !examSetting.StudentGroups.Any() ||
                                examSetting.StudentGroups.Any(g => studentGroups.Contains(g.Id));

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("无权参加此考试");
            }

            // 查找进行中的考试记录
            var existingRecord = recordId.HasValue
                ? await _context.ExamRecords
                    .Where(r => r.Id == recordId.Value)
                    .FirstOrDefaultAsync()
                : await _context.ExamRecords
                    .Where(r => r.ExamSettingId == examId && 
                          r.StudentId == studentId && 
                          r.Status == ExamRecordStatus.InProgress)
                    .OrderByDescending(r => r.StartTime)
                    .FirstOrDefaultAsync();

            // 组装考试基本信息
            var examBasicInfo = new ClientExamBasicInfoDto
            {
                Id = examSetting.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = existingRecord?.StartTime ?? now,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                RecordId = existingRecord?.Id,
                AllowedScreenSwitchCount = examSetting.AllowedScreenSwitchCount,
                ScreenSwitchCount = existingRecord?.ScreenSwitchCount ?? 0,
                EnableViewResult = examSetting.EnableViewResult
            };

            return examBasicInfo;
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not InvalidOperationException &&
            ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "获取考试基本信息时发生错误");
            throw;
        }
    }
} 