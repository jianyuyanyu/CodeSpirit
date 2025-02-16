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
        CreateMap<App, AppDto>();

        CreateMap<CreateAppDto, App>()
            .ForMember(dest => dest.Secret, opt => opt.Ignore())
            .ForMember(dest => dest.Enabled, opt => opt.MapFrom(_ => true));

        CreateMap<UpdateAppDto, App>()
            .ForMember(dest => dest.Secret, opt => opt.Ignore());

        CreateMap<AppBatchImportItemDto, App>()
            .ForMember(dest => dest.Secret, opt => opt.Ignore())
            .ForMember(dest => dest.Enabled, opt => opt.MapFrom(_ => true));

        CreateMap<PageList<App>, PageList<AppDto>>();
    }
} 