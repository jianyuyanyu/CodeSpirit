using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.Settings.Migrations.MySql
{
    /// <inheritdoc />
    public partial class RemoveModuleKeyUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SettingItems_Module_Key",
                table: "SettingItems");

            migrationBuilder.CreateIndex(
                name: "IX_SettingItems_Module_Key",
                table: "SettingItems",
                columns: new[] { "Module", "Key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SettingItems_Module_Key",
                table: "SettingItems");

            migrationBuilder.CreateIndex(
                name: "IX_SettingItems_Module_Key",
                table: "SettingItems",
                columns: new[] { "Module", "Key" },
                unique: true);
        }
    }
}
