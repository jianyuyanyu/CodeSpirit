using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.StudentGroup;
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
        CreateMap<StudentGroup, StudentGroupDto>();

        CreateMap<CreateStudentGroupDto, StudentGroup>();
        CreateMap<UpdateStudentGroupDto, StudentGroup>();

        // 添加 PageList 映射配置
        CreateMap<PageList<StudentGroup>, PageList<StudentGroupDto>>();
    }
}