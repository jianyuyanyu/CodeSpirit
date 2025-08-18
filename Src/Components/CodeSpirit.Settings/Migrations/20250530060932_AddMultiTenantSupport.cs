using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.Settings.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "SettingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "SettingHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SettingItems_TenantId",
                table: "SettingItems");

            migrationBuilder.DropIndex(
                name: "IX_SettingItems_TenantId_Id",
                table: "SettingItems");

            migrationBuilder.DropIndex(
                name: "IX_SettingItems_TenantId_Module_Key",
                table: "SettingItems");

            migrationBuilder.DropIndex(
                name: "IX_SettingItems_TenantId_Module_Scope_ScopeId",
                table: "SettingItems");

            migrationBuilder.DropIndex(
                name: "IX_SettingHistories_TenantId",
                table: "SettingHistories");

            migrationBuilder.DropIndex(
                name: "IX_SettingHistories_TenantId_Id",
                table: "SettingHistories");

            migrationBuilder.DropIndex(
                name: "IX_SettingHistories_TenantId_SettingId",
                table: "SettingHistories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SettingItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SettingHistories");
        }
    }
}
