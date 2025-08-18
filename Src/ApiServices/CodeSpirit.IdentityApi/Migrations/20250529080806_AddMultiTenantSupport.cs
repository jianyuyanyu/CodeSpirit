using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 首先创建租户表
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Strategy = table.Column<int>(type: "int", nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TablePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThemeConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    StorageLimit = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            // 2. 创建租户表索引
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Domain",
                table: "Tenants",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ExpiresAt",
                table: "Tenants",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsActive",
                table: "Tenants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants",
                column: "TenantId",
                unique: true);

            // 3. 插入默认租户
            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "TenantId", "Name", "DisplayName", "Description", "Strategy", "IsActive", "Configuration", "ThemeConfig", "MaxUsers", "StorageLimit", "CreatedAt", "CreatedBy", "IsDeleted" },
                values: new object[] { 
                    Guid.NewGuid().ToString(), 
                    "default", 
                    "默认租户", 
                    "默认租户", 
                    "系统默认租户，用于迁移现有数据", 
                    1, // SharedDatabase
                    true, 
                    "{}", 
                    "{}", 
                    10000, 
                    102400L, // 100GB
                    DateTime.UtcNow, 
                    1L, // 假设系统管理员ID为1
                    false 
                });

            // 4. 为用户表添加TenantId列（允许为空，稍后更新）
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ApplicationUser",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // 5. 为角色表添加多租户和审计字段（允许为空，稍后更新）
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ApplicationRole",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ApplicationRole",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedBy",
                table: "ApplicationRole",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ApplicationRole",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedBy",
                table: "ApplicationRole",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ApplicationRole",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ApplicationRole",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ApplicationRole",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedBy",
                table: "ApplicationRole",
                type: "bigint",
                nullable: true);

            // 6. 更新现有用户数据，设置为默认租户
            migrationBuilder.Sql(@"
                UPDATE ApplicationUser 
                SET TenantId = 'default' 
                WHERE TenantId IS NULL OR TenantId = ''
            ");

            // 7. 更新现有角色数据，设置为默认租户和审计信息
            migrationBuilder.Sql(@"
                UPDATE ApplicationRole 
                SET TenantId = 'default',
                    CreatedAt = GETUTCDATE(),
                    CreatedBy = 1
                WHERE TenantId IS NULL OR TenantId = ''
            ");

            // 8. 将TenantId列设置为不允许为空
            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "ApplicationUser",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "ApplicationRole",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // 9. 将角色表的审计字段设置为不允许为空
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ApplicationRole",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CreatedBy",
                table: "ApplicationRole",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationUser");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ApplicationRole");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ApplicationRole");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ApplicationRole");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ApplicationRole");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ApplicationRole");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ApplicationRole");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationRole");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ApplicationRole");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ApplicationRole");
        }
    }
}
