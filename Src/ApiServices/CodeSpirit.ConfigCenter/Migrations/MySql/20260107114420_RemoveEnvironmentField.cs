using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ConfigCenter.Migrations.MySql
{
    /// <inheritdoc />
    public partial class RemoveEnvironmentField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Environment",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "ConfigPublishHistorys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Environment",
                table: "Configs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "ConfigPublishHistorys",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
