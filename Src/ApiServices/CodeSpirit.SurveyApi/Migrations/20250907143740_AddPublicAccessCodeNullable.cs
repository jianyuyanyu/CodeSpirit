using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.SurveyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicAccessCodeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicAccessCode",
                table: "Surveys",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShareExpiresAt",
                table: "Surveys",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_PublicAccessCode",
                table: "Surveys",
                column: "PublicAccessCode",
                unique: true,
                filter: "[PublicAccessCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Surveys_PublicAccessCode",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "PublicAccessCode",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "ShareExpiresAt",
                table: "Surveys");
        }
    }
}
