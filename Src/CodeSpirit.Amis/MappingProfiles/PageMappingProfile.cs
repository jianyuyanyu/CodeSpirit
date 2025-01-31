using AutoMapper;
using CodeSpirit.Amis.App;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Configuration;
using Newtonsoft.Json;

namespace CodeSpirit.Amis.MappingProfiles
{
    public class PageMappingProfile : Profile
    {
        public PageMappingProfile()
        {
            // ConfigurationPage 到 Page 的映射
            CreateMap<ConfigurationPage, Page>();

            // PageAttribute 到 Page 的映射
            CreateMap<PageAttribute, Page>()
                .ForMember(dest => dest.Schema, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Schema) ? null : JsonConvert.DeserializeObject<Schema>(src.Schema)));
        }
    }
}
