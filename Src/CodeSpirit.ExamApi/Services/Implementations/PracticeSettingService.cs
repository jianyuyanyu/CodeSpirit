using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.PracticeSetting;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 练习设置服务实现
/// </summary>
public class PracticeSettingService : BaseCRUDService<PracticeSetting, PracticeSettingDto, long, CreatePracticeSettingDto, UpdatePracticeSettingDto>, IPracticeSettingService
{
    private readonly IRepository<PracticeSetting> _repository;
    private readonly IRepository<ExamPaper> _examPaperRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PracticeSettingService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">练习设置仓储</param>
    /// <param name="examPaperRepository">试卷仓储</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="logger">日志记录器</param>
    public PracticeSettingService(
        IRepository<PracticeSetting> repository,
        IRepository<ExamPaper> examPaperRepository,
        IMapper mapper,
        ILogger<PracticeSettingService> logger)
        : base(repository, mapper)
    {
        _repository = repository;
        _examPaperRepository = examPaperRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 获取练习设置分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>练习设置分页列表</returns>
    public async Task<PageList<PracticeSettingDto>> GetPracticeSettingsAsync(PracticeSettingQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<PracticeSetting>(true);

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            predicate = predicate.And(x => x.Name.Contains(queryDto.Keywords) ||
                                          (x.Description != null && x.Description.Contains(queryDto.Keywords)));
        }

        // 名称筛选
        if (!string.IsNullOrWhiteSpace(queryDto.Name))
        {
            predicate = predicate.And(x => x.Name.Contains(queryDto.Name));
        }

        // 试卷ID筛选
        if (queryDto.ExamPaperId.HasValue)
        {
            predicate = predicate.And(x => x.ExamPaperId == queryDto.ExamPaperId.Value);
        }

        // 练习模式筛选
        if (queryDto.PracticeMode.HasValue)
        {
            predicate = predicate.And(x => x.PracticeMode == queryDto.PracticeMode.Value);
        }

        // 状态筛选
        if (queryDto.Status.HasValue)
        {
            predicate = predicate.And(x => x.Status == queryDto.Status.Value);
        }

        var query = _repository.CreateQuery()
            .Include(x => x.ExamPaper)
            .Where(predicate)
            .OrderByDescending(x => x.CreatedAt);

        return await GetPagedListAsync(
            queryDto.Page,
            queryDto.PerPage,
            predicate,
            queryDto.OrderBy,
            queryDto.OrderDir,
            "ExamPaper");
    }

    /// <summary>
    /// 创建练习设置
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <returns>创建后的练习设置</returns>
    public override async Task<PracticeSettingDto> CreateAsync(CreatePracticeSettingDto createDto)
    {
        // 验证试卷是否存在
        var examPaper = await _examPaperRepository.GetByIdAsync(createDto.ExamPaperId);
        if (examPaper == null)
        {
            throw new AppServiceException(404, "试卷不存在");
        }

        // 验证试卷状态
        if (examPaper.Status != ExamPaperStatus.Published)
        {
            throw new AppServiceException(400, "只能使用已发布的试卷");
        }

        // 创建练习设置
        var practiceSetting = _mapper.Map<PracticeSetting>(createDto);
        practiceSetting.Status = PracticeSettingStatus.Draft;
        
        await _repository.AddAsync(practiceSetting);

        // 使用基类的GetAsync方法
        return await GetAsync(practiceSetting.Id);
    }

    /// <summary>
    /// 更新练习设置
    /// </summary>
    /// <param name="id">ID</param>
    /// <param name="updateDto">更新DTO</param>
    public override async Task UpdateAsync(long id, UpdatePracticeSettingDto updateDto)
    {
        var practiceSetting = await _repository.GetByIdAsync(id);
        if (practiceSetting == null)
        {
            throw new AppServiceException(404, "练习设置不存在");
        }

        // 检查练习设置状态
        if (practiceSetting.Status != PracticeSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的练习设置可以更新");
        }

        // 更新基本信息
        _mapper.Map(updateDto, practiceSetting);

        await _repository.UpdateAsync(practiceSetting);
    }

    /// <summary>
    /// 删除练习设置
    /// </summary>
    /// <param name="id">ID</param>
    public override async Task DeleteAsync(long id)
    {
        var practiceSetting = await _repository.GetByIdAsync(id);
        if (practiceSetting == null)
        {
            return;
        }

        // 检查练习设置状态
        if (practiceSetting.Status != PracticeSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的练习设置可以删除");
        }

        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// 发布练习设置
    /// </summary>
    /// <param name="id">练习设置ID</param>
    public async Task PublishPracticeSettingAsync(long id)
    {
        var practiceSetting = await _repository.GetByIdAsync(id);
        if (practiceSetting == null)
        {
            throw new AppServiceException(404, "练习设置不存在");
        }

        // 检查练习设置状态
        if (practiceSetting.Status != PracticeSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的练习设置可以发布");
        }

        // 检查试卷状态
        var examPaper = await _examPaperRepository.GetByIdAsync(practiceSetting.ExamPaperId);
        if (examPaper == null || examPaper.Status != ExamPaperStatus.Published)
        {
            throw new AppServiceException(400, "试卷未发布，不能发布练习设置");
        }

        practiceSetting.Status = PracticeSettingStatus.Published;
        await _repository.UpdateAsync(practiceSetting);
    }

    /// <summary>
    /// 禁用练习设置
    /// </summary>
    /// <param name="id">练习设置ID</param>
    public async Task DisablePracticeSettingAsync(long id)
    {
        var practiceSetting = await _repository.GetByIdAsync(id);
        if (practiceSetting == null)
        {
            throw new AppServiceException(404, "练习设置不存在");
        }

        // 检查练习设置状态
        if (practiceSetting.Status != PracticeSettingStatus.Published)
        {
            throw new AppServiceException(400, "只有已发布状态的练习设置可以禁用");
        }

        practiceSetting.Status = PracticeSettingStatus.Disabled;
        await _repository.UpdateAsync(practiceSetting);
    }

    /// <summary>
    /// 启用练习设置
    /// </summary>
    /// <param name="id">练习设置ID</param>
    public async Task EnablePracticeSettingAsync(long id)
    {
        var practiceSetting = await _repository.GetByIdAsync(id);
        if (practiceSetting == null)
        {
            throw new AppServiceException(404, "练习设置不存在");
        }

        // 检查练习设置状态
        if (practiceSetting.Status != PracticeSettingStatus.Disabled)
        {
            throw new AppServiceException(400, "只有已禁用状态的练习设置可以启用");
        }

        // 检查试卷状态
        var examPaper = await _examPaperRepository.GetByIdAsync(practiceSetting.ExamPaperId);
        if (examPaper == null || examPaper.Status != ExamPaperStatus.Published)
        {
            throw new AppServiceException(400, "试卷未发布，不能启用练习设置");
        }

        practiceSetting.Status = PracticeSettingStatus.Published;
        await _repository.UpdateAsync(practiceSetting);
    }

    /// <summary>
    /// 获取试卷的所有练习设置
    /// </summary>
    /// <param name="examPaperId">试卷ID</param>
    /// <returns>练习设置列表</returns>
    public async Task<List<PracticeSettingDto>> GetPracticeSettingsByExamPaperIdAsync(long examPaperId)
    {
        var practiceSettings = await _repository.Find(x => x.ExamPaperId == examPaperId)
            .Include(x => x.ExamPaper)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<PracticeSettingDto>>(practiceSettings);
    }
} 