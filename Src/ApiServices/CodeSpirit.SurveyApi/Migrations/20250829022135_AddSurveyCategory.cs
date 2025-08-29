using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.SurveyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Surveys",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SurveyCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyCategories_SurveyCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SurveyCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_CategoryId",
                table: "Surveys",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyCategories_ParentId",
                table: "SurveyCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyCategories_TenantId",
                table: "SurveyCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyCategories_TenantId_IsEnabled",
                table: "SurveyCategories",
                columns: new[] { "TenantId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyCategories_TenantId_ParentId_OrderIndex",
                table: "SurveyCategories",
                columns: new[] { "TenantId", "ParentId", "OrderIndex" });

            migrationBuilder.AddForeignKey(
                name: "FK_Surveys_SurveyCategories_CategoryId",
                table: "Surveys",
                column: "CategoryId",
                principalTable: "SurveyCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Surveys_SurveyCategories_CategoryId",
                table: "Surveys");

            migrationBuilder.DropTable(
                name: "SurveyCategories");

            migrationBuilder.DropIndex(
                name: "IX_Surveys_CategoryId",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Surveys");
        }
    }
}
