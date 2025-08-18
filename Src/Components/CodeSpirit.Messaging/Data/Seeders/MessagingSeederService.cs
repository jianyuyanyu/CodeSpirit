using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CodeSpirit.Messaging.Data.Seeders
{
    /// <summary>
    /// 消息模块数据初始化服务
    /// </summary>
    public class MessagingSeederService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessagingSeederService> _logger;
        private readonly MessagingDbContext _dbContext;
        private readonly MessageSeeder _messageSeeder;
        private readonly ConversationSeeder _conversationSeeder;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="messageSeeder">消息数据初始化器</param>
        /// <param name="conversationSeeder">对话数据初始化器</param>
        public MessagingSeederService(
            IServiceProvider serviceProvider,
            ILogger<MessagingSeederService> logger,
            MessagingDbContext dbContext,
            MessageSeeder messageSeeder,
            ConversationSeeder conversationSeeder)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _dbContext = dbContext;
            _messageSeeder = messageSeeder;
            _conversationSeeder = conversationSeeder;
        }

        /// <summary>
        /// 初始化种子数据
        /// </summary>
        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("开始初始化消息模块数据...");

                // 应用迁移
                await _dbContext.Database.MigrateAsync();
                _logger.LogInformation("数据库迁移应用成功！");

                // 初始化系统通知
                await _messageSeeder.SeedSystemNotificationsAsync();

                // 初始化示例对话
                await _conversationSeeder.SeedSampleConversationsAsync();

                _logger.LogInformation("消息模块数据初始化完成！");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化消息模块数据时发生错误：{Message}", ex.Message);
            }
        }
    }
} 