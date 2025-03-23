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

        // 学生分组映射
        CreateMap<StudentGroup, StudentGroupDto>();

        CreateMap<ExamSetting, ExamSettingDto>()
            .ForMember(dest => dest.ExamPaperName, opt => opt.MapFrom(src => src.ExamPaper.Name))
            .ForMember(dest => dest.StudentGroups, opt => opt.MapFrom(src => src.StudentGroups.Select(x => x.StudentGroup)));

        CreateMap<CreateExamSettingDto, ExamSetting>();
        CreateMap<UpdateExamSettingDto, ExamSetting>();
    }
} 