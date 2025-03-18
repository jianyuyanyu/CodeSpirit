using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.Core;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 学生服务接口
/// </summary>
public interface IStudentService : IBaseCRUDIService<Student, StudentDto, long, CreateStudentDto, UpdateStudentDto, StudentBatchImportDto>
{
    /// <summary>
    /// 获取学生分页列表
    /// </summary>
    Task<PageList<StudentDto>> GetStudentsAsync(StudentQueryDto queryDto);
    
    /// <summary>
    /// 添加学生到分组
    /// </summary>
    Task AddStudentToGroupsAsync(long studentId, List<long> groupIds);
    
    /// <summary>
    /// 从分组移除学生
    /// </summary>
    Task RemoveStudentFromGroupsAsync(long studentId, List<long> groupIds);
    
    /// <summary>
    /// 通过学号查找学生
    /// </summary>
    Task<StudentDto?> GetByStudentNumberAsync(string studentNumber);
    
    /// <summary>
    /// 通过用户ID查找学生
    /// </summary>
    Task<StudentDto?> GetByUserIdAsync(long userId);
} 