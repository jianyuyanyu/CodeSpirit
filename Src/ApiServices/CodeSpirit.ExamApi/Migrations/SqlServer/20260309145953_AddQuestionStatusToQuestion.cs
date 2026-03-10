using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddQuestionStatusToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Questions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PublishedBy",
                table: "Questions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 2);

            // 历史数据迁移：将已有题目更新为已发布，填充发布时间和发布人
            migrationBuilder.Sql(
                "UPDATE Questions SET PublishedAt = COALESCE(UpdatedAt, CreatedAt), PublishedBy = COALESCE(UpdatedBy, CreatedBy) WHERE PublishedAt IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Questions");
        }
    }
}
