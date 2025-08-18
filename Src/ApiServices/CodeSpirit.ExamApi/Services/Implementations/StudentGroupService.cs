using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.StudentGroup;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考生组服务实现
/// </summary>
public class StudentGroupService : BaseCRUDIService<StudentGroup, StudentGroupDto, long, CreateStudentGroupDto, UpdateStudentGroupDto, StudentGroupBatchImportDto>, IStudentGroupService, IScopedDependency
{
    private readonly IRepository<Student> _studentRepository;
    private readonly IRepository<StudentGroupMapping> _mappingRepository;
    private readonly ILogger<StudentGroupService> _logger;
    private readonly IIdGenerator _idGenerator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StudentGroupService(
        IRepository<StudentGroup> repository,
        IRepository<Student> studentRepository,
        IRepository<StudentGroupMapping> mappingRepository,
        IMapper mapper,
        ILogger<StudentGroupService> logger,
        IIdGenerator idGenerator)
        : base(repository, mapper)
    {
        _studentRepository = studentRepository;
        _mappingRepository = mappingRepository;
        _logger = logger;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// 获取题目分页列表
    /// </summary>
    public async Task<PageList<StudentGroupDto>> GetStudentGroupsAsync(StudentGroupQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<StudentGroup>(true);

        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            predicate = predicate.Or(x => x.Name.Contains(queryDto.Keywords));
            predicate = predicate.Or(x => x.Description != null && x.Description.Contains(queryDto.Keywords));
        }

        return await GetPagedListAsync(
            queryDto,
            predicate,
            includes: ["Students"]
        );
    }

    /// <summary>
    /// 删除考生组重写
    /// </summary>
    public override async Task DeleteAsync(long id)
    {
        // 检查考生组是否存在
        var group = await Repository
            .Find(g => g.Id == id)
            .Include(g => g.ExamSettings)
            .FirstOrDefaultAsync();

        if (group == null)
        {
            throw new AppServiceException(404, "考生组不存在！");
        }

        // 检查是否有关联的考试
        if (group.ExamSettings.Any())
        {
            throw new AppServiceException(400, "该考生组已关联考试，无法删除！");
        }

        try
        {
            // 删除考生映射关系
            await _mappingRepository
                .Find(x => x.StudentGroupId == id)
                .ExecuteDeleteAsync();

            await Repository.DeleteAsync(group);
            await Repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除考生组失败: {Id}", id);
            throw new AppServiceException(500, "删除考生组失败！");
        }
    }

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreateStudentGroupDto createDto)
    {
        // 验证名称是否重复
        var exists = await Repository.Find(x => x.Name == createDto.Name).AnyAsync();
        if (exists)
        {
            throw new AppServiceException(400, "考生组名称已存在！");
        }
    }

    /// <summary>
    /// 验证更新DTO
    /// </summary>
    protected override async Task ValidateUpdateDto(long id, UpdateStudentGroupDto updateDto)
    {
        // 验证名称是否重复
        var exists = await Repository
            .Find(x => x.Id != id && x.Name == updateDto.Name)
            .AnyAsync();
        if (exists)
        {
            throw new AppServiceException(400, "考生组名称已存在！");
        }
    }

    protected override Task OnCreating(StudentGroup entity, CreateStudentGroupDto createDto)
    {
        entity.Id = _idGenerator.NewId();
        return base.OnCreating(entity, createDto);
    }

    /// <summary>
    /// 创建实体后的处理
    /// </summary>
    protected override async Task OnCreated(StudentGroup entity, CreateStudentGroupDto createDto)
    {
        if (createDto.StudentIds.Any())
        {
            await AddStudentsToGroupAsync(entity.Id, createDto.StudentIds);
        }
    }

    /// <summary>
    /// 获取导入项ID
    /// </summary>
    protected override string GetImportItemId(StudentGroupBatchImportDto importDto)
    {
        return importDto.Name;
    }

    /// <summary>
    /// 验证导入项
    /// </summary>
    protected override async Task<IEnumerable<StudentGroupBatchImportDto>> ValidateImportItems(IEnumerable<StudentGroupBatchImportDto> importData)
    {
        var items = importData.ToList();

        // 检查名称是否重复
        var existingNames = await Repository
            .Find(g => items.Select(i => i.Name).Contains(g.Name))
            .Select(g => g.Name)
            .ToListAsync();

        return items.Where(i => !existingNames.Contains(i.Name));
    }

    protected override Task OnImporting(StudentGroup entity)
    {
        base.OnImporting(entity);

        if (entity.Id == default)
            entity.Id = _idGenerator.NewId();

        return Task.CompletedTask;
    }

    /// <summary>
    /// 添加考生到分组
    /// </summary>
    public async Task AddStudentsToGroupAsync(long groupId, List<long> studentIds)
    {
        var group = await Repository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new AppServiceException(404, "考生组不存在！");
        }

        // 验证考生是否存在
        var students = await _studentRepository
            .Find(x => studentIds.Contains(x.Id))
            .ToListAsync();

        if (students.Count != studentIds.Count)
        {
            throw new AppServiceException(400, "部分考生不存在！");
        }

        // 获取已存在的映射
        var existingMappings = await _mappingRepository
            .Find(x => x.StudentGroupId == groupId && studentIds.Contains(x.StudentId))
            .Select(x => x.StudentId)
            .ToListAsync();

        // 创建新的映射
        var newMappings = studentIds
            .Except(existingMappings)
            .Select(studentId => new StudentGroupMapping
            {
                StudentId = studentId,
                StudentGroupId = groupId
            })
            .ToList();

        if (newMappings.Any())
        {
            await _mappingRepository.AddRangeAsync(newMappings);
            await _mappingRepository.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 从分组移除考生
    /// </summary>
    public async Task RemoveStudentsFromGroupAsync(long groupId, List<long> studentIds)
    {
        var group = await Repository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new AppServiceException(404, "考生组不存在！");
        }

        await _mappingRepository
            .Find(x => x.StudentGroupId == groupId && studentIds.Contains(x.StudentId))
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// 获取所有未删除的学生组
    /// </summary>
    public async Task<List<StudentGroupDto>> GetAllActiveGroupsAsync()
    {
        var entities = await Repository.GetAllAsync();
        return Mapper.Map<List<StudentGroupDto>>(entities.Where(x => !x.IsDeleted));
    }
}