using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultTenantForExistingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 为现有数据设置默认租户ID
            // 优先检查是否有空TenantId的数据，如果有则更新为默认值
            
            migrationBuilder.Sql(@"
                -- 为 RolePermissions 表中的现有数据设置默认租户ID
                UPDATE [RolePermissions] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");

            migrationBuilder.Sql(@"
                -- 为 RefreshTokens 表中的现有数据设置默认租户ID
                UPDATE [RefreshTokens] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");

            migrationBuilder.Sql(@"
                -- 为 LoginLogs 表中的现有数据设置默认租户ID
                UPDATE [LoginLogs] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");

            migrationBuilder.Sql(@"
                -- 为 AuditLogs 表中的现有数据设置默认租户ID
                UPDATE [AuditLogs] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");

            migrationBuilder.Sql(@"
                -- 为 ApplicationUserRole 表中的现有数据设置默认租户ID
                UPDATE [ApplicationUserRole] 
                SET [TenantId] = 'default' 
                WHERE [TenantId] = '' OR [TenantId] IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚操作：将默认租户ID设置回空字符串
            // 注意：这个操作会丢失数据，仅在需要完全回滚时使用
            
            migrationBuilder.Sql(@"
                -- 回滚 RolePermissions 表的租户ID设置
                UPDATE [RolePermissions] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");

            migrationBuilder.Sql(@"
                -- 回滚 RefreshTokens 表的租户ID设置
                UPDATE [RefreshTokens] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");

            migrationBuilder.Sql(@"
                -- 回滚 LoginLogs 表的租户ID设置
                UPDATE [LoginLogs] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");

            migrationBuilder.Sql(@"
                -- 回滚 AuditLogs 表的租户ID设置
                UPDATE [AuditLogs] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");

            migrationBuilder.Sql(@"
                -- 回滚 ApplicationUserRole 表的租户ID设置
                UPDATE [ApplicationUserRole] 
                SET [TenantId] = '' 
                WHERE [TenantId] = 'default';
            ");
        }
    }
}
