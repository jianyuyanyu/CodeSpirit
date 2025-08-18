using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations
{
    /// <inheritdoc />
    public partial class FixIdNoMultiTenantIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 删除旧的IdNo全局唯一索引
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUser_IdNo' AND object_id = OBJECT_ID('ApplicationUser'))
                    DROP INDEX IX_ApplicationUser_IdNo ON ApplicationUser;
            ");

            // 创建新的租户感知的IdNo复合唯一索引（主要目标）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUser_TenantId_IdNo' AND object_id = OBJECT_ID('ApplicationUser'))
                    CREATE UNIQUE INDEX IX_ApplicationUser_TenantId_IdNo ON ApplicationUser (TenantId, IdNo) WHERE [IdNo] IS NOT NULL;
            ");

            // 创建ApplicationUserRole相关索引（如果不存在）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_TenantId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    CREATE INDEX IX_ApplicationUserRole_TenantId ON ApplicationUserRole (TenantId);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_TenantId_UserId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    CREATE INDEX IX_ApplicationUserRole_TenantId_UserId ON ApplicationUserRole (TenantId, UserId);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_UserId_RoleId_TenantId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    CREATE UNIQUE INDEX IX_ApplicationUserRole_UserId_RoleId_TenantId ON ApplicationUserRole (UserId, RoleId, TenantId);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除新创建的索引
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_TenantId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    DROP INDEX IX_ApplicationUserRole_TenantId ON ApplicationUserRole;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_TenantId_UserId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    DROP INDEX IX_ApplicationUserRole_TenantId_UserId ON ApplicationUserRole;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_UserId_RoleId_TenantId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    DROP INDEX IX_ApplicationUserRole_UserId_RoleId_TenantId ON ApplicationUserRole;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUser_TenantId_IdNo' AND object_id = OBJECT_ID('ApplicationUser'))
                    DROP INDEX IX_ApplicationUser_TenantId_IdNo ON ApplicationUser;
            ");

            // 恢复原来的IdNo全局唯一索引
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUser_IdNo' AND object_id = OBJECT_ID('ApplicationUser'))
                    CREATE UNIQUE INDEX IX_ApplicationUser_IdNo ON ApplicationUser (IdNo) WHERE [IdNo] IS NOT NULL;
            ");
        }
    }
}
