using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Humanizer;
using LinqKit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 学生服务实现
/// </summary>
public class StudentService : BaseCRUDIService<Student, StudentDto, long, CreateStudentDto, UpdateStudentDto, StudentBatchImportDto>, IStudentService
{
    private readonly IRepository<Student> _repository;
    private readonly IRepository<StudentGroupMapping> _mappingRepository;
    private readonly IRepository<StudentGroup> _studentGroupRepository;
    private readonly ILogger<StudentService> _logger;
    private readonly IIdGenerator _idGenerator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StudentService(
        IRepository<Student> repository,
        IRepository<StudentGroupMapping> mappingRepository,
        IRepository<StudentGroup> studentGroupRepository,
        IMapper mapper,
        ILogger<StudentService> logger,
        IIdGenerator idGenerator)
        : base(repository, mapper)
    {
        _repository = repository;
        _mappingRepository = mappingRepository;
        _studentGroupRepository = studentGroupRepository;
        _logger = logger;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// 获取学生分页列表
    /// </summary>
    public async Task<PageList<StudentDto>> GetStudentsAsync(StudentQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<Student>(true);

        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            predicate = predicate.Or(x => x.Name.Contains(queryDto.Keywords));
            predicate = predicate.Or(x => x.StudentNumber.Contains(queryDto.Keywords));
            predicate = predicate.Or(x => x.PhoneNumber.Contains(queryDto.Keywords));
        }

        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(x => x.IsActive == queryDto.IsActive.Value);
        }

        if (queryDto.StudentGroupId.HasValue)
        {
            predicate = predicate.And(x => x.StudentGroups.Any(sg => sg.StudentGroupId == queryDto.StudentGroupId.Value));
        }

        // 修改查询
        var query = _repository.CreateQuery()
            .Include(x => x.StudentGroups)
                .ThenInclude(x => x.StudentGroup)
            .Where(predicate);

        // 执行分页查询
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        // 映射结果
        var mappedItems = Mapper.Map<List<StudentDto>>(items);

        return new PageList<StudentDto>
        {
            Total = totalCount,
            Items = mappedItems
        };
    }

    /// <summary>
    /// 通过学号查找学生
    /// </summary>
    public async Task<StudentDto?> GetByStudentNumberAsync(string studentNumber)
    {
        var student = await Repository
            .Find(x => x.StudentNumber == studentNumber)
            .FirstOrDefaultAsync();

        return student != null ? Mapper.Map<StudentDto>(student) : null;
    }

    /// <summary>
    /// 通过用户ID查找学生
    /// </summary>
    public async Task<StudentDto?> GetByUserIdAsync(long userId)
    {
        var student = await Repository
            .Find(x => x.UserId == userId)
            .FirstOrDefaultAsync();

        return student != null ? Mapper.Map<StudentDto>(student) : null;
    }

    /// <summary>
    /// 删除学生重写
    /// </summary>
    public override async Task DeleteAsync(long id)
    {
        // 检查学生是否存在
        var student = await Repository
            .Find(s => s.Id == id)
            .Include(s => s.ExamRecords)
            .FirstOrDefaultAsync();

        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在！");
        }

        // 检查是否有关联的考试记录
        if (student.ExamRecords.Any())
        {
            throw new AppServiceException(400, "该学生已有考试记录，无法删除！");
        }

        try
        {
            // 删除学生分组映射关系
            await _mappingRepository
                .Find(x => x.StudentId == id)
                .ExecuteDeleteAsync();

            await Repository.DeleteAsync(student);
            await Repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除学生失败: {Id}", id);
            throw new AppServiceException(500, "删除学生失败！");
        }
    }

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreateStudentDto createDto)
    {
        // 检查学号是否已存在
        var existsStudentNumber = await Repository
            .Find(x => x.StudentNumber == createDto.StudentNumber)
            .AnyAsync();
        if (existsStudentNumber)
        {
            throw new AppServiceException(400, "学号/工号已存在！");
        }
        
        // 检查用户ID是否已存在
        var existsUserId = await Repository
            .Find(x => x.UserId == createDto.UserId)
            .AnyAsync();
        if (existsUserId)
        {
            throw new AppServiceException(400, "该用户ID已关联学生！");
        }
        
        // 验证学生组是否存在
        if (createDto.StudentGroupIds.Any())
        {
            var existingGroupCount = await _studentGroupRepository
                .Find(x => createDto.StudentGroupIds.Contains(x.Id))
                .CountAsync();
                
            if (existingGroupCount != createDto.StudentGroupIds.Count)
            {
                throw new AppServiceException(400, "部分学生组不存在！");
            }
        }
    }

    protected override Task OnCreating(Student entity, CreateStudentDto createDto)
    {
        entity.Id = _idGenerator.NewId();
        return base.OnCreating(entity, createDto);
    }

    /// <summary>
    /// 创建实体后的处理
    /// </summary>
    protected override async Task OnCreated(Student entity, CreateStudentDto createDto)
    {
        if (createDto.StudentGroupIds.Any())
        {
            await SaveStudentToGroupsAsync(entity, createDto.StudentGroupIds);
        }
    }

    /// <summary>
    /// 获取导入项ID
    /// </summary>
    protected override string GetImportItemId(StudentBatchImportDto importDto)
    {
        return importDto.StudentNumber;
    }

    /// <summary>
    /// 验证导入项
    /// </summary>
    protected override async Task<IEnumerable<StudentBatchImportDto>> ValidateImportItems(IEnumerable<StudentBatchImportDto> importData)
    {
        var items = importData.ToList();
        
        // 检查学号是否重复
        var existingStudentNumbers = await Repository
            .Find(s => items.Select(i => i.StudentNumber).Contains(s.StudentNumber))
            .Select(s => s.StudentNumber)
            .ToListAsync();
            
        return items.Where(i => !existingStudentNumbers.Contains(i.StudentNumber));
    }

    /// <summary>
    /// 保存学生到分组
    /// </summary>
    public async Task SaveStudentToGroupsAsync(Student entity, List<long> groupIds)
    { 

        // 验证学生组是否存在
        var groups = await _studentGroupRepository
            .Find(x => groupIds.Contains(x.Id))
            .ToListAsync();

        if (groups.Count != groupIds.Count)
            throw new AppServiceException(400, "部分学生组不存在！");

        var mappings = await _mappingRepository.CreateQuery().AsNoTracking().Where(x=>x.StudentId == entity.Id).ToListAsync();
        if (mappings.Any())
            await _mappingRepository.DeleteRangeAsync(mappings);
        // 创建新的映射
        var newMappings = groupIds
            .Select(groupId => new StudentGroupMapping
            {
                 Id = _idGenerator.NewId(),
                StudentId = entity.Id,
                StudentGroupId = groupId
            })
            .ToList();

        await _mappingRepository.AddRangeAsync(newMappings);
        await _mappingRepository.SaveChangesAsync();
    }
    protected override async Task<Student> GetEntityForUpdate(long id, UpdateStudentDto updateDto)
    {
        return await _repository.CreateQuery().Include(x => x.StudentGroups).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }
    protected override async Task OnUpdating(Student entity, UpdateStudentDto updateDto)
    {
        // 检查学号是否已存在
        var existsStudentNumber = await Repository
            .Find(x => x.StudentNumber == updateDto.StudentNumber && x.Id != entity.Id)
            .AnyAsync();
        if (existsStudentNumber)
        {
            throw new AppServiceException(400, "学号/工号已存在！");
        }


        var entityGroups = entity.StudentGroups?.Select(x => x.StudentGroupId).ToList();
        if(entityGroups == null)
            entityGroups = new List<long>();
        if (entityGroups.Except(updateDto.StudentGroupIds).Any())
        {
            entity.StudentGroups = null;
            await SaveStudentToGroupsAsync(entity, updateDto.StudentGroupIds);

        }
        else if (updateDto.StudentGroupIds.Except(entityGroups).Any())
        {
            entity.StudentGroups = null;
            await SaveStudentToGroupsAsync(entity, updateDto.StudentGroupIds);

        } 
    }
} 