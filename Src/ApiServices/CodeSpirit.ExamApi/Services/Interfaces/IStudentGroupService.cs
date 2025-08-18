using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.StudentGroup;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考生组服务接口
/// </summary>
public interface IStudentGroupService : IBaseCRUDIService<StudentGroup, StudentGroupDto, long, CreateStudentGroupDto, UpdateStudentGroupDto, StudentGroupBatchImportDto>
{    
    /// <summary>
    /// 添加考生到分组
    /// </summary>
    Task AddStudentsToGroupAsync(long groupId, List<long> studentIds);
    
    /// <summary>
    /// 从分组移除考生
    /// </summary>
    Task RemoveStudentsFromGroupAsync(long groupId, List<long> studentIds);

    /// <summary>
    /// 获取所有未删除的学生组
    /// </summary>
    Task<List<StudentGroupDto>> GetAllActiveGroupsAsync();
    Task<PageList<StudentGroupDto>> GetStudentGroupsAsync(StudentGroupQueryDto queryDto);
} 