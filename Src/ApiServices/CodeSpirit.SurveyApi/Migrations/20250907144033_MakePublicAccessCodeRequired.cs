using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.SurveyApi.Migrations
{
    /// <inheritdoc />
    public partial class MakePublicAccessCodeRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Surveys_PublicAccessCode",
                table: "Surveys");

            migrationBuilder.AlterColumn<string>(
                name: "PublicAccessCode",
                table: "Surveys",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_PublicAccessCode",
                table: "Surveys",
                column: "PublicAccessCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Surveys_PublicAccessCode",
                table: "Surveys");

            migrationBuilder.AlterColumn<string>(
                name: "PublicAccessCode",
                table: "Surveys",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_PublicAccessCode",
                table: "Surveys",
                column: "PublicAccessCode",
                unique: true,
                filter: "[PublicAccessCode] IS NOT NULL");
        }
    }
}
