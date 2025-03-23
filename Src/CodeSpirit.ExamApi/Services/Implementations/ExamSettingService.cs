using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamSettingService(
        IRepository<ExamSetting> repository,
        IRepository<ExamPaper> examPaperRepository,
        IRepository<StudentGroup> studentGroupRepository,
        IRepository<ExamSettingStudentGroup> examSettingStudentGroupRepository,
        IMapper mapper,
        ILogger<ExamSettingService> logger)
        : base(repository, mapper)
    {
        _repository = repository;
        _examPaperRepository = examPaperRepository;
        _studentGroupRepository = studentGroupRepository;
        _examSettingStudentGroupRepository = examSettingStudentGroupRepository;
        _mapper = mapper;
        _logger = logger;
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

        // 检查时间设置
        if (examSetting.StartTime <= DateTime.Now)
        {
            throw new AppServiceException(400, "考试开始时间必须大于当前时间");
        }

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
} 