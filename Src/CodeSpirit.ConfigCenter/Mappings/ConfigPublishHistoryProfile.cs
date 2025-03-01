using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.Core;

namespace CodeSpirit.ConfigCenter.Mappings;

/// <summary>
/// 配置发布历史映射配置
/// </summary>
public class ConfigPublishHistoryProfile : Profile
{
    /// <summary>
    /// 初始化配置发布历史映射配置
    /// </summary>
    public ConfigPublishHistoryProfile()
    {
        // 配置发布历史映射
        CreateMap<ConfigPublishHistory, ConfigPublishHistoryDto>()
            .ForMember(dest => dest.AppName, opt => opt.MapFrom(src => src.App != null ? src.App.Name : string.Empty))
            .ForMember(dest => dest.Environment, opt => opt.MapFrom(src => src.Environment.ToString()))
            ;

        // 配置项发布历史映射
        CreateMap<ConfigItemPublishHistory, ConfigItemPublishHistoryDto>()
            .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.ConfigItem != null ? src.ConfigItem.Key : string.Empty))
            .ForMember(dest => dest.Group, opt => opt.MapFrom(src => src.ConfigItem != null ? src.ConfigItem.Group : string.Empty))
            .ForMember(dest => dest.ValueType, opt => opt.MapFrom(src => src.ConfigItem != null ? src.ConfigItem.ValueType : default));

        // 添加新的映射配置
        CreateMap<CreateConfigPublishHistoryDto, ConfigPublishHistory>()
            .ForMember(dest => dest.Version, opt => opt.Ignore());

        CreateMap<UpdateConfigPublishHistoryDto, ConfigPublishHistory>();

        // 添加 PageList 映射配置
        CreateMap<PageList<ConfigPublishHistory>, PageList<ConfigPublishHistoryDto>>();
    }
}