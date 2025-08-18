using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 为所有表的TenantId字段创建索引以提高查询性能
            
            // 1. RolePermissions 表索引
            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_TenantId",
                table: "RolePermissions",
                column: "TenantId");

            // 2. RefreshTokens 表索引
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TenantId",
                table: "RefreshTokens",
                column: "TenantId");

            // 3. LoginLogs 表索引
            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_TenantId",
                table: "LoginLogs",
                column: "TenantId");

            // 4. AuditLogs 表索引
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            // 5. ApplicationUserRole 表索引
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRole_TenantId",
                table: "ApplicationUserRole",
                column: "TenantId");

            // 常用复合索引优化查询性能
            
            // RefreshTokens: 按租户和用户查询令牌
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TenantId_UserId",
                table: "RefreshTokens",
                columns: new[] { "TenantId", "UserId" });

            // LoginLogs: 按租户和用户查询登录日志
            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_TenantId_UserId",
                table: "LoginLogs",
                columns: new[] { "TenantId", "UserId" });

            // ApplicationUserRole: 按租户和用户查询角色
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRole_TenantId_UserId",
                table: "ApplicationUserRole",
                columns: new[] { "TenantId", "UserId" });

            // ApplicationUserRole: 按租户和角色查询用户
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRole_TenantId_RoleId",
                table: "ApplicationUserRole",
                columns: new[] { "TenantId", "RoleId" });

            // AuditLogs: 按租户和事件时间查询（最常用的查询）
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_EventTime",
                table: "AuditLogs",
                columns: new[] { "TenantId", "EventTime" });

            // LoginLogs: 按租户和登录时间查询
            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_TenantId_LoginTime",
                table: "LoginLogs",
                columns: new[] { "TenantId", "LoginTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除复合索引
            migrationBuilder.DropIndex(
                name: "IX_LoginLogs_TenantId_LoginTime",
                table: "LoginLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_EventTime",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserRole_TenantId_RoleId",
                table: "ApplicationUserRole");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserRole_TenantId_UserId",
                table: "ApplicationUserRole");

            migrationBuilder.DropIndex(
                name: "IX_LoginLogs_TenantId_UserId",
                table: "LoginLogs");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TenantId_UserId",
                table: "RefreshTokens");

            // 删除单独的TenantId索引
            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserRole_TenantId",
                table: "ApplicationUserRole");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_LoginLogs_TenantId",
                table: "LoginLogs");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TenantId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_TenantId",
                table: "RolePermissions");
        }
    }
}
