using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.Messaging.Migrations
{
    /// <summary>
    /// 将现有数据迁移到默认租户以确保向后兼容性
    /// </summary>
    public partial class MigrateExistingDataToDefaultTenant : Migration
    {
        /// <summary>
        /// 将所有现有数据的TenantId设置为"default"
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 将所有现有的消息记录设置为默认租户
            migrationBuilder.Sql(@"
                UPDATE [Messages] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");

            // 将所有现有的对话记录设置为默认租户
            migrationBuilder.Sql(@"
                UPDATE [Conversations] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");

            // 将所有现有的对话参与者记录设置为默认租户
            migrationBuilder.Sql(@"
                UPDATE [ConversationParticipants] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");

            // 将所有现有的用户消息已读记录设置为默认租户
            migrationBuilder.Sql(@"
                UPDATE [UserMessageReads] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");
        }

        /// <summary>
        /// 回滚操作：将默认租户的数据TenantId设置回空字符串
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚时将默认租户的记录TenantId设置回空字符串
            migrationBuilder.Sql(@"
                UPDATE [Messages] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");

            migrationBuilder.Sql(@"
                UPDATE [Conversations] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");

            migrationBuilder.Sql(@"
                UPDATE [ConversationParticipants] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");

            migrationBuilder.Sql(@"
                UPDATE [UserMessageReads] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");
        }
    }
}
