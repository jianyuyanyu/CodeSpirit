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
        _repository = repository;
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
            predicate = predicate.Or(x => x.IdNo.Contains(queryDto.Keywords));
            predicate = predicate.Or(x => x.AdmissionTicket.Contains(queryDto.Keywords));
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

        var existsIdNo = await Repository
            .Find(x => x.IdNo == createDto.IdNo)
            .AnyAsync();
        if (existsIdNo)
        {
            throw new AppServiceException(400, "该身份证已存在！");
        }
        var existsAdmissionTicket = await Repository
            .Find(x => x.AdmissionTicket == createDto.AdmissionTicket)
            .AnyAsync();
        if (existsAdmissionTicket)
        {
            throw new AppServiceException(400, "该准考证已存在！");
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
        entity.UserId = entity.Id;
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
            await SaveStudentToGroupsAsync(entity, createDto.StudentGroupIds);
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

        var mappings = await _mappingRepository.CreateQuery().AsNoTracking().Where(x => x.StudentId == entity.Id).ToListAsync();
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


        var existsIdNo = await Repository
            .Find(x => x.IdNo == updateDto.IdNo && x.Id != entity.Id)
            .AnyAsync();
        if (existsIdNo)
        {
            throw new AppServiceException(400, "该身份证已存在！");
        }
        var existsAdmissionTicket = await Repository
            .Find(x => x.AdmissionTicket == updateDto.AdmissionTicket && x.Id != entity.Id)
            .AnyAsync();
        if (existsAdmissionTicket)
        {
            throw new AppServiceException(400, "该准考证已存在！");
        }
        var entityGroups = entity.StudentGroups?.Select(x => x.StudentGroupId).ToList();
        if (entityGroups == null)
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
    public override async Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<StudentBatchImportDto> importData)
    {
        ArgumentNullException.ThrowIfNull(importData);

        var successCount = 0;
        var importList = importData.ToList();
        var inserts = new List<Student>();
        var failedItems = new List<string>();

        var studentNumberRepetition = importList.GroupBy(x => x.StudentNumber).Select(s => new { studentNumber = s.Key, count = s.Count() });
        if (studentNumberRepetition.Any(x => x.count > 1))
        {
            var error = string.Join(",", studentNumberRepetition.Where(x => x.count > 1).Select(x => x.studentNumber));
            failedItems.Add($"导入数据中出现重复的学号：{error}");
        }
        var idNoRepetition = importList.GroupBy(x => x.IdNo).Select(s => new { idNo = s.Key, count = s.Count() });
        if (idNoRepetition.Any(x => x.count > 1))
        {
            var error = string.Join(",", idNoRepetition.Where(x => x.count > 1).Select(x => x.idNo));
            failedItems.Add($"导入数据中出现重复的身份证：{error}");
        }
        var admissionTicketRepetition = importList.GroupBy(x => x.AdmissionTicket).Select(s => new { admissionTicket = s.Key, count = s.Count() });
        if (admissionTicketRepetition.Any(x => x.count > 1))
        {
            var error = string.Join(",", admissionTicketRepetition.Where(x => x.count > 1).Select(x => x.admissionTicket));
            failedItems.Add($"导入数据中出现重复的准考证：{error}");
        }

        var checkDatas = await Repository.CreateQuery().Where(x => importList.Select(x => x.StudentNumber).Contains(x.StudentNumber)
        || importList.Select(x => x.IdNo).Contains(x.IdNo)
        || importList.Select(x => x.AdmissionTicket).Contains(x.AdmissionTicket))
            .Select(x => new { x.StudentNumber, x.IdNo, x.AdmissionTicket }).ToListAsync();

        foreach (var item in importList)
        {
            if (checkDatas.Any())
            {
                if (checkDatas.Any(x => x.IdNo == item.IdNo))
                {
                    failedItems.Add($"{item.IdNo}「传入的身份证'{item.IdNo}'已存在");
                    continue;
                }
                if (checkDatas.Any(x => x.AdmissionTicket == item.AdmissionTicket))
                {
                    failedItems.Add($"{item.AdmissionTicket}「传入的准考证'{item.AdmissionTicket}'已存在");
                    continue;
                }
                if (checkDatas.Any(x => x.StudentNumber == item.StudentNumber))
                {
                    failedItems.Add($"{item.StudentNumber}「传入的学工号'{item.StudentNumber}'已存在");
                    continue;
                }
            }

            var genderType = Gender.Unknown;
            switch (item.Gender)
            {
                case "男":
                    genderType = Gender.Male;
                    break;
                case "女":
                    genderType = Gender.Female;
                    break;
                default:
                    break;
            }
            var entity = Mapper.Map<Student>(item);
            entity.Gender = genderType;
            entity.Id = _idGenerator.NewId();
            entity.UserId = entity.Id;
            inserts.Add(entity);
            successCount++;

        }
        await Repository.AddRangeAsync(inserts);
        return (successCount, failedItems);
    }

}