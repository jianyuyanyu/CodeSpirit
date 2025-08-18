using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableViewResultToExamSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableViewResult",
                table: "ExamSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableViewResult",
                table: "ExamSettings");
        }
    }
}
