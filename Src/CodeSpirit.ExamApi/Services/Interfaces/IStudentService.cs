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
    /// 通过学号查找学生
    /// </summary>
    Task<StudentDto?> GetByStudentNumberAsync(string studentNumber);
    
    /// <summary>
    /// 通过用户ID查找学生
    /// </summary>
    Task<StudentDto?> GetByUserIdAsync(long userId);

    /// <summary>
    /// 批量分配考生到考生组
    /// </summary>
    /// <param name="studentIds">考生ID列表</param>
    /// <param name="groupIds">考生组ID列表</param>
    /// <returns>成功数量和失败的ID列表</returns>
    Task<(int successCount, List<long> failedIds)> BatchAssignGroupsAsync(List<long> studentIds, List<long> groupIds);
} 