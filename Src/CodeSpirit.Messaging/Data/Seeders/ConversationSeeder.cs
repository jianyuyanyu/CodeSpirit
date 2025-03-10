using CodeSpirit.Messaging.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CodeSpirit.Messaging.Data.Seeders
{
    /// <summary>
    /// 对话数据初始化
    /// </summary>
    public class ConversationSeeder
    {
        private readonly ILogger<ConversationSeeder> _logger;
        private readonly MessagingDbContext _dbContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        /// <param name="dbContext">数据库上下文</param>
        public ConversationSeeder(
            ILogger<ConversationSeeder> logger,
            MessagingDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// 初始化示例对话
        /// </summary>
        public async Task SeedSampleConversationsAsync()
        {
            try
            {
                // 检查是否已经有数据
                if (_dbContext.Conversations.Any())
                {
                    _logger.LogInformation("对话数据已存在，跳过初始化");
                    return;
                }

                var systemUserId = "system";
                var adminUserId = "admin";
                var testUser1Id = "user1";
                var testUser2Id = "user2";

                // 创建示例对话1：系统与管理员
                var conversationSystem = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Title = "系统通知",
                    CreatedAt = DateTime.Now.AddDays(-5),
                    LastActivityAt = DateTime.Now.AddDays(-1),
                    Participants = new List<ConversationParticipant>
                    {
                        new ConversationParticipant
                        {
                            UserId = systemUserId,
                            UserName = "系统",
                            JoinedAt = DateTime.Now.AddDays(-5)
                        },
                        new ConversationParticipant
                        {
                            UserId = adminUserId,
                            UserName = "管理员",
                            JoinedAt = DateTime.Now.AddDays(-5)
                        }
                    }
                };

                // 创建示例对话2：测试用户之间的对话
                var conversationUsers = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Title = "测试用户对话",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    LastActivityAt = DateTime.Now.AddHours(-12),
                    Participants = new List<ConversationParticipant>
                    {
                        new ConversationParticipant
                        {
                            UserId = testUser1Id,
                            UserName = "测试用户1",
                            JoinedAt = DateTime.Now.AddDays(-3)
                        },
                        new ConversationParticipant
                        {
                            UserId = testUser2Id,
                            UserName = "测试用户2",
                            JoinedAt = DateTime.Now.AddDays(-3)
                        }
                    }
                };

                // 添加对话到数据库
                await _dbContext.Conversations.AddRangeAsync(conversationSystem, conversationUsers);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("成功初始化 2 个示例对话");

                // 为对话添加消息
                await SeedMessagesForConversationsAsync(conversationSystem, conversationUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化示例对话时发生错误：{Message}", ex.Message);
            }
        }

        /// <summary>
        /// 为对话添加示例消息
        /// </summary>
        private async Task SeedMessagesForConversationsAsync(Conversation systemConversation, Conversation userConversation)
        {
            try
            {
                var now = DateTime.Now;
                
                // 系统对话的消息
                var systemMessages = new List<Message>
                {
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        Type = MessageType.SystemNotification,
                        Title = "系统维护通知",
                        Content = "系统将于本周日凌晨2点至4点进行例行维护，期间部分功能可能暂时不可用。",
                        SenderId = "system",
                        SenderName = "系统",
                        RecipientId = "admin",
                        CreatedAt = now.AddDays(-5)
                    },
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        Type = MessageType.UserMessage,
                        Title = "回复：系统维护通知",
                        Content = "已收到通知，我会提前告知团队成员。",
                        SenderId = "admin",
                        SenderName = "管理员",
                        RecipientId = "system",
                        CreatedAt = now.AddDays(-5).AddHours(1)
                    }
                };

                // 用户对话的消息
                var userMessages = new List<Message>
                {
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        Type = MessageType.UserMessage,
                        Title = "",
                        Content = "你好，请问新功能什么时候上线？",
                        SenderId = "user1",
                        SenderName = "测试用户1",
                        RecipientId = "user2",
                        CreatedAt = now.AddDays(-3)
                    },
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        Type = MessageType.UserMessage,
                        Title = "",
                        Content = "你好，根据计划将在下周三发布，届时会有邮件通知。",
                        SenderId = "user2",
                        SenderName = "测试用户2",
                        RecipientId = "user1",
                        CreatedAt = now.AddDays(-3).AddMinutes(10)
                    },
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        Type = MessageType.UserMessage,
                        Title = "",
                        Content = "好的，谢谢你的回复！",
                        SenderId = "user1",
                        SenderName = "测试用户1",
                        RecipientId = "user2",
                        CreatedAt = now.AddDays(-3).AddMinutes(20)
                    }
                };

                // 将消息添加到对话中
                systemConversation.Messages.AddRange(systemMessages);
                userConversation.Messages.AddRange(userMessages);

                // 更新对话
                _dbContext.Conversations.Update(systemConversation);
                _dbContext.Conversations.Update(userConversation);
                
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("成功为对话添加 {Count} 条示例消息", systemMessages.Count + userMessages.Count);

                // 创建消息已读状态记录
                var userMessageReads = new List<UserMessageRead>();
                
                // 系统消息已读状态
                userMessageReads.Add(new UserMessageRead
                {
                    MessageId = systemMessages[0].Id,
                    UserId = "admin",
                    IsRead = true,
                    ReadAt = now.AddDays(-5).AddHours(1)
                });
                
                userMessageReads.Add(new UserMessageRead
                {
                    MessageId = systemMessages[1].Id,
                    UserId = "system",
                    IsRead = true,
                    ReadAt = now.AddDays(-5).AddHours(1)
                });
                
                // 用户消息已读状态
                userMessageReads.Add(new UserMessageRead
                {
                    MessageId = userMessages[0].Id,
                    UserId = "user2",
                    IsRead = true,
                    ReadAt = now.AddDays(-3).AddMinutes(5)
                });
                
                userMessageReads.Add(new UserMessageRead
                {
                    MessageId = userMessages[1].Id,
                    UserId = "user1",
                    IsRead = true,
                    ReadAt = now.AddDays(-3).AddMinutes(15)
                });
                
                userMessageReads.Add(new UserMessageRead
                {
                    MessageId = userMessages[2].Id,
                    UserId = "user2",
                    IsRead = true,
                    ReadAt = now.AddDays(-3).AddMinutes(25)
                });

                // 保存用户消息已读记录
                await _dbContext.UserMessageReads.AddRangeAsync(userMessageReads);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation("成功创建 {Count} 条消息已读状态记录", userMessageReads.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为对话添加示例消息时发生错误：{Message}", ex.Message);
            }
        }
    }
} 