using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionExamPaperRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamPaperQuestions_ExamPapers_ExamPaperId",
                table: "ExamPaperQuestions");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamPaperQuestions_ExamPapers_ExamPaperId",
                table: "ExamPaperQuestions",
                column: "ExamPaperId",
                principalTable: "ExamPapers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamPaperQuestions_ExamPapers_ExamPaperId",
                table: "ExamPaperQuestions");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamPaperQuestions_ExamPapers_ExamPaperId",
                table: "ExamPaperQuestions",
                column: "ExamPaperId",
                principalTable: "ExamPapers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
