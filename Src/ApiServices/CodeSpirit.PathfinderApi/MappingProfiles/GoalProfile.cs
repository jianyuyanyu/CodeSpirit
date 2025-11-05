using AutoMapper;
using CodeSpirit.PathfinderApi.Dtos.Goal;

namespace CodeSpirit.PathfinderApi.MappingProfiles;

/// <summary>
/// 目标AutoMapper映射配置
/// </summary>
public class GoalProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public GoalProfile()
    {
        // Goal -> GoalDto
        CreateMap<Models.Goal, GoalDto>();
        
        // CreateGoalDto -> Goal
        CreateMap<CreateGoalDto, Models.Goal>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Progress, opt => opt.Ignore())
            .ForMember(dest => dest.FeasibilityScore, opt => opt.Ignore())
            .ForMember(dest => dest.ClarityScore, opt => opt.MapFrom(src => src.ClarityScore))
            .ForMember(dest => dest.ExecutabilityScore, opt => opt.MapFrom(src => src.ExecutabilityScore))
            .ForMember(dest => dest.CompletenessScore, opt => opt.MapFrom(src => src.CompletenessScore))
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tasks, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
        
        // UpdateGoalDto -> Goal（仅更新非null字段）
        CreateMap<UpdateGoalDto, Models.Goal>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}

