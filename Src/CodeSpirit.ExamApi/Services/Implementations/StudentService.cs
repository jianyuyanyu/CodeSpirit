using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.EventBus.Events;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 学生服务实现
/// </summary>
public class StudentService : BaseCRUDIService<Student, StudentDto, long, CreateStudentDto, UpdateStudentDto, StudentBatchImportDto>, IStudentService
{
    private readonly IRepository<StudentGroupMapping> _mappingRepository;
    private readonly IRepository<StudentGroup> _studentGroupRepository;
    private readonly ILogger<StudentService> _logger;
    private readonly IIdGenerator _idGenerator;
    private readonly IEventBus _eventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StudentService(
        IRepository<Student> repository,
        IRepository<StudentGroupMapping> mappingRepository,
        IRepository<StudentGroup> studentGroupRepository,
        IMapper mapper,
        ILogger<StudentService> logger,
        IIdGenerator idGenerator,
        IEventBus eventBus)
        : base(repository, mapper)
    {
        _mappingRepository = mappingRepository;
        _studentGroupRepository = studentGroupRepository;
        _logger = logger;
        _idGenerator = idGenerator;
        _eventBus = eventBus;
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

        return await GetPagedListAsync(
            queryDto,
            predicate
        );
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
        // 处理学生分组
        if (createDto.StudentGroupIds.Any())
        {
            await AddStudentToGroupsAsync(entity.Id, createDto.StudentGroupIds);
        }
        
        // 发布用户创建事件
        await PublishUserCreatedEventAsync(entity);
    }

    protected override async Task OnUpdated(Student entity)
    {
        await base.OnUpdated(entity);
        // 发布用户创建事件
        await PublishUserCreatedEventAsync(entity);
    }

    /// <summary>
    /// 发布用户创建事件
    /// </summary>
    private async Task PublishUserCreatedEventAsync(Student student)
    {
        try
        {
            var @event = new UserCreatedEvent
            {
                UserId = student.UserId,
                UserName = student.PhoneNumber,
                //IdNo
                Name = student.Name,
                PhoneNumber = student.PhoneNumber,
                Email = $"{student.StudentNumber}@example.com", // 默认邮箱
                IsActive = student.IsActive
            };
            
            await _eventBus.PublishAsync(@event);
            _logger.LogInformation("已发布用户创建事件: {@UserId}", student.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布用户创建事件失败: {@UserId}", student.UserId);
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
    /// 添加学生到分组
    /// </summary>
    public async Task AddStudentToGroupsAsync(long studentId, List<long> groupIds)
    {
        var student = await Repository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在！");
        }

        // 验证学生组是否存在
        var groups = await _studentGroupRepository
            .Find(x => groupIds.Contains(x.Id))
            .ToListAsync();

        if (groups.Count != groupIds.Count)
        {
            throw new AppServiceException(400, "部分学生组不存在！");
        }

        // 获取已存在的映射
        var existingMappings = await _mappingRepository
            .Find(x => x.StudentId == studentId && groupIds.Contains(x.StudentGroupId))
            .Select(x => x.StudentGroupId)
            .ToListAsync();

        // 创建新的映射
        var newMappings = groupIds
            .Except(existingMappings)
            .Select(groupId => new StudentGroupMapping
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
    /// 从分组移除学生
    /// </summary>
    public async Task RemoveStudentFromGroupsAsync(long studentId, List<long> groupIds)
    {
        var student = await Repository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在！");
        }

        await _mappingRepository
            .Find(x => x.StudentId == studentId && groupIds.Contains(x.StudentGroupId))
            .ExecuteDeleteAsync();
    }

} 