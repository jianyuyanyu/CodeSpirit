using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.Shared.Extensions;
/// <summary>
/// 学生映射配置
/// </summary>
public class StudentMappingProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public StudentMappingProfile()
    {
        /// 使用扩展方法自动配置所有映射
        this.ConfigureBaseCRUDIMappings<
            Student,             // 实体
            StudentDto,          // DTO
            long,                // 主键类型
            CreateStudentDto,    // 创建DTO
            UpdateStudentDto,    // 更新DTO
            StudentBatchImportDto // 批量导入DTO
        >();
    }
}
