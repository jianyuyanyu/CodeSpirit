using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.ExamApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamPapers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    PassScore = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    RandomRules = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    AverageScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PassRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_ExamPapers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_QuestionCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionCategories_QuestionCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "QuestionCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_StudentGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StudentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExamPaperId = table.Column<long>(type: "bigint", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    AllowedAttempts = table.Column<int>(type: "int", nullable: false),
                    EnableRandomQuestionOrder = table.Column<bool>(type: "bit", nullable: false),
                    EnableRandomOptionOrder = table.Column<bool>(type: "bit", nullable: false),
                    AllowedScreenSwitchCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ExamSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSettings_ExamPapers_ExamPaperId",
                        column: x => x.ExamPaperId,
                        principalTable: "ExamPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Analysis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    KnowledgePoints = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    DefaultScore = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CorrectRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_QuestionCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "QuestionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentGroupMappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_StudentGroupMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGroupMappings_StudentGroups_StudentGroupId",
                        column: x => x.StudentGroupId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentGroupMappings_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ExamSettingId = table.Column<long>(type: "bigint", nullable: false),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: true),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    ScreenSwitchCount = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BrowserInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CheatingSuspicionLevel = table.Column<int>(type: "int", nullable: false),
                    CheatingSuspicionRecord = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GraderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GradedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ExamRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamRecords_ExamSettings_ExamSettingId",
                        column: x => x.ExamSettingId,
                        principalTable: "ExamSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "PracticeRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    PracticeType = table.Column<int>(type: "int", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    TimeSpent = table.Column<int>(type: "int", nullable: false),
                    PracticeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MockExamId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_PracticeRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeRecords_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Options = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Analysis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    KnowledgePoints = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultScore = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_QuestionVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionVersions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WrongQuestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    WrongCount = table.Column<int>(type: "int", nullable: false),
                    LastWrongAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastWrongTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_WrongQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WrongQuestions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WrongQuestions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamAnswerRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ExamRecordId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsMarked = table.Column<bool>(type: "bit", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    Score = table.Column<double>(type: "float", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GradedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GraderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_ExamAnswerRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAnswerRecords_ExamRecords_ExamRecordId",
                        column: x => x.ExamRecordId,
                        principalTable: "ExamRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAnswerRecords_QuestionVersions_QuestionVersionId",
                        column: x => x.QuestionVersionId,
                        principalTable: "QuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAnswerRecords_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamPaperQuestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ExamPaperId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ExamPaperQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamPaperQuestions_ExamPapers_ExamPaperId",
                        column: x => x.ExamPaperId,
                        principalTable: "ExamPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamPaperQuestions_QuestionVersions_QuestionVersionId",
                        column: x => x.QuestionVersionId,
                        principalTable: "QuestionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamPaperQuestions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerRecords_ExamRecordId_OrderNumber",
                table: "ExamAnswerRecords",
                columns: new[] { "ExamRecordId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerRecords_QuestionId",
                table: "ExamAnswerRecords",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerRecords_QuestionVersionId",
                table: "ExamAnswerRecords",
                column: "QuestionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperQuestions_ExamPaperId_OrderNumber",
                table: "ExamPaperQuestions",
                columns: new[] { "ExamPaperId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperQuestions_QuestionId",
                table: "ExamPaperQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperQuestions_QuestionVersionId",
                table: "ExamPaperQuestions",
                column: "QuestionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRecords_ExamSettingId",
                table: "ExamRecords",
                column: "ExamSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRecords_StudentId",
                table: "ExamRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettings_ExamPaperId",
                table: "ExamSettings",
                column: "ExamPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSettingStudentGroup_StudentGroupsId",
                table: "ExamSettingStudentGroup",
                column: "StudentGroupsId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRecords_QuestionId",
                table: "PracticeRecords",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRecords_StudentId",
                table: "PracticeRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_ParentId",
                table: "QuestionCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CategoryId",
                table: "Questions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionVersions_QuestionId_Version",
                table: "QuestionVersions",
                columns: new[] { "QuestionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroupMappings_StudentGroupId",
                table: "StudentGroupMappings",
                column: "StudentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroupMappings_StudentId",
                table: "StudentGroupMappings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_WrongQuestions_QuestionId",
                table: "WrongQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WrongQuestions_StudentId",
                table: "WrongQuestions",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamAnswerRecords");

            migrationBuilder.DropTable(
                name: "ExamPaperQuestions");

            migrationBuilder.DropTable(
                name: "ExamSettingStudentGroup");

            migrationBuilder.DropTable(
                name: "PracticeRecords");

            migrationBuilder.DropTable(
                name: "StudentGroupMappings");

            migrationBuilder.DropTable(
                name: "WrongQuestions");

            migrationBuilder.DropTable(
                name: "ExamRecords");

            migrationBuilder.DropTable(
                name: "QuestionVersions");

            migrationBuilder.DropTable(
                name: "StudentGroups");

            migrationBuilder.DropTable(
                name: "ExamSettings");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "ExamPapers");

            migrationBuilder.DropTable(
                name: "QuestionCategories");
        }
    }
}
