using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Services;
using CodeSpirit.ExamApi.Tests.TestBase;

namespace CodeSpirit.ExamApi.Tests.Services
{
    public class ClientServiceTests : ExamServiceTestBase
    {
        private readonly ClientService _clientService;
        private readonly Mock<ILogger<ClientService>> _mockLogger;
        private readonly IIdGenerator _idGenerator;

        public ClientServiceTests()
            : base()
        {
            // 设置额外依赖
            _mockLogger = new Mock<ILogger<ClientService>>();
            _idGenerator = new SnowflakeIdGenerator();

            // 初始化ClientService
            _clientService = new ClientService(
                DbContext,
                _mockLogger.Object,
                _idGenerator
            );

            // 准备测试数据
            SeedTestData();
        }

        /// <summary>
        /// 准备考试测试数据
        /// </summary>
        protected override void SeedTestData()
        {
            // 1. 创建学生组
            var studentGroup1 = new StudentGroup
            {
                Id = 1,
                Name = "初级开发组",
                Description = "适合1-2年工作经验的开发人员"
            };

            var studentGroup2 = new StudentGroup
            {
                Id = 2,
                Name = "中级开发组",
                Description = "适合3-5年工作经验的开发人员"
            };

            DbContext.StudentGroups.AddRange(studentGroup1, studentGroup2);
            DbContext.SaveChanges();

            // 2. 创建学生并关联组
            var student = new Student
            {
                Id = -1,
                UserId = -1, // 测试用户ID
                Name = "Admin（测试用户）",
                StudentNumber = "TEST001",
                PhoneNumber = "13800000000",
                AdmissionTicket = "000001",
                IdNo = "4202132322",
                Gender = Gender.Unknown,
                IsActive = true
            };

            DbContext.Students.Add(student);
            DbContext.SaveChanges();

            // 3. 创建学生与组的映射关系
            var studentGroupMappings = new List<StudentGroupMapping>
            {
                new StudentGroupMapping
                {
                    Id = 1,
                    StudentId = -1,
                    StudentGroupId = 1
                },
                new StudentGroupMapping
                {
                    Id = 2,
                    StudentId = -1,
                    StudentGroupId = 2
                }
            };

            DbContext.StudentGroupMappings.AddRange(studentGroupMappings);
            DbContext.SaveChanges();

            // 4. 创建试卷
            var examPaper1 = new ExamPaper
            {
                Id = 1,
                Name = "初级开发工程师认证考试",
                TotalScore = 100,
                PassScore = 60,
                Status = ExamPaperStatus.Published
            };

            var examPaper2 = new ExamPaper
            {
                Id = 2,
                Name = "中级开发工程师认证考试",
                TotalScore = 100,
                PassScore = 70,
                Status = ExamPaperStatus.Published
            };

            DbContext.ExamPapers.AddRange(examPaper1, examPaper2);
            DbContext.SaveChanges();

            // 5. 创建考试设置
            var now = DateTime.Now;
            var examSetting1 = new ExamSetting
            {
                Id = 1,
                Name = "初级开发组2023年度认证考试",
                Description = "面向初级开发组的年度认证考试",
                ExamPaperId = 1,
                StartTime = now.AddDays(-1), // 已开始
                EndTime = now.AddDays(5), // 未结束
                Duration = 90,
                AllowedAttempts = 2,
                EnableRandomQuestionOrder = true,
                EnableRandomOptionOrder = true,
                StudentGroups = new List<ExamSettingStudentGroup> 
                { 
                    new ExamSettingStudentGroup 
                    { 
                        Id = 1,
                        StudentGroupId = 1
                    }
                }
            };

            var examSetting2 = new ExamSetting
            {
                Id = 2,
                Name = "中级开发组2023年度认证考试",
                Description = "面向中级开发组的年度认证考试",
                ExamPaperId = 2,
                StartTime = now.AddDays(-2), // 已开始
                EndTime = now.AddDays(3), // 未结束
                Duration = 120,
                AllowedAttempts = 2,
                EnableRandomQuestionOrder = true,
                EnableRandomOptionOrder = true,
                StudentGroups = new List<ExamSettingStudentGroup> 
                { 
                    new ExamSettingStudentGroup 
                    { 
                        Id = 2,
                        StudentGroupId = 2
                    }
                }
            };

            var examSetting3 = new ExamSetting
            {
                Id = 3,
                Name = "通用基础知识测试",
                Description = "所有学生组都可以参加的基础测试",
                ExamPaperId = 1,
                StartTime = now.AddDays(-3), // 已开始
                EndTime = now.AddDays(10), // 未结束
                Duration = 60,
                AllowedAttempts = 3,
                EnableRandomQuestionOrder = false,
                EnableRandomOptionOrder = false,
                StudentGroups = new List<ExamSettingStudentGroup>() // 空列表表示所有学生组都可以参加
            };

            DbContext.ExamSettings.AddRange(examSetting1, examSetting2, examSetting3);
            DbContext.SaveChanges();

            // 6. 创建已完成的考试记录
            var examRecord = new ExamRecord
            {
                Id = 1,
                ExamSettingId = 3, // 对应通用基础知识测试
                StudentId = -1, // 测试用户
                AttemptNumber = 1,
                StartTime = now.AddDays(-2),
                Status = ExamRecordStatus.InProgress, // 修改为进行中状态
                Score = 0, // 进行中的考试没有分数
                IsPassed = false, // 进行中的考试未通过
                Duration = 60
            };

            DbContext.ExamRecords.Add(examRecord);
            DbContext.SaveChanges();
        }

        [Fact]
        public async Task GetAvailableExamsAsync_ForUser_Minus1_ReturnsCorrectExams()
        {
            // 安排测试数据已在SeedTestData中完成

            // 执行
            var result = await _clientService.GetAvailableExamsAsync(-1);

            // 断言
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // 应返回3个可参加的考试（2个特定学生组的+1个通用的）
            
            // 验证ID
            var examIds = result.Select(e => e.Id).OrderBy(id => id).ToArray();
            Assert.Equal(new long[] { 1, 2, 3 }, examIds);
            
            // 验证通用考试的HasResult状态（已参加并完成）
            var generalExam = result.FirstOrDefault(e => e.Id == 3);
            Assert.NotNull(generalExam);
            Assert.True(generalExam.HasResult);
            
            // 验证其他考试的HasResult状态（未参加）
            var otherExams = result.Where(e => e.Id != 3).ToList();
            Assert.All(otherExams, exam => Assert.False(exam.HasResult));
            
            // 验证所有考试的状态
            Assert.All(result, exam => Assert.Equal("进行中", exam.Status));
        }
    }
} 