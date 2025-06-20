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
using CodeSpirit.Core.Extensions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;

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
            if (queryDto.StudentGroupId.Value == -1)
            {
                // 查询无分组的学生
                predicate = predicate.And(x => !x.StudentGroups.Any());
            }
            else
            {
                // 查询指定分组的学生
                predicate = predicate.And(x => x.StudentGroups.Any(sg => sg.StudentGroupId == queryDto.StudentGroupId.Value));
            }
        }

        // 修改查询
        var query = _repository.CreateQuery()
            .Include(x => x.StudentGroups)
                .ThenInclude(x => x.StudentGroup)
            .Where(predicate).OrderByDescending(x=>x.CreatedAt);

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

            await OnDeleting(student);
            await Repository.DeleteAsync(student);
            await Repository.SaveChangesAsync();
            await OnDeleted(student);
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
        var existsStudentNumber = Repository.Find(x => !createDto.StudentNumber.IsNullOrWhiteSpace() && x.StudentNumber == createDto.StudentNumber);
        if (!createDto.StudentNumber.IsNullOrWhiteSpace() && await existsStudentNumber.AnyAsync())
        {
            throw new AppServiceException(400, "学号/工号已存在！");
        }

        var existsIdNo = Repository.Find(x => !createDto.IdNo.IsNullOrWhiteSpace() && x.IdNo == createDto.IdNo);
        if (!createDto.IdNo.IsNullOrWhiteSpace() && await existsIdNo.AnyAsync())
        {
            throw new AppServiceException(400, "该身份证已存在！");
        }
        var existsAdmissionTicket = Repository.Find(x => !createDto.AdmissionTicket.IsNullOrWhiteSpace() && x.AdmissionTicket == createDto.AdmissionTicket);
        if (!createDto.AdmissionTicket.IsNullOrWhiteSpace() && await existsAdmissionTicket.AnyAsync())
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

    protected override async Task OnDeleted(Student entity)
    {
        await base.OnDeleted(entity);
        // 发布用户删除事件
        await PublishUserDeletedEventAsync(entity);
    }

    /// <summary>
    /// 发布用户创建事件
    /// </summary>
    private async Task PublishUserCreatedEventAsync(Student student)
    {
        try
        {
            var @event = new UserCreatedOrUpdatedEvent
            {
                UserId = student.UserId,
                UserName = student.IdNo,
                Gender = student.Gender == Gender.Male ? "男" : (student.Gender == Gender.Female ? "女" : "未知"),
                Name = student.Name,
                PhoneNumber = student.PhoneNumber,
                Email = $"{student.IdNo}@example.com", // 默认邮箱
                IsActive = student.IsActive,
                IdNo = student.IdNo
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
    /// 发布用户删除事件
    /// </summary>
    /// <param name="student"></param>
    /// <returns></returns>
    private async Task PublishUserDeletedEventAsync(Student student)
    {
        try
        {
            _logger.LogInformation("准备发布用户删除事件: 学生ID={StudentId}, 用户ID={UserId}", 
                student.Id, student.UserId);

            if (student.UserId <= 0)
            {
                _logger.LogError("无法发布用户删除事件：用户ID无效: {UserId}", student.UserId);
                return;
            }

            var @event = new UserDeletedEvent
            {
                UserId = student.UserId,
            };

            _logger.LogInformation("正在发布用户删除事件: {@Event}", @event);
            await _eventBus.PublishAsync(@event);
            _logger.LogInformation("用户删除事件发布成功: 用户ID={UserId}", student.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布用户删除事件失败: 学生ID={StudentId}, 用户ID={UserId}, 错误信息: {ErrorMessage}", 
                student.Id, student.UserId, ex.Message);
            throw; // 重新抛出异常，让调用者知道发布失败
        }
    }

    /// <summary>
    /// 获取导入项ID
    /// </summary>
    protected override string GetImportItemId(StudentBatchImportDto importDto)
    {
        return importDto.IdNo;
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
        var existsStudentNumberQueryable = Repository.Find(x => x.StudentNumber == updateDto.StudentNumber && x.Id != entity.Id);
        if (!updateDto.StudentNumber.IsNullOrWhiteSpace() && await existsStudentNumberQueryable.AnyAsync())
        {
            throw new AppServiceException(400, "学号/工号已存在！");
        }

        var existsIdNo = Repository.Find(x => x.IdNo == updateDto.IdNo && x.Id != entity.Id);
        if (!updateDto.IdNo.IsNullOrWhiteSpace() && await existsIdNo.AnyAsync())
        {
            throw new AppServiceException(400, "该身份证已存在！");
        }

        var existsAdmissionTicket = Repository.Find(x => x.AdmissionTicket == updateDto.AdmissionTicket && x.Id != entity.Id);
        if (!updateDto.AdmissionTicket.IsNullOrWhiteSpace() && await existsAdmissionTicket.AnyAsync())
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
        var failedItems = new List<string>();

        importData.ForEach(s => s.IdNo = s.IdNo.Trim());

        var studentNumberRepetition = importList.Where(s => !s.StudentNumber.IsNullOrWhiteSpace()).GroupBy(x => x.StudentNumber).Select(s => new { studentNumber = s.Key, count = s.Count() });
        if (studentNumberRepetition.Any(x => x.count > 1))
        {
            var error = string.Join(",", studentNumberRepetition.Where(x => x.count > 1).Select(x => x.studentNumber));
            failedItems.Add($"导入数据中出现重复的学号：{error}");
        }
        var idNoRepetition = importList.Where(s => !s.IdNo.IsNullOrWhiteSpace()).GroupBy(x => x.IdNo).Select(s => new { idNo = s.Key, count = s.Count() });
        if (idNoRepetition.Any(x => x.count > 1))
        {
            var error = string.Join(",", idNoRepetition.Where(x => x.count > 1).Select(x => x.idNo));
            failedItems.Add($"导入数据中出现重复的身份证：{error}");
        }
        var admissionTicketRepetition = importList.Where(s => !s.AdmissionTicket.IsNullOrWhiteSpace()).GroupBy(x => x.AdmissionTicket).Select(s => new { admissionTicket = s.Key, count = s.Count() });
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
                if (!item.PhoneNumber.IsNullOrWhiteSpace() && !Regex.IsMatch(item.PhoneNumber, @"1(3[0-9]|4[57]|5[0-35-9]|7[01235678]|8[0-9]|6[0-9]|9[0-9])\d{8}"))
                {
                    failedItems.Add($"{item.PhoneNumber}「传入的手机号'{item.PhoneNumber}'格式不正确");
                    continue;
                }

                if (!item.IdNo.IsNullOrWhiteSpace() && checkDatas.Any(x => x.IdNo == item.IdNo))
                {
                    failedItems.Add($"{item.IdNo}「传入的身份证'{item.IdNo}'已存在");
                    continue;
                }
                if (!item.AdmissionTicket.IsNullOrWhiteSpace() && checkDatas.Any(x => x.AdmissionTicket == item.AdmissionTicket))
                {
                    failedItems.Add($"{item.AdmissionTicket}「传入的准考证'{item.AdmissionTicket}'已存在");
                    continue;
                }
                if (!item.StudentNumber.IsNullOrWhiteSpace() && checkDatas.Any(x => x.StudentNumber == item.StudentNumber))
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
            successCount++;

            await Repository.AddAsync(entity);
            await PublishUserCreatedEventAsync(entity);
        }

        return (successCount, failedItems);
    }

    /// <summary>
    /// 批量分配考生到考生组
    /// </summary>
    public async Task<(int successCount, List<long> failedIds)> BatchAssignGroupsAsync(List<long> studentIds, List<long> groupIds)
    {
        // 验证考生组是否存在
        var groups = await _studentGroupRepository
            .Find(x => groupIds.Contains(x.Id))
            .ToListAsync();

        if (groups.Count != groupIds.Count)
        {
            throw new AppServiceException(400, "部分考生组不存在！");
        }

        // 验证考生是否存在
        var students = await _repository
            .Find(x => studentIds.Contains(x.Id))
            .ToListAsync();

        if (!students.Any())
        {
            throw new AppServiceException(400, "未找到指定的考生！");
        }

        var failedIds = new List<long>();
        var successCount = 0;

        // 获取已存在的映射关系
        var existingMappings = await _mappingRepository
            .Find(x => groupIds.Contains(x.StudentGroupId) && studentIds.Contains(x.StudentId))
            .Select(x => new { x.StudentId, x.StudentGroupId })
            .ToListAsync();

        // 创建新的映射关系
        var newMappings = new List<StudentGroupMapping>();
        foreach (var studentId in studentIds)
        {
            try
            {
                if (!students.Any(s => s.Id == studentId))
                {
                    failedIds.Add(studentId);
                    continue;
                }

                foreach (var groupId in groupIds)
                {
                    // 检查映射是否已存在
                    if (!existingMappings.Any(m => m.StudentId == studentId && m.StudentGroupId == groupId))
                    {
                        newMappings.Add(new StudentGroupMapping
                        {
                            StudentId = studentId,
                            StudentGroupId = groupId
                        });
                    }
                }

                if (!failedIds.Contains(studentId))
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为考生 {StudentId} 分配考生组时发生错误", studentId);
                failedIds.Add(studentId);
            }
        }

        if (newMappings.Any())
        {
            await _mappingRepository.AddRangeAsync(newMappings);
            await _mappingRepository.SaveChangesAsync();
        }

        return (successCount, failedIds);
    }
}