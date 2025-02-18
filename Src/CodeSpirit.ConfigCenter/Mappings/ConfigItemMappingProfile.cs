using AutoMapper;
using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;

namespace CodeSpirit.ConfigCenter.Mappings;

/// <summary>
/// 配置项映射配置
/// </summary>
public class ConfigItemMappingProfile : Profile
{
    public ConfigItemMappingProfile()
    {
        // ConfigItem -> ConfigItemDto
        CreateMap<ConfigItem, ConfigItemDto>();

        // CreateConfigDto -> ConfigItem
        CreateMap<CreateConfigDto, ConfigItem>();

        // UpdateConfigDto -> ConfigItem
        CreateMap<UpdateConfigDto, ConfigItem>();

        // ConfigItemBatchImportDto -> ConfigItem
        CreateMap<ConfigItemBatchImportDto, ConfigItem>();

        // PageList mapping
        CreateMap<PageList<ConfigItem>, PageList<ConfigItemDto>>();
    }
} 