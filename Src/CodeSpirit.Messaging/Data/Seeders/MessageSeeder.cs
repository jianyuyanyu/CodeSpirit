using CodeSpirit.Messaging.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSpirit.Messaging.Data.Seeders
{
    /// <summary>
    /// 消息数据初始化
    /// </summary>
    public class MessageSeeder
    {
        private readonly ILogger<MessageSeeder> _logger;
        private readonly MessagingDbContext _dbContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param>
        /// <param name="dbContext">数据库上下文</param>
        public MessageSeeder(
            ILogger<MessageSeeder> logger,
            MessagingDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// 初始化系统通知
        /// </summary>
        public async Task SeedSystemNotificationsAsync()
        {
            try
            {
                // 检查是否已经有数据 - 在没有多租户过滤器的情况下检查
                var hasSystemNotifications = await _dbContext.WithoutMultiTenantFilterAsync(async () =>
                {
                    return await _dbContext.Messages.AnyAsync(m => m.Type == MessageType.SystemNotification);
                });

                if (hasSystemNotifications)
                {
                    _logger.LogInformation("系统通知已存在，跳过初始化");
                    return;
                }

                // 创建系统通知列表
                var systemNotifications = new List<Message>
                {
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        Type = MessageType.SystemNotification,
                        Title = "欢迎使用 CodeSpirit",
                        Content = "感谢您选择使用 CodeSpirit 平台，我们致力于为您提供最佳的开发体验。",
                        RecipientId = "all",
                        SenderId = "system",
                        SenderName = "系统管理员",
                        CreatedAt = DateTime.Now,
                        TenantId = "default" // 设置默认租户ID
                    },
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        Type = MessageType.SystemNotification,
                        Title = "系统更新通知",
                        Content = "系统已更新到最新版本，包含多项功能改进和性能优化，详情请参阅更新日志。",
                        RecipientId = "all",
                        SenderId = "system",
                        SenderName = "系统管理员",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        TenantId = "default" // 设置默认租户ID
                    },
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        Type = MessageType.SystemNotification,
                        Title = "安全提醒",
                        Content = "请定期修改您的密码并保护好您的账户信息，不要将密码泄露给他人。",
                        RecipientId = "all",
                        SenderId = "system",
                        SenderName = "系统管理员",
                        CreatedAt = DateTime.Now.AddDays(-2),
                        TenantId = "default" // 设置默认租户ID
                    }
                };

                // 在没有多租户过滤器的情况下添加到数据库
                await _dbContext.WithoutMultiTenantFilterAsync(async () =>
                {
                    await _dbContext.Messages.AddRangeAsync(systemNotifications);
                    await _dbContext.SaveChangesAsync();
                    return true;
                });
                _logger.LogInformation("成功初始化 {Count} 条系统通知", systemNotifications.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化系统通知时发生错误：{Message}", ex.Message);
            }
        }
    }
} 