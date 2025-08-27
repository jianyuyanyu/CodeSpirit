using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.SurveyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddLLMRawOutputToSurvey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LLMRawOutput",
                table: "Surveys",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LLMRawOutput",
                table: "Surveys");
        }
    }
}
