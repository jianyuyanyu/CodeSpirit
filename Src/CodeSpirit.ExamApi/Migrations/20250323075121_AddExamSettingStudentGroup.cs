using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSettingStudentGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSettingStudentGroup");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ExamSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ExamSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ExamSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "StudentGroupId",
                table: "ExamSettings",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExamSettingStudentGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ExamSettingId = table.Column<long>(type: "bigint", nullable: false),
                    StudentGroupId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_ExamSettingStudentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSettingStudentGroups_ExamSettings_ExamSettingId",
                        column: x => x.ExamSettingId,
                        principalTable: "ExamSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSettingStudentGroups_StudentGroups_StudentGroupId",
                        column: x => x.StudentGroupId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettings_StudentGroupId",
                table: "ExamSettings",
                column: "StudentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettingStudentGroups_ExamSettingId",
                table: "ExamSettingStudentGroups",
                column: "ExamSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettingStudentGroups_StudentGroupId",
                table: "ExamSettingStudentGroups",
                column: "StudentGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamSettings_StudentGroups_StudentGroupId",
                table: "ExamSettings",
                column: "StudentGroupId",
                principalTable: "StudentGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamSettings_StudentGroups_StudentGroupId",
                table: "ExamSettings");

            migrationBuilder.DropTable(
                name: "ExamSettingStudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_ExamSettings_StudentGroupId",
                table: "ExamSettings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ExamSettings");

            migrationBuilder.DropColumn(
                name: "StudentGroupId",
                table: "ExamSettings");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ExamSettings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ExamSettings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ExamSettingStudentGroup",
                columns: table => new
                {
                    ExamSettingsId = table.Column<long>(type: "bigint", nullable: false),
                    StudentGroupsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSettingStudentGroup", x => new { x.ExamSettingsId, x.StudentGroupsId });
                    table.ForeignKey(
                        name: "FK_ExamSettingStudentGroup_ExamSettings_ExamSettingsId",
                        column: x => x.ExamSettingsId,
                        principalTable: "ExamSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSettingStudentGroup_StudentGroups_StudentGroupsId",
                        column: x => x.StudentGroupsId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettingStudentGroup_StudentGroupsId",
                table: "ExamSettingStudentGroup",
                column: "StudentGroupsId");
        }
    }
}
