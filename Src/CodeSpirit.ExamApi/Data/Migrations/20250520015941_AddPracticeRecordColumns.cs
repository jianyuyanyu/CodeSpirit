using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeRecordColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMarked",
                table: "PracticeRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PracticeSessionId",
                table: "PracticeRecords",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "PracticeSession",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    PracticeSettingId = table.Column<long>(type: "bigint", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PracticeSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeSession_PracticeSettings_PracticeSettingId",
                        column: x => x.PracticeSettingId,
                        principalTable: "PracticeSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PracticeSession_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRecords_PracticeSessionId",
                table: "PracticeRecords",
                column: "PracticeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSession_PracticeSettingId",
                table: "PracticeSession",
                column: "PracticeSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSession_StudentId",
                table: "PracticeSession",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeRecords_PracticeSession_PracticeSessionId",
                table: "PracticeRecords",
                column: "PracticeSessionId",
                principalTable: "PracticeSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeRecords_PracticeSession_PracticeSessionId",
                table: "PracticeRecords");

            migrationBuilder.DropTable(
                name: "PracticeSession");

            migrationBuilder.DropIndex(
                name: "IX_PracticeRecords_PracticeSessionId",
                table: "PracticeRecords");

            migrationBuilder.DropColumn(
                name: "IsMarked",
                table: "PracticeRecords");

            migrationBuilder.DropColumn(
                name: "PracticeSessionId",
                table: "PracticeRecords");
        }
    }
}
