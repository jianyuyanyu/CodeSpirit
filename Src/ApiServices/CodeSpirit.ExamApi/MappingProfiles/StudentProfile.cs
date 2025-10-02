using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.Shared.Extensions;
/// <summary>
/// 学生映射配置
/// </summary>
public class StudentProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public StudentProfile()
    {
        CreateMap<Student, StudentDto>()
            .ForMember(dest => dest.StudentGroups, opt => opt.MapFrom(src =>
                src.StudentGroups != null ?
                src.StudentGroups.Select(x => x.StudentGroup.Name).ToList() :
                new List<string>()))
            .ForMember(dest => dest.StudentGroupIds, opt => opt.MapFrom(src =>
                src.StudentGroups != null ?
                src.StudentGroups.Select(x => x.StudentGroup.Id).ToList() :
                new List<long>()));

        CreateMap<CreateStudentDto, Student>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<UpdateStudentDto, Student>();
        
        // 批量导入映射到实体
        CreateMap<StudentBatchImportDto , Student>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Gender, opt => opt.Ignore());

        // 批量导入映射到创建DTO（用于增强批量导入）
        CreateMap<StudentBatchImportDto, CreateStudentDto>()
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => 
                src.Gender == "男" ? Gender.Male :
                src.Gender == "女" ? Gender.Female :
                Gender.Unknown))
            .ForMember(dest => dest.StudentGroupIds, opt => opt.MapFrom(src => new List<long>()));

        CreateMap<PageList<Student>, PageList<StudentDto>>();
    }

}
