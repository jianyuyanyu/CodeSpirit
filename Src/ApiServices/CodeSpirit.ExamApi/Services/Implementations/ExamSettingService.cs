using AutoMapper;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Caching;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.ExamApi.Extensions;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考试设置服务实现
/// </summary>
public class ExamSettingService : BaseCRUDService<ExamSetting, ExamSettingDto, long, CreateExamSettingDto, UpdateExamSettingDto>, IExamSettingService, IScopedDependency
{
    private readonly IRepository<ExamSetting> _repository;
    private readonly IRepository<ExamPaper> _examPaperRepository;
    private readonly IRepository<StudentGroup> _studentGroupRepository;
    private readonly IRepository<ExamSettingStudentGroup> _examSettingStudentGroupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ExamSettingService> _logger;
    private readonly ExamDbContext _context;
    private readonly IExamCacheService _examCacheService;
    private readonly ICacheService _cacheService;
    private readonly IExamDataScopeService _dataScopeService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">考试设置仓储</param>
    /// <param name="examPaperRepository">试卷仓储</param>
    /// <param name="studentGroupRepository">学生组仓储</param>
    /// <param name="examSettingStudentGroupRepository">考试设置学生组仓储</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="context">数据库上下文</param>
    /// <param name="examCacheService">考试缓存服务</param>
    /// <param name="cacheService">缓存服务</param>
    public ExamSettingService(
        IRepository<ExamSetting> repository,
        IRepository<ExamPaper> examPaperRepository,
        IRepository<StudentGroup> studentGroupRepository,
        IRepository<ExamSettingStudentGroup> examSettingStudentGroupRepository,
        IMapper mapper,
        ILogger<ExamSettingService> logger,
        ExamDbContext context,
        IExamCacheService examCacheService,
        ICacheService cacheService,
        IExamDataScopeService dataScopeService)
        : base(repository, mapper)
    {
        _repository = repository;
        _examPaperRepository = examPaperRepository;
        _studentGroupRepository = studentGroupRepository;
        _examSettingStudentGroupRepository = examSettingStudentGroupRepository;
        _mapper = mapper;
        _logger = logger;
        _context = context;
        _examCacheService = examCacheService;
        _cacheService = cacheService;
        _dataScopeService = dataScopeService;
    }

    /// <summary>
    /// 获取考试设置分页列表
    /// </summary>
    public async Task<PageList<ExamSettingDto>> GetExamSettingsAsync(ExamSettingQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<ExamSetting>(true);

        // 数据可见性过滤：非 Admin/exam_view_all 用户仅能查看自己创建的考试
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return new PageList<ExamSettingDto>([], 0);
            }
            predicate = predicate.And(x => x.CreatedBy == userId.Value);
        }

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

        // 使用投影查询，只获取需要的数据，避免笛卡尔积和过度加载
        var query = _repository.CreateQuery()
            .Where(predicate)
            .Select(x => new 
            {
                // 考试设置基本信息
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                ExamPaperId = x.ExamPaperId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Duration = x.Duration,
                AllowedAttempts = x.AllowedAttempts,
                EnableRandomQuestionOrder = x.EnableRandomQuestionOrder,
                EnableRandomOptionOrder = x.EnableRandomOptionOrder,
                AllowedScreenSwitchCount = x.AllowedScreenSwitchCount,
                EnableViewResult = x.EnableViewResult,
                MinExamTime = x.MinExamTime,
                EnableQuestionAnalysis = x.EnableQuestionAnalysis,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                
                // 试卷信息
                ExamPaperName = x.ExamPaper.Name,
                PassScore = x.ExamPaper.PassScore,
                TotalScore = x.ExamPaper.TotalScore,
                
                // 学生分组信息（在数据库层面计算考生数量）
                StudentGroupsInfo = x.StudentGroups.Select(sg => new
                {
                    Id = sg.StudentGroup.Id,
                    Name = sg.StudentGroup.Name,
                    Description = sg.StudentGroup.Description,
                    StudentCount = sg.StudentGroup.Students.Count, // SQL: COUNT(*)
                    UpdatedAt = sg.StudentGroup.UpdatedAt
                }).ToList(),
                
                // 考试统计信息（在数据库层面计算）
                // 只统计已提交和已批改的考试记录，不包括进行中的
                ExamRecordsCount = x.ExamRecords.Count(r => r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded),
                PassedCount = x.ExamRecords.Count(r => (r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded) && r.Score >= x.ExamPaper.PassScore)
            });

        // 通过率范围筛选
        // 注意：由于通过率是计算得出的值，我们需要在内存中进行筛选
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        // 计算并筛选通过率
        if (queryDto.MinPassRate.HasValue || queryDto.MaxPassRate.HasValue)
        {
            items = items.Where(x =>
            {
                if (x.ExamRecordsCount == 0) return false;
                var passRate = (decimal)x.PassedCount / x.ExamRecordsCount * 100;
                return (!queryDto.MinPassRate.HasValue || passRate >= queryDto.MinPassRate.Value) &&
                       (!queryDto.MaxPassRate.HasValue || passRate <= queryDto.MaxPassRate.Value);
            }).ToList();
        }

        // 计算总数并进行分页
        var total = items.Count;
        items = items
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToList();

        // 手动映射到 DTO
        var dtos = items.Select(item => new ExamSettingDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            ExamPaperId = item.ExamPaperId,
            ExamPaperName = item.ExamPaperName,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            Duration = item.Duration,
            AllowedAttempts = item.AllowedAttempts,
            EnableRandomQuestionOrder = item.EnableRandomQuestionOrder,
            EnableRandomOptionOrder = item.EnableRandomOptionOrder,
            AllowedScreenSwitchCount = item.AllowedScreenSwitchCount,
            EnableViewResult = item.EnableViewResult,
            MinExamTime = item.MinExamTime,
            EnableQuestionAnalysis = item.EnableQuestionAnalysis,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            
            // 学生分组信息（考生数量已在数据库层面计算）
            StudentGroups = item.StudentGroupsInfo.Select(sg => new ExamSettingStudentGroupDto
            {
                Name = sg.Name,
                Description = sg.Description,
                StudentCount = sg.StudentCount,
                UpdatedAt = sg.UpdatedAt
            }).ToList(),
            StudentGroupIds = item.StudentGroupsInfo.Select(sg => sg.Id).ToList(),
            
            // 考试统计信息
            TotalParticipants = item.ExamRecordsCount,
            PassedParticipants = item.PassedCount,
            PassRate = item.ExamRecordsCount > 0 
                ? Math.Round((decimal)item.PassedCount / item.ExamRecordsCount * 100, 2)
                : null
        }).ToList();

        return new PageList<ExamSettingDto>(dtos, total);
    }

    /// <summary>
    /// 获取考试设置详情
    /// </summary>
    public async Task<ExamSettingDto> GetExamSettingDetailAsync(long id)
    {
        // 使用投影查询，只获取需要的数据
        var query = _repository.CreateQuery().Where(x => x.Id == id);

        // 数据可见性过滤：非 Admin/exam_view_all 用户仅能查看自己创建的考试
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null!;
            }
            query = query.Where(x => x.CreatedBy == userId.Value);
        }

        var result = await query
            .Select(x => new 
            {
                // 考试设置基本信息
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                ExamPaperId = x.ExamPaperId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Duration = x.Duration,
                AllowedAttempts = x.AllowedAttempts,
                EnableRandomQuestionOrder = x.EnableRandomQuestionOrder,
                EnableRandomOptionOrder = x.EnableRandomOptionOrder,
                AllowedScreenSwitchCount = x.AllowedScreenSwitchCount,
                EnableViewResult = x.EnableViewResult,
                MinExamTime = x.MinExamTime,
                EnableQuestionAnalysis = x.EnableQuestionAnalysis,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                
                // 试卷信息
                ExamPaperName = x.ExamPaper.Name,
                PassScore = x.ExamPaper.PassScore,
                TotalScore = x.ExamPaper.TotalScore,
                
                // 学生分组信息（在数据库层面计算考生数量）
                StudentGroupsInfo = x.StudentGroups.Select(sg => new
                {
                    Id = sg.StudentGroup.Id,
                    Name = sg.StudentGroup.Name,
                    Description = sg.StudentGroup.Description,
                    StudentCount = sg.StudentGroup.Students.Count, // SQL: COUNT(*)
                    UpdatedAt = sg.StudentGroup.UpdatedAt
                }).ToList(),
                
                // 考试统计信息（在数据库层面计算）
                // 只统计已提交和已批改的考试记录，不包括进行中的
                ExamRecordsCount = x.ExamRecords.Count(r => r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded),
                PassedCount = x.ExamRecords.Count(r => (r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded) && r.Score >= x.ExamPaper.PassScore)
            })
            .FirstOrDefaultAsync();

        if (result == null)
        {
            return null!;
        }

        // 手动映射到 DTO
        return new ExamSettingDto
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description,
            ExamPaperId = result.ExamPaperId,
            ExamPaperName = result.ExamPaperName,
            StartTime = result.StartTime,
            EndTime = result.EndTime,
            Duration = result.Duration,
            AllowedAttempts = result.AllowedAttempts,
            EnableRandomQuestionOrder = result.EnableRandomQuestionOrder,
            EnableRandomOptionOrder = result.EnableRandomOptionOrder,
            AllowedScreenSwitchCount = result.AllowedScreenSwitchCount,
            EnableViewResult = result.EnableViewResult,
            MinExamTime = result.MinExamTime,
            EnableQuestionAnalysis = result.EnableQuestionAnalysis,
            Status = result.Status,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt,
            
            // 学生分组信息（考生数量已在数据库层面计算）
            StudentGroups = result.StudentGroupsInfo.Select(sg => new ExamSettingStudentGroupDto
            {
                Name = sg.Name,
                Description = sg.Description,
                StudentCount = sg.StudentCount,
                UpdatedAt = sg.UpdatedAt
            }).ToList(),
            StudentGroupIds = result.StudentGroupsInfo.Select(sg => sg.Id).ToList(),
            
            // 考试统计信息
            TotalParticipants = result.ExamRecordsCount,
            PassedParticipants = result.PassedCount,
            PassRate = result.ExamRecordsCount > 0 
                ? Math.Round((decimal)result.PassedCount / result.ExamRecordsCount * 100, 2)
                : null
        };
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

        // 关联数据可见性校验：非 Admin/exam_view_all 用户只能使用自己创建的试卷和考生组
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                throw new AppServiceException(403, "未认证用户无法创建考试");
            }
            if (examPaper.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "试卷不存在");
            }
            if (studentGroups.Any(sg => sg.CreatedBy != userId.Value))
            {
                throw new AppServiceException(404, "部分学生分组不存在");
            }
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

        // 数据可见性校验：非 Admin/exam_view_all 用户仅能操作自己创建的考试
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue || examSetting.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "考试设置不存在");
            }
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

        // 关联数据可见性校验：非 Admin/exam_view_all 用户只能使用自己创建的试卷和考生组
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue || examPaper.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "试卷不存在");
            }
            if (studentGroups.Any(sg => sg.CreatedBy != userId.Value))
            {
                throw new AppServiceException(404, "部分学生分组不存在");
            }
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
        
        // 清理相关缓存
        try
        {
            await _examCacheService.ClearExamCacheAsync(id);
            _logger.LogDebug("已清理考试设置更新后的缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理考试设置更新后的缓存失败: {ExamId}", id);
        }
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

        // 数据可见性校验
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue || examSetting.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "考试设置不存在");
            }
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的考试设置可以删除");
        }

        await _repository.DeleteAsync(examSetting);
        
        // 清理相关缓存
        try
        {
            await _examCacheService.ClearExamCacheAsync(id);
            _logger.LogDebug("已清理考试设置删除后的缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理考试设置删除后的缓存失败: {ExamId}", id);
        }
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

        // 数据可见性校验
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue || examSetting.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "考试设置不存在");
            }
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
        
        // ✅ 发布时清除共享考试列表缓存，确保新发布的考试能被查询到
        try
        {
            await _examCacheService.ClearSharedAvailableExamsCacheAsync();
            _logger.LogDebug("已清除共享考试列表缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清除共享考试列表缓存失败: {ExamId}", id);
        }
        
        // 发布时预热缓存（仅预热即将开始的考试）
        try
        {
            await _examCacheService.WarmupExamCacheAsync(id);
            _logger.LogDebug("已尝试预热考试发布后的缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预热考试发布后的缓存失败: {ExamId}", id);
        }
        
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

        // 数据可见性校验
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue || examSetting.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "考试设置不存在");
            }
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
        
        // 取消发布时清理缓存
        try
        {
            await _examCacheService.ClearExamCacheAsync(id);
            _logger.LogDebug("已清理考试取消发布后的缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理考试取消发布后的缓存失败: {ExamId}", id);
        }
    }

    /// <summary>
    /// 获取用户可参加的考试列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>可参加的考试列表</returns>
    public async Task<List<ClientExamDto>> GetAvailableExamsForClientAsync(long userId, long studentId)
    {
        try
        {
            _logger.LogDebug("开始获取学生可参加的考试列表: UserId={UserId}, StudentId={StudentId}", userId, studentId);
            
            // ✅ 第1步：从缓存获取学生档案信息（包含学生组ID列表）
            var clientProfile = await _examCacheService.GetClientProfileWithCacheAsync(userId);
            var studentGroups = clientProfile.StudentGroupIds;
            
            _logger.LogDebug("从缓存获取学生所属的学生组数量: {Count}, StudentId={StudentId}", studentGroups.Count, studentId);

            // ✅ 第2步：从共享缓存获取可用考试列表（所有学生共享）
            var sharedExams = await _examCacheService.GetSharedAvailableExamsWithCacheAsync();
            
            _logger.LogDebug("从共享缓存获取到考试数量: {Count}", sharedExams.Count);

            // ✅ 第3步：在内存中根据学生组过滤考试
            var filteredExams = sharedExams
                .Where(e => e.IsOpenToAll || e.StudentGroupIds.Any(gid => studentGroups.Contains(gid)))
                .ToList();
            
            _logger.LogDebug("根据学生组过滤后的考试数量: {Count}, StudentId={StudentId}", filteredExams.Count, studentId);

            if (!filteredExams.Any())
            {
                _logger.LogDebug("没有可参加的考试，StudentId={StudentId}", studentId);
                return new List<ClientExamDto>();
            }

            // ✅ 第4步：在内存中根据考试时间和已完成次数过滤（开始考试时会有专门的判断逻辑）
            var now = DateTime.UtcNow;
            var result = new List<ClientExamDto>();
            
            foreach (var e in filteredExams)
            {
                // ✅ 检查已完成次数（如果有次数限制）
                if (e.AllowedAttempts > 0)
                {
                    var completedCountCacheKey = new ExamCacheOptions.CompletedExamCount(e.Id, studentId);
                    int? completedCountValue = await _cacheService.GetAsync<int>(completedCountCacheKey.Key);
                    var completedCount = completedCountValue ?? 0;
                    
                    // 如果已达到次数上限，跳过该考试
                    if (completedCount >= e.AllowedAttempts)
                    {
                        _logger.LogWarning("学生已达考试次数上限，跳过: ExamId={ExamId}, StudentId={StudentId}, CompletedCount={CompletedCount}, AllowedAttempts={AllowedAttempts}", 
                            e.Id, studentId, completedCount, e.AllowedAttempts);
                        continue;
                    }
                }
                
                // 根据考试时间确定状态
                string status;
                if (e.StartTime <= now && e.EndTime >= now)
                {
                    status = "进行中";
                }
                else if (e.StartTime > now)
                {
                    status = "未开始";
                }
                else
                {
                    status = "已结束";
                }

                result.Add(new ClientExamDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Duration = e.Duration,
                    TotalScore = e.TotalScore,
                    Status = status,
                    HasResult = false // 开始考试时会进行详细判断
                });
            }

            _logger.LogInformation("成功获取学生可参加的考试列表，考试数量: {Count}, StudentId={StudentId}", result.Count, studentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可参加的考试列表时发生错误，StudentId={StudentId}", studentId);
            throw;
        }
    }

    /// <summary>
    /// 获取考试题目及答案（轻量级方法，复用缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>按用户顺序排列的题目列表（包含答案）</returns>
    public async Task<List<ClientExamQuestionDto>> GetExamQuestionsWithAnswersAsync(long examId, long recordId)
    {
        try
        {
            // ⚡ 性能优化：并行获取答题记录和题目数据（两者无依赖关系）
            var answerRecordsTask = _context.ExamAnswerRecords.AsNoTracking()
                .Where(a => a.ExamRecordId == recordId)
                .OrderBy(a => a.OrderNumber)
                .ToListAsync();

            var questionDataTask = _examCacheService.GetExamQuestionsDataWithCacheAsync(examId);

            await Task.WhenAll(answerRecordsTask, questionDataTask);

            var answerRecords = await answerRecordsTask;
            var questionDataDict = await questionDataTask;
                
            if (!answerRecords.Any())
            {
                _logger.LogError("考试记录 {RecordId} 没有答题记录，无法构建题目列表", recordId);
                throw new BusinessException("考试记录没有答题记录");
            }

            if (questionDataDict == null || !questionDataDict.Any())
            {
                _logger.LogError("无法获取考试 {ExamId} 的题目数据", examId);
                throw new BusinessException("无法获取考试题目数据");
            }

            // 按照用户的 AnswerRecord.OrderNumber 排序构建题目列表，并填充答案
            var questions = answerRecords
                .Where(a => questionDataDict.ContainsKey(a.QuestionId))
                .Select((answerRecord, index) =>
                {
                    var cachedQuestion = questionDataDict[answerRecord.QuestionId];
                    // 创建新对象，避免修改缓存中的数据
                    return new ClientExamQuestionDto
                    {
                        Id = cachedQuestion.Id,
                        QuestionId = cachedQuestion.QuestionId,
                        QuestionVersionId = cachedQuestion.QuestionVersionId,
                        Content = cachedQuestion.Content,
                        Type = cachedQuestion.Type,
                        Options = cachedQuestion.Options,
                        Score = cachedQuestion.Score,
                        IsRequired = cachedQuestion.IsRequired,
                        SequenceNumber = index + 1,  // 使用连续的序号
                        Answer = answerRecord.Answer ?? string.Empty  // 填充用户的答案
                    };
                })
                .ToList();

            _logger.LogInformation("获取考试题目及答案完成，考试ID: {ExamId}, 记录ID: {RecordId}, 题目数: {QuestionCount}",
                examId, recordId, questions.Count);

            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试题目及答案失败，考试ID: {ExamId}, 记录ID: {RecordId}", examId, recordId);
            throw;
        }
    }

    public async Task<ClientExamDetailDto> GetExamDetailForClientAsync(long examId, long recordId)
    {
        try
        {
            // 获取考试基本信息
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new BusinessException("考试不存在");
            }

            // 获取考试记录（包含答题记录，其中保存了用户的题目顺序）
            var examRecord = await _context.ExamRecords
                .Include(r => r.AnswerRecords)
                .Where(r => r.Id == recordId)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new BusinessException("考试记录不存在");
            }
            
            // ⚠️ 如果AnswerRecords为空，可能是因为刚创建记录，需要重新加载
            if (examRecord.AnswerRecords == null || !examRecord.AnswerRecords.Any())
            {
                _logger.LogWarning("考试记录 {RecordId} 的答题记录为空，尝试重新加载", recordId);
                
                // 重新从数据库加载答题记录
                examRecord.AnswerRecords = await _context.ExamAnswerRecords
                    .Where(a => a.ExamRecordId == recordId)
                    .ToListAsync();
                    
                if (!examRecord.AnswerRecords.Any())
                {
                    _logger.LogError("考试记录 {RecordId} 没有答题记录，无法构建题目列表", recordId);
                    throw new BusinessException("考试记录没有答题记录");
                }
            }

            // 使用统一的缓存组件获取题目数据
            var questionDataDict = await _examCacheService.GetExamQuestionsDataWithCacheAsync(examId);
            if (questionDataDict == null || !questionDataDict.Any())
            {
                _logger.LogError("无法获取考试 {ExamId} 的题目数据", examId);
                throw new BusinessException("无法获取考试题目数据");
            }

            // ✅ 使用已保存的 OrderNumber，而不是每次重新随机
            // 题目顺序在创建 ExamRecord 时已经确定并保存在 AnswerRecord.OrderNumber 中
            
            // 按照用户的 AnswerRecord.OrderNumber 排序构建题目列表，然后再按题型分组排序
            var questions = examRecord.AnswerRecords
                .OrderBy(a => a.OrderNumber)
                .Where(a => questionDataDict.ContainsKey(a.QuestionId))
                .Select(answerRecord =>
                {
                    var cachedQuestion = questionDataDict[answerRecord.QuestionId];
                    // 创建新对象，避免修改缓存中的数据
                    return new ClientExamQuestionDto
                    {
                        Id = cachedQuestion.Id,
                        QuestionId = cachedQuestion.QuestionId,
                        QuestionVersionId = cachedQuestion.QuestionVersionId,
                        Content = cachedQuestion.Content,
                        Type = cachedQuestion.Type,
                        Options = cachedQuestion.Options,
                        Score = cachedQuestion.Score,
                        IsRequired = cachedQuestion.IsRequired,
                        SequenceNumber = answerRecord.OrderNumber  // 设置用户特定的顺序号
                    };
                })
                .ToList();
            
            // 重新分配连续的序号
            for (int i = 0; i < questions.Count; i++)
            {
                questions[i].SequenceNumber = i + 1;
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
                Questions = questions
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
    /// 获取考试轻量信息（客户端视图，用于倒计时页面）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试轻量信息</returns>
    public async Task<ClientExamLightInfoDto> GetExamLightInfoForClientAsync(long examId, long studentId)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .ThenInclude(p => p.ExamPaperQuestions)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new ArgumentException("考试不存在", nameof(examId));
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

            // 组装考试轻量信息
            var lightInfo = new ClientExamLightInfoDto
            {
                Id = examSetting.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = examSetting.StartTime,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                QuestionCount = examSetting.ExamPaper.ExamPaperQuestions.Count,
                ServerTime = DateTime.UtcNow,
                Status = string.Empty, // 将由ClientService设置
                CanStart = false // 将由ClientService设置
            };

            return lightInfo;
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "获取考试轻量信息时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试信息用于缓存预热（不进行学生权限验证）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试基本配置信息，如果考试不存在则返回null</returns>
    public async Task<ExamBasicInfoCacheDto?> GetExamInfoForWarmupAsync(long examId)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                return null;
            }

            // 只返回共享的考试配置信息，不包含用户特定的数据
            return new ExamBasicInfoCacheDto
            {
                Id = examSetting.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = examSetting.StartTime,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                AllowedScreenSwitchCount = examSetting.AllowedScreenSwitchCount,
                EnableViewResult = examSetting.EnableViewResult,
                MinExamTime = examSetting.MinExamTime,
                AllowedAttempts = examSetting.AllowedAttempts,
                EnableRandomQuestionOrder = examSetting.EnableRandomQuestionOrder,
                EnableRandomOptionOrder = examSetting.EnableRandomOptionOrder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试信息用于预热时发生错误，考试ID: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 获取考试题目数据用于缓存预热（不进行学生权限验证）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>题目数据字典（QuestionId -> 题目详情），如果考试不存在则返回null</returns>
    public async Task<Dictionary<long, ClientExamQuestionDto>?> GetExamQuestionsForWarmupAsync(long examId)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .ThenInclude(p => p.ExamPaperQuestions)
                .ThenInclude(q => q.Question)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                return null;
            }

            // 加载题目版本信息
            foreach (var paperQuestion in examSetting.ExamPaper.ExamPaperQuestions)
            {
                await _context.Entry(paperQuestion)
                    .Reference(q => q.QuestionVersion)
                    .LoadAsync();
            }

            // 构建题目字典（QuestionId -> 题目详情）
            // 注意：这里只包含题目内容，不包含用户特定的顺序
            var questionsDict = examSetting.ExamPaper.ExamPaperQuestions
                .Where(q => q.Question != null && q.QuestionVersion != null)
                .ToDictionary(
                    q => q.QuestionId,
                    q => new ClientExamQuestionDto
                    {
                        Id = q.Id,
                        QuestionId = q.QuestionId,
                        QuestionVersionId = q.QuestionVersionId,
                        Content = q.QuestionVersion.Content,
                        Type = q.Question.Type.ToString(),
                        Options = q.QuestionVersion.Options
                            .Select(option => new OptionDisplayDto { Label = option, Value = option })
                            .ToList(),
                        Score = q.Score,
                        IsRequired = q.IsRequired,
                        // SequenceNumber 不在预热时设置，因为它是用户特定的
                        SequenceNumber = 0
                    }
                );

            _logger.LogDebug("成功获取考试题目数据用于预热，考试ID: {ExamId}, 题目数: {Count}", 
                examId, questionsDict.Count);

            return questionsDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试题目数据用于预热时发生错误，考试ID: {ExamId}", examId);
            throw;
        }
    }

}