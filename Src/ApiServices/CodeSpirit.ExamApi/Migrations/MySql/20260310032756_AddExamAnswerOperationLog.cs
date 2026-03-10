using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddExamAnswerOperationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamAnswerOperationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ExamRecordId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    Answer = table.Column<string>(type: "text", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_ExamAnswerOperationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAnswerOperationLogs_ExamRecords_ExamRecordId",
                        column: x => x.ExamRecordId,
                        principalTable: "ExamRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAnswerOperationLogs_QuestionVersions_QuestionVersionId",
                        column: x => x.QuestionVersionId,
                        principalTable: "QuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerOperationLogs_ExamRecordId_OperationTime",
                table: "ExamAnswerOperationLogs",
                columns: new[] { "ExamRecordId", "OperationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerOperationLogs_QuestionVersionId",
                table: "ExamAnswerOperationLogs",
                column: "QuestionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerOperationLogs_TenantId",
                table: "ExamAnswerOperationLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerOperationLogs_TenantId_Id",
                table: "ExamAnswerOperationLogs",
                columns: new[] { "TenantId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamAnswerOperationLogs");
        }
    }
}
