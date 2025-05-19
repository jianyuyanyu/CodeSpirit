using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.PracticeSetting;
using CodeSpirit.Shared.Extensions;

namespace CodeSpirit.ExamApi.MappingProfiles;

/// <summary>
/// 练习设置映射配置
/// </summary>
public class PracticeSettingProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PracticeSettingProfile()
    {
        // 配置基本CRUD映射
        this.ConfigureBaseCRUDIMappings<
            PracticeSetting,
            PracticeSettingDto,
            long,
            CreatePracticeSettingDto,
            UpdatePracticeSettingDto,
            CreatePracticeSettingDto>(); // 使用CreatePracticeSettingDto作为批量导入DTO类型

        // 实体到DTO的详细映射
        CreateMap<PracticeSetting, PracticeSettingDto>()
            .ForMember(dest => dest.ExamPaperName, opt => opt.MapFrom(src => src.ExamPaper.Name));

        // DTO到实体的映射
        CreateMap<CreatePracticeSettingDto, PracticeSetting>();
        CreateMap<UpdatePracticeSettingDto, PracticeSetting>();
    }
} 