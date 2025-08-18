using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations
{
    /// <inheritdoc />
    public partial class FixMultiTenantIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 删除现有的唯一索引
            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "ApplicationRole");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "ApplicationUser");

            // 创建新的复合唯一索引，包含TenantId以支持多租户
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRole_TenantId_NormalizedName",
                table: "ApplicationRole",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUser_TenantId_NormalizedUserName",
                table: "ApplicationUser", 
                columns: new[] { "TenantId", "NormalizedUserName" },
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            // 重新创建旧的索引名称作为别名（可选，为了兼容性）
            migrationBuilder.Sql(@"
                -- 为角色名称创建别名索引
                CREATE NONCLUSTERED INDEX [RoleNameIndex] 
                ON [ApplicationRole] ([NormalizedName], [TenantId])
                WHERE [NormalizedName] IS NOT NULL;

                -- 为用户名创建别名索引  
                CREATE NONCLUSTERED INDEX [UserNameIndex]
                ON [ApplicationUser] ([NormalizedUserName], [TenantId])
                WHERE [NormalizedUserName] IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除新的复合索引
            migrationBuilder.DropIndex(
                name: "IX_ApplicationRole_TenantId_NormalizedName",
                table: "ApplicationRole");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUser_TenantId_NormalizedUserName",
                table: "ApplicationUser");

            // 删除别名索引
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS [RoleNameIndex] ON [ApplicationRole];
                DROP INDEX IF EXISTS [UserNameIndex] ON [ApplicationUser];
            ");

            // 恢复原来的唯一索引
            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "ApplicationRole",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "ApplicationUser",
                column: "NormalizedUserName", 
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }
    }
}
