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
        CreateMap<StudentBatchImportDto , Student>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Gender, opt => opt.Ignore());

        CreateMap<PageList<Student>, PageList<StudentDto>>();
    }

}
