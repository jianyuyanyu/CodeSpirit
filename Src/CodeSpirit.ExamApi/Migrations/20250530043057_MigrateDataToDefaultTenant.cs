using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class MigrateDataToDefaultTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 将所有现有数据的TenantId设置为默认值'default'
            // 这个操作是幂等的，可以安全地重复执行
            
            // 更新学生表
            migrationBuilder.Sql(
                "UPDATE [Students] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新题目表
            migrationBuilder.Sql(
                "UPDATE [Questions] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新题目分类表
            migrationBuilder.Sql(
                "UPDATE [QuestionCategories] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新题目版本表
            migrationBuilder.Sql(
                "UPDATE [QuestionVersions] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新试卷表
            migrationBuilder.Sql(
                "UPDATE [ExamPapers] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新试卷题目关联表
            migrationBuilder.Sql(
                "UPDATE [ExamPaperQuestions] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新考试设置表
            migrationBuilder.Sql(
                "UPDATE [ExamSettings] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新考试设置学生组关联表
            migrationBuilder.Sql(
                "UPDATE [ExamSettingStudentGroups] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新考试记录表
            migrationBuilder.Sql(
                "UPDATE [ExamRecords] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新考试答题记录表
            migrationBuilder.Sql(
                "UPDATE [ExamAnswerRecords] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新学生组表
            migrationBuilder.Sql(
                "UPDATE [StudentGroups] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新学生组映射表
            migrationBuilder.Sql(
                "UPDATE [StudentGroupMappings] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新错题表
            migrationBuilder.Sql(
                "UPDATE [WrongQuestions] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新练习设置表
            migrationBuilder.Sql(
                "UPDATE [PracticeSettings] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新练习记录表
            migrationBuilder.Sql(
                "UPDATE [PracticeRecords] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
            
            // 更新练习会话表
            migrationBuilder.Sql(
                "UPDATE [PracticeSession] SET [TenantId] = 'default' WHERE [TenantId] = '' OR [TenantId] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚时将TenantId设置回空字符串
            // 注意：这可能会导致数据一致性问题，在生产环境中需要谨慎考虑
            
            migrationBuilder.Sql("UPDATE [Students] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [Questions] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [QuestionCategories] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [QuestionVersions] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [ExamPapers] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [ExamPaperQuestions] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [ExamSettings] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [ExamSettingStudentGroups] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [ExamRecords] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [ExamAnswerRecords] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [StudentGroups] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [StudentGroupMappings] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [WrongQuestions] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [PracticeSettings] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [PracticeRecords] SET [TenantId] = '' WHERE [TenantId] = 'default';");
            migrationBuilder.Sql("UPDATE [PracticeSession] SET [TenantId] = '' WHERE [TenantId] = 'default';");
        }
    }
}
