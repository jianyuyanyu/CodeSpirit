using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ConfigCenter.Migrations
{
    /// <inheritdoc />
    public partial class Remove_ConfigItems_IsEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "OnlineStatus",
                table: "Configs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Configs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnlineStatus",
                table: "Configs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
