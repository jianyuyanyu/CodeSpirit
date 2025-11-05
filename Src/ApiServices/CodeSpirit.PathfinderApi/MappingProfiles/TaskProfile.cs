using AutoMapper;
using CodeSpirit.PathfinderApi.Dtos.Task;
using CodeSpirit.PathfinderApi.Models;

namespace CodeSpirit.PathfinderApi.MappingProfiles;

/// <summary>
/// 任务映射配置
/// </summary>
public class TaskProfile : Profile
{
    public TaskProfile()
    {
        // Entity -> DTO
        CreateMap<PathfinderTask, TaskDto>();
        
        // CreateDTO -> Entity
        CreateMap<CreateTaskDto, PathfinderTask>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Models.Enums.TaskStatus.Pending))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        
        // UpdateDTO -> Entity
        CreateMap<UpdateTaskDto, PathfinderTask>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.GoalId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}

