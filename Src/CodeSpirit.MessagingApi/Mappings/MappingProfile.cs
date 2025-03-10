using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Messaging.Models;
using CodeSpirit.MessagingApi.Dtos.Responses;

namespace CodeSpirit.MessagingApi.Mappings;

/// <summary>
/// AutoMapper映射配置
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public MappingProfile()
    {
        // 消息映射
        CreateMap<Message, MessageDto>();
        
        // 会话参与者映射
        CreateMap<ConversationParticipant, ConversationParticipantDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName));
        
        // 会话映射
        CreateMap<Conversation, ConversationDto>()
            .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants))
            .ForMember(dest => dest.MessageCount, opt => opt.MapFrom(src => src.Messages.Count));
    }
} 