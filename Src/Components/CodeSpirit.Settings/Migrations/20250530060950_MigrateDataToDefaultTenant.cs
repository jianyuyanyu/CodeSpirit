using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.Settings.Migrations
{
    /// <inheritdoc />
    public partial class MigrateDataToDefaultTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 为现有的SettingItems设置默认租户ID
            migrationBuilder.Sql(@"
                UPDATE SettingItems 
                SET TenantId = 'default' 
                WHERE TenantId = '' OR TenantId IS NULL;
            ");

            // 为现有的SettingHistories设置默认租户ID
            migrationBuilder.Sql(@"
                UPDATE SettingHistories 
                SET TenantId = 'default' 
                WHERE TenantId = '' OR TenantId IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚操作：将默认租户ID设置为空
            migrationBuilder.Sql(@"
                UPDATE SettingItems 
                SET TenantId = '' 
                WHERE TenantId = 'default';
            ");

            migrationBuilder.Sql(@"
                UPDATE SettingHistories 
                SET TenantId = '' 
                WHERE TenantId = 'default';
            ");
        }
    }
}
