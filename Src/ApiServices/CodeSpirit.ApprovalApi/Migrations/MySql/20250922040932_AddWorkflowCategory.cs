using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ApprovalApi.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddWorkflowCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "WorkflowDefinitions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowCategories_WorkflowCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "WorkflowCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_CategoryId",
                table: "WorkflowDefinitions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCategories_ParentId",
                table: "WorkflowCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCategories_TenantId",
                table: "WorkflowCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCategories_TenantId_Id",
                table: "WorkflowCategories",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCategories_TenantId_Name",
                table: "WorkflowCategories",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCategories_TenantId_ParentId",
                table: "WorkflowCategories",
                columns: new[] { "TenantId", "ParentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowDefinitions_WorkflowCategories_CategoryId",
                table: "WorkflowDefinitions",
                column: "CategoryId",
                principalTable: "WorkflowCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowDefinitions_WorkflowCategories_CategoryId",
                table: "WorkflowDefinitions");

            migrationBuilder.DropTable(
                name: "WorkflowCategories");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowDefinitions_CategoryId",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "WorkflowDefinitions");
        }
    }
}
