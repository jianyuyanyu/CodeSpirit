using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSettingNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableQuestionAnalysis",
                table: "ExamSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MinExamTime",
                table: "ExamSettings",
                type: "int",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableQuestionAnalysis",
                table: "ExamSettings");

            migrationBuilder.DropColumn(
                name: "MinExamTime",
                table: "ExamSettings");
        }
    }
}
