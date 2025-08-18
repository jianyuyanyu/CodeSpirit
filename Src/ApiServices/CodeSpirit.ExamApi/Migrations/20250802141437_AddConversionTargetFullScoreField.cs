using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class AddConversionTargetFullScoreField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConversionTargetFullScore",
                table: "ExamPapers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversionTargetFullScore",
                table: "ExamPapers");
        }
    }
}
