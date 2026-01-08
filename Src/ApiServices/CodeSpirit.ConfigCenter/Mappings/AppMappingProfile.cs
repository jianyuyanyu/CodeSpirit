using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Models;

namespace CodeSpirit.ConfigCenter.Mappings;

/// <summary>
/// 应用程序实体映射配置文件
/// </summary>
public class AppMappingProfile : Profile
{
    /// <summary>
    /// 初始化应用程序映射配置
    /// </summary>
    public AppMappingProfile()
    {
        CreateMap<App, AppDto>()
            .ForMember(dest => dest.InheritancedAppName, 
                      opt => opt.MapFrom(src => src.InheritancedApp != null ? src.InheritancedApp.Name : null))
            .ForMember(dest => dest.Tags,
                      opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Tag) ? new List<string>() : new List<string> { src.Tag }))
            .ForMember(dest => dest.ConfigCount,
                      opt => opt.MapFrom(src => src.ConfigItems != null ? src.ConfigItems.Count : 0));

        CreateMap<CreateAppDto, App>()
            .ForMember(dest => dest.Secret, opt => opt.Ignore())
            .ForMember(dest => dest.Enabled, opt => opt.MapFrom(_ => true));

        CreateMap<UpdateAppDto, App>()
            .ForMember(dest => dest.Secret, opt => opt.Ignore())
            .ForMember(dest => dest.AutoPublish, opt => opt.MapFrom(p => p.AutoPublish.HasValue ? true : false));

        CreateMap<AppBatchImportItemDto, App>()
            .ForMember(dest => dest.Secret, opt => opt.Ignore())
            .ForMember(dest => dest.Enabled, opt => opt.MapFrom(_ => true));

        CreateMap<PageList<App>, PageList<AppDto>>();
    }
}