using AutoMapper;
using CodeSpirit.SurveyApi.Dtos.Draft;
using CodeSpirit.SurveyApi.Models;

namespace CodeSpirit.SurveyApi.MappingProfiles;

/// <summary>
/// 草稿映射配置
/// </summary>
public class DraftMappingProfile : Profile
{
    /// <summary>
    /// 初始化草稿映射配置
    /// </summary>
    public DraftMappingProfile()
    {
        CreateDraftMaps();
    }

    /// <summary>
    /// 创建草稿映射
    /// </summary>
    private void CreateDraftMaps()
    {
        // SurveyDraft实体到SurveyDraftDto的映射
        CreateMap<SurveyDraft, SurveyDraftDto>();
    }
}
