using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.Settings.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SettingItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScopeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsSystemDefault = table.Column<bool>(type: "bit", nullable: false),
                    Group = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Options = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettingHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SettingId = table.Column<long>(type: "bigint", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingHistories_SettingItems_SettingId",
                        column: x => x.SettingId,
                        principalTable: "SettingItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SettingHistories_SettingId",
                table: "SettingHistories",
                column: "SettingId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingHistories_TenantId",
                table: "SettingHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingHistories_TenantId_Id",
                table: "SettingHistories",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SettingHistories_TenantId_SettingId",
                table: "SettingHistories",
                columns: new[] { "TenantId", "SettingId" });

            migrationBuilder.CreateIndex(
                name: "IX_SettingHistories_Version",
                table: "SettingHistories",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_SettingItems_Module_Key",
                table: "SettingItems",
                columns: new[] { "Module", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettingItems_Module_Scope_ScopeId",
                table: "SettingItems",
                columns: new[] { "Module", "Scope", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SettingItems_TenantId",
                table: "SettingItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingItems_TenantId_Id",
                table: "SettingItems",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SettingItems_TenantId_Module_Key",
                table: "SettingItems",
                columns: new[] { "TenantId", "Module", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettingItems_TenantId_Module_Scope_ScopeId",
                table: "SettingItems",
                columns: new[] { "TenantId", "Module", "Scope", "ScopeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SettingHistories");

            migrationBuilder.DropTable(
                name: "SettingItems");
        }
    }
}
