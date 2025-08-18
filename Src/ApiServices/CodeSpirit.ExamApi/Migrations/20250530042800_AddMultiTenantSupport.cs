using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "WrongQuestions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Students",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "StudentGroups",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "StudentGroupMappings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "QuestionVersions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Questions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "QuestionCategories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "PracticeSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "PracticeSession",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "PracticeRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ExamSettingStudentGroups",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ExamSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ExamRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ExamPapers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ExamPaperQuestions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ExamAnswerRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // 在创建索引之前，先更新所有空值数据
            // 更新Students表的空TenantId
            migrationBuilder.Sql(
                "UPDATE [Students] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新Students表的空StudentNumber - 为每个空值生成唯一的学号
            migrationBuilder.Sql(@"
                WITH EmptyStudentNumbers AS (
                    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) as RowNum
                    FROM [Students] 
                    WHERE [StudentNumber] = '' OR [StudentNumber] IS NULL
                )
                UPDATE s 
                SET [StudentNumber] = 'STU' + RIGHT('000000' + CAST(e.RowNum as varchar), 6)
                FROM [Students] s
                INNER JOIN EmptyStudentNumbers e ON s.Id = e.Id;");

            // 更新其他表的空TenantId值
            migrationBuilder.Sql(
                "UPDATE [Questions] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [QuestionCategories] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [QuestionVersions] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [ExamPapers] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [ExamPaperQuestions] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [ExamSettings] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [ExamSettingStudentGroups] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [ExamRecords] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [ExamAnswerRecords] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [StudentGroups] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [StudentGroupMappings] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [WrongQuestions] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [PracticeSettings] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [PracticeRecords] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            migrationBuilder.Sql(
                "UPDATE [PracticeSession] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_WrongQuestions_TenantId",
                table: "WrongQuestions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WrongQuestions_TenantId_Id",
                table: "WrongQuestions",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_WrongQuestions_TenantId_StudentId",
                table: "WrongQuestions",
                columns: new[] { "TenantId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_TenantId",
                table: "Students",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_TenantId_Id",
                table: "Students",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_TenantId_StudentNumber",
                table: "Students",
                columns: new[] { "TenantId", "StudentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_TenantId",
                table: "StudentGroups",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_TenantId_Id",
                table: "StudentGroups",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroupMappings_TenantId",
                table: "StudentGroupMappings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroupMappings_TenantId_Id",
                table: "StudentGroupMappings",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionVersions_TenantId",
                table: "QuestionVersions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionVersions_TenantId_Id",
                table: "QuestionVersions",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TenantId",
                table: "Questions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TenantId_CategoryId",
                table: "Questions",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TenantId_Id",
                table: "Questions",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_TenantId",
                table: "QuestionCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_TenantId_Id",
                table: "QuestionCategories",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSettings_TenantId",
                table: "PracticeSettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSettings_TenantId_Id",
                table: "PracticeSettings",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSession_TenantId",
                table: "PracticeSession",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSession_TenantId_Id",
                table: "PracticeSession",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRecords_TenantId",
                table: "PracticeRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRecords_TenantId_Id",
                table: "PracticeRecords",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRecords_TenantId_StudentId",
                table: "PracticeRecords",
                columns: new[] { "TenantId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettingStudentGroups_TenantId",
                table: "ExamSettingStudentGroups",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettingStudentGroups_TenantId_Id",
                table: "ExamSettingStudentGroups",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettings_TenantId",
                table: "ExamSettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettings_TenantId_Id",
                table: "ExamSettings",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamRecords_TenantId",
                table: "ExamRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRecords_TenantId_ExamSettingId",
                table: "ExamRecords",
                columns: new[] { "TenantId", "ExamSettingId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamRecords_TenantId_Id",
                table: "ExamRecords",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamRecords_TenantId_StudentId",
                table: "ExamRecords",
                columns: new[] { "TenantId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_TenantId",
                table: "ExamPapers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_TenantId_Id",
                table: "ExamPapers",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperQuestions_TenantId",
                table: "ExamPaperQuestions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperQuestions_TenantId_Id",
                table: "ExamPaperQuestions",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerRecords_TenantId",
                table: "ExamAnswerRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerRecords_TenantId_Id",
                table: "ExamAnswerRecords",
                columns: new[] { "TenantId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WrongQuestions_TenantId",
                table: "WrongQuestions");

            migrationBuilder.DropIndex(
                name: "IX_WrongQuestions_TenantId_Id",
                table: "WrongQuestions");

            migrationBuilder.DropIndex(
                name: "IX_WrongQuestions_TenantId_StudentId",
                table: "WrongQuestions");

            migrationBuilder.DropIndex(
                name: "IX_Students_TenantId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_TenantId_Id",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_TenantId_StudentNumber",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_TenantId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_TenantId_Id",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroupMappings_TenantId",
                table: "StudentGroupMappings");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroupMappings_TenantId_Id",
                table: "StudentGroupMappings");

            migrationBuilder.DropIndex(
                name: "IX_QuestionVersions_TenantId",
                table: "QuestionVersions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionVersions_TenantId_Id",
                table: "QuestionVersions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_TenantId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_TenantId_CategoryId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_TenantId_Id",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionCategories_TenantId",
                table: "QuestionCategories");

            migrationBuilder.DropIndex(
                name: "IX_QuestionCategories_TenantId_Id",
                table: "QuestionCategories");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSettings_TenantId",
                table: "PracticeSettings");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSettings_TenantId_Id",
                table: "PracticeSettings");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSession_TenantId",
                table: "PracticeSession");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSession_TenantId_Id",
                table: "PracticeSession");

            migrationBuilder.DropIndex(
                name: "IX_PracticeRecords_TenantId",
                table: "PracticeRecords");

            migrationBuilder.DropIndex(
                name: "IX_PracticeRecords_TenantId_Id",
                table: "PracticeRecords");

            migrationBuilder.DropIndex(
                name: "IX_PracticeRecords_TenantId_StudentId",
                table: "PracticeRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExamSettingStudentGroups_TenantId",
                table: "ExamSettingStudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_ExamSettingStudentGroups_TenantId_Id",
                table: "ExamSettingStudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_ExamSettings_TenantId",
                table: "ExamSettings");

            migrationBuilder.DropIndex(
                name: "IX_ExamSettings_TenantId_Id",
                table: "ExamSettings");

            migrationBuilder.DropIndex(
                name: "IX_ExamRecords_TenantId",
                table: "ExamRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExamRecords_TenantId_ExamSettingId",
                table: "ExamRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExamRecords_TenantId_Id",
                table: "ExamRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExamRecords_TenantId_StudentId",
                table: "ExamRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExamPapers_TenantId",
                table: "ExamPapers");

            migrationBuilder.DropIndex(
                name: "IX_ExamPapers_TenantId_Id",
                table: "ExamPapers");

            migrationBuilder.DropIndex(
                name: "IX_ExamPaperQuestions_TenantId",
                table: "ExamPaperQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ExamPaperQuestions_TenantId_Id",
                table: "ExamPaperQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ExamAnswerRecords_TenantId",
                table: "ExamAnswerRecords");

            migrationBuilder.DropIndex(
                name: "IX_ExamAnswerRecords_TenantId_Id",
                table: "ExamAnswerRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WrongQuestions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StudentGroupMappings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "QuestionVersions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "QuestionCategories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PracticeSettings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PracticeSession");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PracticeRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ExamSettingStudentGroups");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ExamSettings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ExamRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ExamPapers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ExamPaperQuestions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ExamAnswerRecords");
        }
    }
}
