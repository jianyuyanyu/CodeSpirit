using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.StudentGroup;
using System.Text.Json;
/// <summary>
/// 题目AutoMapper配置
/// </summary>
public class StudentGroupProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public StudentGroupProfile()
    {
        // 学生分组映射
        CreateMap<StudentGroup, StudentGroupDto>()
            .ForMember(dest => dest.StudentCount, opt =>
                opt.MapFrom(src => src.Students != null ? src.Students.Count : 0));

        CreateMap<CreateStudentGroupDto, StudentGroup>();
        CreateMap<UpdateStudentGroupDto, StudentGroup>();

        CreateMap<PageList<StudentGroup>, PageList<StudentGroupDto>>();

        CreateMap<StudentGroupBatchImportDto, StudentGroup>();
    }
}