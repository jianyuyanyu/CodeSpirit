using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.SurveyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPreviewCheckedToSurvey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPreviewChecked",
                table: "Surveys",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPreviewChecked",
                table: "Surveys");
        }
    }
}
