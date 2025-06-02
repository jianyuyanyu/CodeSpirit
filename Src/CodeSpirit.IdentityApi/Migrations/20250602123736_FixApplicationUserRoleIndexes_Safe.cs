using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations
{
    /// <inheritdoc />
    public partial class FixApplicationUserRoleIndexes_Safe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 首先删除可能存在的冲突索引（如果存在的话）
            migrationBuilder.Sql(@"
                -- 删除可能存在的旧索引
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_TenantId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    DROP INDEX IX_ApplicationUserRole_TenantId ON ApplicationUserRole;
                
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_TenantId_UserId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    DROP INDEX IX_ApplicationUserRole_TenantId_UserId ON ApplicationUserRole;
                
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_UserId_RoleId_TenantId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    DROP INDEX IX_ApplicationUserRole_UserId_RoleId_TenantId ON ApplicationUserRole;
                
                -- 删除可能存在的旧的不完整索引
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApplicationUserRole_UserId_RoleId' AND object_id = OBJECT_ID('ApplicationUserRole'))
                    DROP INDEX IX_ApplicationUserRole_UserId_RoleId ON ApplicationUserRole;
            ");

            // 清理可能存在的重复数据
            migrationBuilder.Sql(@"
                -- 删除重复的用户角色关联（保留最早创建的记录）
                WITH CTE AS (
                    SELECT 
                        UserId, 
                        RoleId, 
                        TenantId,
                        ROW_NUMBER() OVER (PARTITION BY UserId, RoleId, TenantId ORDER BY CreatedAt) as rn
                    FROM ApplicationUserRole
                    WHERE TenantId IS NOT NULL AND TenantId != ''
                )
                DELETE FROM ApplicationUserRole 
                WHERE EXISTS (
                    SELECT 1 FROM CTE 
                    WHERE CTE.UserId = ApplicationUserRole.UserId 
                    AND CTE.RoleId = ApplicationUserRole.RoleId 
                    AND CTE.TenantId = ApplicationUserRole.TenantId 
                    AND CTE.rn > 1
                );
            ");

            // 修复空的或NULL的TenantId（设置为默认租户）
            migrationBuilder.Sql(@"
                -- 将空的TenantId设置为默认租户
                UPDATE ApplicationUserRole 
                SET TenantId = 'default' 
                WHERE TenantId IS NULL OR TenantId = '';
            ");

            // 创建新的正确索引
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRole_TenantId",
                table: "ApplicationUserRole",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRole_TenantId_UserId",
                table: "ApplicationUserRole",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRole_UserId_RoleId_TenantId",
                table: "ApplicationUserRole",
                columns: new[] { "UserId", "RoleId", "TenantId" },
                unique: true);

            // 注释：此迁移已经创建了ApplicationUserRole表所需的所有索引，
            // 包括多租户支持和唯一性约束，与ApplicationDbContext中定义的索引完全匹配
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除我们创建的索引
            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserRole_TenantId",
                table: "ApplicationUserRole");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserRole_TenantId_UserId",
                table: "ApplicationUserRole");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserRole_UserId_RoleId_TenantId",
                table: "ApplicationUserRole");
        }
    }
}
