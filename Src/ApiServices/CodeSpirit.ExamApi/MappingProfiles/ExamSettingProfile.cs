using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.ExamApi.Dtos.StudentGroup;
using CodeSpirit.Shared.Extensions;

namespace CodeSpirit.ExamApi.MappingProfiles;

/// <summary>
/// 考试设置映射配置
/// </summary>
public class ExamSettingProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamSettingProfile()
    {
        // 配置基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            ExamSetting,
            ExamSettingDto,
            long,
            CreateExamSettingDto,
            UpdateExamSettingDto,
            CreateExamSettingDto>();


        // StudentGroup -> ExamSettingStudentGroupDto 映射（简化版）
        CreateMap<Data.Models.StudentGroup, ExamSettingStudentGroupDto>()
            .ForMember(dest => dest.StudentCount, opt => opt.MapFrom(src => src.Students.Count));

        CreateMap<ExamSetting, ExamSettingDto>()
            .ForMember(dest => dest.ExamPaperName, opt => opt.MapFrom(src => src.ExamPaper.Name))
            .ForMember(dest => dest.StudentGroups, opt => opt.MapFrom(src => src.StudentGroups.Select(x => x.StudentGroup)))
            .ForMember(dest => dest.StudentGroupIds, opt => opt.MapFrom(src =>
                src.StudentGroups != null ?
                src.StudentGroups.Select(x => x.StudentGroup.Id).ToList() :
                new List<long>()));

        CreateMap<CreateExamSettingDto, ExamSetting>();
        CreateMap<UpdateExamSettingDto, ExamSetting>();
    }
} 