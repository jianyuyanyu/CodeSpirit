using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreConversionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScoreConverted",
                table: "ExamRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "OriginalScore",
                table: "ExamRecords",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreConversionRatio",
                table: "ExamRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConversionDecimalPlaces",
                table: "ExamPapers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ConversionRatio",
                table: "ExamPapers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConversionTargetFullScore",
                table: "ExamPapers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableScoreConversion",
                table: "ExamPapers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalPassScore",
                table: "ExamPapers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsScoreConverted",
                table: "ExamRecords");

            migrationBuilder.DropColumn(
                name: "OriginalScore",
                table: "ExamRecords");

            migrationBuilder.DropColumn(
                name: "ScoreConversionRatio",
                table: "ExamRecords");

            migrationBuilder.DropColumn(
                name: "ConversionDecimalPlaces",
                table: "ExamPapers");

            migrationBuilder.DropColumn(
                name: "ConversionRatio",
                table: "ExamPapers");

            migrationBuilder.DropColumn(
                name: "ConversionTargetFullScore",
                table: "ExamPapers");

            migrationBuilder.DropColumn(
                name: "EnableScoreConversion",
                table: "ExamPapers");

            migrationBuilder.DropColumn(
                name: "OriginalPassScore",
                table: "ExamPapers");
        }
    }
}
