using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ApprovalApi.Migrations.SqlServer
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
                    table.PrimaryKey("PK_WorkflowCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowCategories_WorkflowCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "WorkflowCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
