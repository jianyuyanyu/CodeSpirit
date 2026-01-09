using AutoMapper;
using CodeSpirit.ScheduledTasks.Helpers;
using CodeSpirit.ScheduledTasks.Models;
using CodeSpirit.ScheduledTasks.Dto;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.Web.MappingProfiles;

/// <summary>
/// 定时任务映射配置
/// </summary>
public class ScheduledTaskProfile : Profile
{
    public ScheduledTaskProfile()
    {
        // ScheduledTask -> ScheduledTaskDto
        CreateMap<ScheduledTask, ScheduledTaskDto>()
            .ForMember(dest => dest.CronDescription, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.CronExpression) 
                    ? CronHelper.GetDescription(src.CronExpression) 
                    : null));

        // CreateScheduledTaskDto -> ScheduledTask
        CreateMap<CreateScheduledTaskDto, ScheduledTask>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 
                src.EnableImmediately ? TaskStatus.Enabled : TaskStatus.Disabled))
            .ForMember(dest => dest.Timeout, opt => opt.MapFrom(src => 
                src.TimeoutSeconds.HasValue ? TimeSpan.FromSeconds(src.TimeoutSeconds.Value) : (TimeSpan?)null))
            .ForMember(dest => dest.DelayTime, opt => opt.MapFrom(src => 
                src.DelaySeconds.HasValue ? TimeSpan.FromSeconds(src.DelaySeconds.Value) : (TimeSpan?)null))
            .ForMember(dest => dest.RetryInterval, opt => opt.MapFrom(src => 
                src.RetryIntervalSeconds.HasValue ? TimeSpan.FromSeconds(src.RetryIntervalSeconds.Value) : (TimeSpan?)null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.ExecutionCount, opt => opt.Ignore())
            .ForMember(dest => dest.LastExecuteTime, opt => opt.Ignore())
            .ForMember(dest => dest.NextExecuteTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsFromConfiguration, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.NotificationConfig, opt => opt.Ignore());

        // UpdateScheduledTaskDto -> ScheduledTask (仅更新部分字段)
        CreateMap<UpdateScheduledTaskDto, ScheduledTask>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Timeout, opt => opt.MapFrom(src => 
                src.TimeoutSeconds.HasValue ? TimeSpan.FromSeconds(src.TimeoutSeconds.Value) : (TimeSpan?)null))
            .ForMember(dest => dest.DelayTime, opt => opt.MapFrom(src => 
                src.DelaySeconds.HasValue ? TimeSpan.FromSeconds(src.DelaySeconds.Value) : (TimeSpan?)null))
            .ForMember(dest => dest.RetryInterval, opt => opt.MapFrom(src => 
                src.RetryIntervalSeconds.HasValue ? TimeSpan.FromSeconds(src.RetryIntervalSeconds.Value) : (TimeSpan?)null))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ExecutionCount, opt => opt.Ignore())
            .ForMember(dest => dest.LastExecuteTime, opt => opt.Ignore())
            .ForMember(dest => dest.NextExecuteTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsFromConfiguration, opt => opt.Ignore())
            .ForMember(dest => dest.NotificationConfig, opt => opt.Ignore());

        // ScheduledTask -> UpdateScheduledTaskDto (用于编辑表单数据初始化)
        CreateMap<ScheduledTask, UpdateScheduledTaskDto>()
            .ForMember(dest => dest.TimeoutSeconds, opt => opt.MapFrom(src => 
                src.Timeout.HasValue ? (int?)src.Timeout.Value.TotalSeconds : null))
            .ForMember(dest => dest.DelaySeconds, opt => opt.MapFrom(src => 
                src.DelayTime.HasValue ? (int?)src.DelayTime.Value.TotalSeconds : null))
            .ForMember(dest => dest.RetryIntervalSeconds, opt => opt.MapFrom(src => 
                src.RetryInterval.HasValue ? (int?)src.RetryInterval.Value.TotalSeconds : null));

        // TaskExecution -> TaskExecutionDto
        CreateMap<TaskExecution, TaskExecutionDto>()
            .ForMember(dest => dest.DurationDisplay, opt => opt.MapFrom(src => 
                src.Duration.HasValue 
                    ? FormatDuration(src.Duration.Value) 
                    : null));
    }

    /// <summary>
    /// 格式化执行时长
    /// </summary>
    /// <param name="duration">时长</param>
    /// <returns>格式化后的字符串</returns>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{duration.Days}天{duration.Hours}小时{duration.Minutes}分钟";
        }
        if (duration.TotalHours >= 1)
        {
            return $"{duration.Hours}小时{duration.Minutes}分钟{duration.Seconds}秒";
        }
        if (duration.TotalMinutes >= 1)
        {
            return $"{duration.Minutes}分钟{duration.Seconds}秒";
        }
        if (duration.TotalSeconds >= 1)
        {
            return $"{duration.Seconds}.{duration.Milliseconds:D3}秒";
        }
        return $"{duration.Milliseconds}毫秒";
    }
}
