using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeSpirit.ExamApi.Data.Seeds;

/// <summary>
/// 考试系统数据库种子
/// </summary>
public static class ExamDbContextSeed
{
    /// <summary>
    /// 初始化数据库
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="isDevelopment">是否是开发环境</param>
    public static async Task SeedAsync(ExamDbContext context, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.UserId = -1;

        // 确保数据库已创建
        await context.Database.MigrateAsync();
        
        if (isDevelopment)
        {
            // 初始化基础数据
            await SeedQuestionCategoriesAsync(context);
            await SeedStudentGroupsAsync(context);
            await SeedQuestionsAsync(context);
            await SeedStudentsAsync(context);
            await SeedExamPapersAsync(context);
            await SeedExamSettingsAsync(context);
            await SeedWrongQuestionsAsync(context);
            await SeedPracticeSettingsAsync(context);
            //await SeedExamRecordsAsync(context);
            //await SeedPracticeRecordsAsync(context);

            // 为ID为-1的用户初始化考试数据
            //await SeedTestUserDataAsync(context);
        }

        // 保存所有更改
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化问题分类
    /// </summary>
    private static async Task SeedQuestionCategoriesAsync(ExamDbContext context)
    {
        if (await context.QuestionCategories.AnyAsync())
        {
            return;
        }

        var categories = new List<QuestionCategory>
        {
            new QuestionCategory { Name = "编程语言", Description = "包括C#、Java、Python等编程语言相关题目" },
            new QuestionCategory { Name = "数据库", Description = "SQL、关系型数据库和NoSQL数据库相关题目" },
            new QuestionCategory { Name = "Web开发", Description = "HTML、CSS、JavaScript及相关框架的题目" },
            new QuestionCategory { Name = "算法与数据结构", Description = "各类算法和数据结构相关题目" },
            new QuestionCategory { Name = "系统设计", Description = "架构设计、设计模式相关题目" },
            new QuestionCategory { Name = "DevOps", Description = "CI/CD、容器化、云计算相关题目" },
            new QuestionCategory { Name = "信息安全", Description = "网络安全、加密、安全协议相关题目" },
            new QuestionCategory { Name = "操作系统", Description = "Windows、Linux等操作系统相关题目" },
            new QuestionCategory { Name = "人工智能", Description = "机器学习、深度学习相关题目" },
            new QuestionCategory { Name = "移动开发", Description = "Android、iOS等移动平台开发相关题目" }
        };

        // 使用DbContext的AddRangeAsync而不是DbSet的AddRangeAsync
        await context.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化学生分组
    /// </summary>
    private static async Task SeedStudentGroupsAsync(ExamDbContext context)
    {
        if (await context.StudentGroups.AnyAsync())
        {
            return;
        }

        var groups = new List<StudentGroup>
        {
            new()
            {
                Name = "初级开发组",
                Description = "适合1-2年工作经验的开发人员"
            },
            new()
            {
                Name = "中级开发组",
                Description = "适合3-5年工作经验的开发人员"
            },
            new()
            {
                Name = "高级开发组",
                Description = "适合5年以上工作经验的开发人员"
            }
        };

        // 直接使用DbContext的AddRangeAsync，它会自动设置ID
        await context.AddRangeAsync(groups);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化题目数据
    /// </summary>
    private static async Task SeedQuestionsAsync(ExamDbContext context)
    {
        if (await context.Questions.AnyAsync())
        {
            return;
        }

        // 获取问题分类
        var categories = await context.QuestionCategories.ToListAsync();
        if (!categories.Any())
        {
            return;
        }

        var categoryDict = categories.ToDictionary(c => c.Name, c => c);

        var questions = new List<Question>
        {
            // 编程基础题目
            new()
            {
                Category = categoryDict["编程语言"],
                Type = QuestionType.SingleChoice,
                Difficulty = QuestionDifficulty.Easy,
                Content = "以下关于值类型和引用类型的说法，哪个是正确的？",
                Options = new List<string>
                {
                    "值类型直接存储在栈上，引用类型存储在堆上",
                    "值类型和引用类型都存储在堆上",
                    "值类型和引用类型都存储在栈上",
                    "引用类型直接存储在栈上，值类型存储在堆上"
                },
                CorrectAnswer = "0",
                Analysis = "值类型（如int、struct等）直接存储在栈上，而引用类型（如class、interface等）的引用存储在栈上，实际对象存储在堆上。",
                KnowledgePoints = JsonSerializer.Serialize(new[] { "值类型", "引用类型", "内存管理" }),
                Tags = JsonSerializer.Serialize(new[] { "C#", "内存", "基础概念" }),
                CorrectRate = 0
            },
            
            // 数据结构题目
            new()
            {
                Category = categoryDict["算法与数据结构"],
                Type = QuestionType.SingleChoice,
                Difficulty = QuestionDifficulty.Medium,
                Content = "在一个包含n个元素的平衡二叉搜索树中，查找一个元素的时间复杂度是多少？",
                Options = new List<string>
                {
                    "O(1)",
                    "O(log n)",
                    "O(n)",
                    "O(n log n)"
                },
                CorrectAnswer = "1",
                Analysis = "平衡二叉搜索树的高度是log n，每次查找都会减半搜索范围，因此时间复杂度是O(log n)。",
                KnowledgePoints = JsonSerializer.Serialize(new[] { "二叉搜索树", "时间复杂度", "树的遍历" }),
                Tags = JsonSerializer.Serialize(new[] { "数据结构", "算法复杂度", "树" }),
                CorrectRate = 0
            },

            // 算法题目
            new()
            {
                Category = categoryDict["算法与数据结构"],
                Type = QuestionType.MultipleChoice,
                Difficulty = QuestionDifficulty.Hard,
                Content = "以下哪些排序算法的平均时间复杂度是O(n log n)？",
                Options = new List<string>
                {
                    "快速排序",
                    "冒泡排序",
                    "归并排序",
                    "堆排序"
                },
                CorrectAnswer = "0,2,3",
                Analysis = "快速排序、归并排序和堆排序的平均时间复杂度都是O(n log n)，而冒泡排序的时间复杂度是O(n²)。",
                KnowledgePoints = JsonSerializer.Serialize(new[] { "排序算法", "时间复杂度", "算法分析" }),
                Tags = JsonSerializer.Serialize(new[] { "算法", "排序", "复杂度分析" }),
                CorrectRate = 0
            },

            // 系统设计题目
            new()
            {
                Category = categoryDict["系统设计"],
                Type = QuestionType.TrueFalse,
                Difficulty = QuestionDifficulty.Hard,
                Content = "请设计一个高并发的缓存系统，要求：\n1. 支持LRU淘汰策略\n2. 支持过期时间设置\n3. 支持并发访问\n请给出关键代码的实现。",
                Options = new List<string>(),
                CorrectAnswer = "true",
                Analysis = "这个实现使用了以下关键技术点：\n1. 使用ConcurrentDictionary保证基本的线程安全\n2. 使用ReaderWriterLockSlim实现细粒度的锁控制\n3. 使用LinkedList实现LRU功能\n4. 使用CacheItem封装值和过期时间\n5. 实现了Set和TryGet方法，保证了线程安全和功能完整性",
                KnowledgePoints = JsonSerializer.Serialize(new[] { "并发编程", "缓存设计", "数据结构", "线程安全" }),
                Tags = JsonSerializer.Serialize(new[] { "系统设计", "高并发", "缓存" }),
                CorrectRate = 0
            },

            // 数据库题目
            new()
            {
                Category = categoryDict["数据库"],
                Type = QuestionType.SingleChoice,
                Difficulty = QuestionDifficulty.Medium,
                Content = "在MySQL中，以下哪种索引类型最适合用于范围查询？",
                Options = new List<string>
                {
                    "Hash索引",
                    "B+树索引",
                    "位图索引",
                    "全文索引"
                },
                CorrectAnswer = "1",
                Analysis = "B+树索引是最适合范围查询的索引类型，因为：\n1. B+树的所有叶子节点都在同一层，并且通过链表相连\n2. 这种结构使得范围查询可以简单地遍历相邻叶子节点\n3. Hash索引不支持范围查询，位图索引主要用于等值查询，全文索引用于文本搜索",
                KnowledgePoints = JsonSerializer.Serialize(new[] { "数据库索引", "B+树", "查询优化" }),
                Tags = JsonSerializer.Serialize(new[] { "MySQL", "索引", "性能优化" }),
                CorrectRate = 0
            }
        };

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(questions);

        // 为每个题目创建初始版本
        var questionVersions = questions.Select(q => new QuestionVersion
        {
            Question = q,
            Version = 1,
            Content = q.Content,
            Options = q.Options,
            CorrectAnswer = q.CorrectAnswer,
            Analysis = q.Analysis,
            KnowledgePoints = q.KnowledgePoints,
            Tags = q.Tags
        }).ToList();

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(questionVersions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化考生数据
    /// </summary>
    private static async Task SeedStudentsAsync(ExamDbContext context)
    {
        IIdGenerator idGenerator = new SnowflakeIdGenerator();
        if (await context.Students.AnyAsync())
        {
            return;
        }

        var groups = await context.StudentGroups.ToListAsync();
        if (!groups.Any())
        {
            return;
        }

        var students = new List<Student>
        {
            new()
            {
                UserId = idGenerator.NewId(),
                Name = "张三",
                PhoneNumber = "13800138001",
                StudentNumber = "STU001",
                AdmissionTicket = "20210001",
                Gender = Gender.Male,
                IdNo = "110101199001011234",
                IsActive = true
            },
            new()
            {
                Id = idGenerator.NewId(),
                UserId = idGenerator.NewId(),
                Name = "李四",
                PhoneNumber = "13800138002",
                StudentNumber = "STU002",
                AdmissionTicket = "20210002",
                Gender = Gender.Male,
                IdNo = "110101199001011235",
                IsActive = true
            },
            new()
            {
                Id = idGenerator.NewId(),
                UserId = idGenerator.NewId(),
                Name = "王五",
                PhoneNumber = "13800138003",
                StudentNumber = "STU003",
                IdNo = "110101199001011236",
                AdmissionTicket = "20210003",
                Gender = Gender.Male,
                IsActive = true
            }
        };

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(students);
        await context.SaveChangesAsync();

        // 添加学生分组映射
        var studentGroupMappings = new List<StudentGroupMapping>();
        foreach (var student in students)
        {
            var group = student.Name switch
            {
                "张三" => groups.First(g => g.Name == "初级开发组"),
                "李四" => groups.First(g => g.Name == "中级开发组"),
                "王五" => groups.First(g => g.Name == "高级开发组"),
                _ => groups.First()
            };

            studentGroupMappings.Add(new StudentGroupMapping
            {
                Student = student,
                StudentGroup = group
            });
        }

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(studentGroupMappings);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化试卷数据
    /// </summary>
    private static async Task SeedExamPapersAsync(ExamDbContext context)
    {
        if (await context.ExamPapers.AnyAsync())
        {
            return;
        }

        var questions = await context.Questions.Include(q => q.Category).ToListAsync();
        if (!questions.Any())
        {
            return;
        }

        var examPapers = new List<ExamPaper>
        {
            new()
            {
                Name = "初级开发工程师认证考试",
                Description = "适用于1-2年工作经验的开发人员",
                TotalScore = 100,
                PassScore = 60,
                Duration = 90,
                Status = ExamPaperStatus.Published,
                AverageScore = 0,
                PassRate = 0
            },
            new()
            {
                Name = "中级开发工程师认证考试",
                Description = "适用于3-5年工作经验的开发人员",
                TotalScore = 100,
                PassScore = 70,
                Duration = 120,
                Status = ExamPaperStatus.Published,
                AverageScore = 0,
                PassRate = 0
            },
            new()
            {
                Name = "高级开发工程师认证考试",
                Description = "适用于5年以上工作经验的开发人员",
                TotalScore = 100,
                PassScore = 80,
                Duration = 150,
                Status = ExamPaperStatus.Published,
                AverageScore = 0,
                PassRate = 0
            }
        };

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(examPapers);
        await context.SaveChangesAsync();

        // 为每份试卷添加题目
        var examPaperQuestions = new List<ExamPaperQuestion>();
        foreach (var paper in examPapers)
        {
            var difficulty = paper.Name.Contains("初级") ? QuestionDifficulty.Easy
                         : paper.Name.Contains("中级") ? QuestionDifficulty.Medium
                         : QuestionDifficulty.Hard;

            var paperQuestions = questions
                .Where(q => q.Difficulty == difficulty)
                .OrderBy(q => Guid.NewGuid())
                .Take(10)
                .Select((q, index) => new ExamPaperQuestion
                {
                    ExamPaper = paper,
                    Question = q,
                    QuestionVersion = context.QuestionVersions
                        .First(qv => qv.QuestionId == q.Id && qv.Version == 1),
                    OrderNumber = index + 1,
                    Score = 10,
                    IsRequired = true
                });

            examPaperQuestions.AddRange(paperQuestions);
        }

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(examPaperQuestions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化考试设置数据
    /// </summary>
    private static async Task SeedExamSettingsAsync(ExamDbContext context)
    {
        if (await context.ExamSettings.AnyAsync())
        {
            return;
        }

        var examPapers = await context.ExamPapers.ToListAsync();
        var studentGroups = await context.StudentGroups.ToListAsync();
        if (!examPapers.Any() || !studentGroups.Any())
        {
            return;
        }

        IIdGenerator idGenerator = new SnowflakeIdGenerator();
        var examSettings = new List<ExamSetting>();
        foreach (var paper in examPapers)
        {
            var group = paper.Name.Contains("初级") ? studentGroups.First(g => g.Name == "初级开发组")
                     : paper.Name.Contains("中级") ? studentGroups.First(g => g.Name == "中级开发组")
                     : studentGroups.First(g => g.Name == "高级开发组");

            examSettings.Add(new ExamSetting
            {
                Name = $"{group.Name}{DateTime.Now.Year}年度认证考试",
                Description = $"面向{group.Name}的年度认证考试",
                ExamPaperId = paper.Id,
                StartTime = DateTime.Now.AddDays(-7),
                EndTime = DateTime.Now.AddDays(14),
                Duration = paper.Duration,
                AllowedAttempts = 1000,
                EnableRandomQuestionOrder = true,
                EnableRandomOptionOrder = true,
                AllowedScreenSwitchCount = 0,
                Status = ExamSettingStatus.Draft,
                StudentGroups = studentGroups.Select(x => new ExamSettingStudentGroup
                {
                    Id = idGenerator.NewId(),
                    StudentGroup = x
                }).ToList()
            });
        }

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(examSettings);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化错题数据
    /// </summary>
    private static async Task SeedWrongQuestionsAsync(ExamDbContext context)
    {
        if (await context.Set<WrongQuestion>().AnyAsync())
        {
            return;
        }

        var students = await context.Students.ToListAsync();
        var questions = await context.Questions.ToListAsync();
        if (!students.Any() || !questions.Any())
        {
            return;
        }

        var wrongQuestions = new List<WrongQuestion>();
        
        // 张三的错题
        var zhangsan = students.First(s => s.Name == "张三");
        var easyQuestions = questions.Where(q => q.Difficulty == QuestionDifficulty.Easy).ToList();
        
        if (easyQuestions.Count >= 2)
        {
            wrongQuestions.Add(new WrongQuestion
            {
                StudentId = zhangsan.Id,
                QuestionId = easyQuestions[0].Id,
                WrongCount = 2,
                LastWrongAnswer = "1", // 假设错误选择了第二个选项
                LastWrongTime = DateTime.UtcNow.AddDays(-5),
                Tags = "重点复习",
                Notes = "需要复习相关概念，下次不能再错"
            });
            
            wrongQuestions.Add(new WrongQuestion
            {
                StudentId = zhangsan.Id,
                QuestionId = easyQuestions[1].Id,
                WrongCount = 1,
                LastWrongAnswer = "2", // 假设错误选择了第三个选项
                LastWrongTime = DateTime.UtcNow.AddDays(-3),
                Tags = "不确定"
            });
        }
        
        // 李四的错题
        var lisi = students.First(s => s.Name == "李四");
        var mediumQuestions = questions.Where(q => q.Difficulty == QuestionDifficulty.Medium).ToList();
        
        if (mediumQuestions.Count >= 2)
        {
            wrongQuestions.Add(new WrongQuestion
            {
                StudentId = lisi.Id,
                QuestionId = mediumQuestions[0].Id,
                WrongCount = 3,
                LastWrongAnswer = "0", // 假设错误选择了第一个选项
                LastWrongTime = DateTime.UtcNow.AddDays(-10),
                Tags = "难点",
                Notes = "这个概念比较复杂，需要多次复习"
            });
        }
        
        // 王五的错题
        var wangwu = students.First(s => s.Name == "王五");
        var hardQuestions = questions.Where(q => q.Difficulty == QuestionDifficulty.Hard).ToList();
        
        if (hardQuestions.Count >= 1)
        {
            wrongQuestions.Add(new WrongQuestion
            {
                StudentId = wangwu.Id,
                QuestionId = hardQuestions[0].Id,
                WrongCount = 1,
                LastWrongAnswer = "0,1", // 假设是多选题，错误选择了前两个选项
                LastWrongTime = DateTime.UtcNow.AddDays(-1),
                Tags = "易错点"
            });
        }

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(wrongQuestions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化练习设置数据
    /// </summary>
    private static async Task SeedPracticeSettingsAsync(ExamDbContext context)
    {
        if (await context.PracticeSettings.AnyAsync())
        {
            return;
        }

        var examPapers = await context.ExamPapers.ToListAsync();
        if (!examPapers.Any())
        {
            return;
        }

        var practiceSettings = new List<PracticeSetting>
        {
            new()
            {
                Name = "C#编程基础练习",
                Description = "适合初学者的C#编程基础练习，包含基本语法和概念",
                ExamPaperId = examPapers.First(p => p.Name.Contains("初级")).Id,
                PracticeMode = PracticeMode.Sequential,
                MaxAttempts = 0, // 无限制
                TimeLimit = 0, // 无时间限制
                ShowAnalysis = true,
                RandomizeQuestions = false,
                Status = PracticeSettingStatus.Published
            },
            new()
            {
                Name = "数据结构随机练习",
                Description = "数据结构相关题目的随机练习，难度适中",
                ExamPaperId = examPapers.First(p => p.Name.Contains("中级")).Id,
                PracticeMode = PracticeMode.Random,
                MaxAttempts = 5,
                TimeLimit = 60,
                ShowAnalysis = true,
                RandomizeQuestions = true,
                Status = PracticeSettingStatus.Published
            },
            new()
            {
                Name = "高级算法模拟考试",
                Description = "高级算法知识模拟考试，限时完成",
                ExamPaperId = examPapers.First(p => p.Name.Contains("高级")).Id,
                PracticeMode = PracticeMode.MockExam,
                MaxAttempts = 3,
                TimeLimit = 120,
                ShowAnalysis = false,
                RandomizeQuestions = true,
                Status = PracticeSettingStatus.Draft
            },
            new()
            {
                Name = "错题重练集",
                Description = "收集学生错题进行重点练习",
                ExamPaperId = examPapers.First().Id,
                PracticeMode = PracticeMode.WrongQuestions,
                MaxAttempts = 0,
                TimeLimit = 0,
                ShowAnalysis = true,
                RandomizeQuestions = false,
                Status = PracticeSettingStatus.Published
            },
            new()
            {
                Name = "自由练习模式",
                Description = "不限时间和次数的自由练习",
                ExamPaperId = examPapers.Last().Id,
                PracticeMode = PracticeMode.Free,
                MaxAttempts = 0,
                TimeLimit = 0,
                ShowAnalysis = true,
                RandomizeQuestions = false,
                Status = PracticeSettingStatus.Published
            }
        };

        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(practiceSettings);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 初始化考试记录数据
    /// </summary>
    private static async Task SeedExamRecordsAsync(ExamDbContext context)
    {
        if (await context.ExamRecords.AnyAsync())
        {
            return;
        }

        var examSettings = await context.ExamSettings
            .Include(e => e.ExamPaper)
            .ThenInclude(p => p.ExamPaperQuestions)
            .ThenInclude(q => q.QuestionVersion)
            .ToListAsync();
            
        var students = await context.Students.ToListAsync();
        
        if (!examSettings.Any() || !students.Any())
        {
            return;
        }

        var examRecords = new List<ExamRecord>();
        var answerRecords = new List<ExamAnswerRecord>();
        
        // 为每个学生创建一条考试记录
        foreach (var student in students)
        {
            // 根据学生名称选择合适的考试设置
            var examSetting = student.Name switch
            {
                "张三" => examSettings.FirstOrDefault(e => e.Name.Contains("初级")),
                "李四" => examSettings.FirstOrDefault(e => e.Name.Contains("中级")),
                "王五" => examSettings.FirstOrDefault(e => e.Name.Contains("高级")),
                _ => examSettings.FirstOrDefault()
            };

            if (examSetting == null)
            {
                continue;
            }

            var startTime = DateTime.UtcNow.AddDays(-2);
            var submitTime = startTime.AddMinutes(examSetting.Duration * 0.8); // 假设用了80%的时间完成考试
            
            // 确定考试状态和成绩
            var status = ExamRecordStatus.Graded;
            var paperQuestions = examSetting.ExamPaper.ExamPaperQuestions.ToList();
            double totalScore = 0;
            double correctRate = student.Name switch
            {
                "张三" => 0.7, // 70%正确率
                "李四" => 0.8, // 80%正确率
                "王五" => 0.9, // 90%正确率
                _ => 0.75
            };
            
            var record = new ExamRecord
            {
                ExamSettingId = examSetting.Id,
                StudentId = student.Id,
                AttemptNumber = 1,
                StartTime = startTime,
                SubmitTime = submitTime,
                Status = status,
                IpAddress = "127.0.0.1",
                DeviceInfo = "{\"os\":\"Windows 10\",\"device\":\"Desktop\",\"screen\":\"1920x1080\"}",
                BrowserInfo = "Chrome 91.0.4472.124",
                ScreenSwitchCount = 0,
                Duration = (int)(submitTime - startTime).TotalMinutes,
                GradedTime = submitTime.AddHours(2),
                GraderId = "system"
            };
            
            examRecords.Add(record);
            
            // 创建答题记录
            foreach (var paperQuestion in paperQuestions)
            {
                var isCorrect = Random.Shared.NextDouble() < correctRate;
                var questionScore = isCorrect ? paperQuestion.Score : 0;
                totalScore += questionScore;
                
                // 构造答案
                string answer;
                if (isCorrect)
                {
                    // 获取正确答案，但检查长度避免数据库截断
                    answer = paperQuestion.QuestionVersion.CorrectAnswer;
                    // 对于系统设计等复杂题目，答案可能很长，需要截断处理
                    if (answer.Length > 1000)
                    {
                        answer = answer.Substring(0, 1000) + "...（答案过长，已截断）";
                    }
                }
                else
                {
                    // 构造错误答案（根据题型简单处理）
                    if (paperQuestion.Question.Type == QuestionType.SingleChoice)
                    {
                        // 单选题：随机选一个不同于正确答案的选项
                        var correctAnswer = paperQuestion.QuestionVersion.CorrectAnswer;
                        // 检查是否是复杂题目（例如代码示例的正确答案）
                        if (correctAnswer.Length > 100)
                        {
                            // 对于复杂题目，简单返回一个固定错误答案
                            answer = "错误答案示例";
                        }
                        else
                        {
                            var correctOption = int.Parse(correctAnswer);
                            var wrongOptions = Enumerable.Range(0, 4) // 假设有4个选项
                                .Where(i => i != correctOption)
                                .ToArray();
                                
                            if (wrongOptions.Length > 0)
                            {
                                answer = wrongOptions[Random.Shared.Next(wrongOptions.Length)].ToString();
                            }
                            else
                            {
                                answer = "0"; // 默认选第一个
                            }
                        }
                    }
                    else if (paperQuestion.Question.Type == QuestionType.MultipleChoice)
                    {
                        // 多选题：随机选择几个选项，确保与正确答案不同
                        var correctAnswer = paperQuestion.QuestionVersion.CorrectAnswer;
                        // 检查是否是复杂题目
                        if (correctAnswer.Length > 100)
                        {
                            // 对于复杂题目，简单返回一个固定错误答案
                            answer = "0,1"; // 简单返回前两个选项
                        }
                        else
                        {
                            var correctAnswers = correctAnswer.Split(',')
                                .Select(int.Parse).ToList();
                            var allOptions = Enumerable.Range(0, 4).ToList(); // 假设有4个选项
                            var wrongAnswers = new List<int>();
                            
                            // 随机选择1-3个选项
                            var selectCount = Random.Shared.Next(1, 4);
                            while (wrongAnswers.Count < selectCount)
                            {
                                var option = Random.Shared.Next(4);
                                if (!wrongAnswers.Contains(option) && 
                                    (wrongAnswers.Count > 0 || !correctAnswers.Contains(option)))
                                {
                                    wrongAnswers.Add(option);
                                }
                            }
                            
                            answer = string.Join(",", wrongAnswers.OrderBy(x => x));
                        }
                    }
                    else if (paperQuestion.Question.Type == QuestionType.TrueFalse)
                    {
                        // 判断题：选择与正确答案相反的选项
                        answer = paperQuestion.QuestionVersion.CorrectAnswer == "0" ? "1" : "0";
                    }
                    else
                    {
                        // 主观题：随机生成简短文本
                        answer = "这是一个示例答案，与正确答案不同。";
                    }
                }
                
                // 最终检查答案长度，确保不超过数据库字段限制（假设为2000字符）
                if (answer.Length > 1800)
                {
                    answer = answer.Substring(0, 1800) + "...（答案过长，已截断）";
                }
                
                var answerRecord = new ExamAnswerRecord
                {
                    ExamRecordId = 0, // 稍后设置
                    QuestionId = paperQuestion.QuestionId,
                    QuestionVersionId = paperQuestion.QuestionVersionId,
                    OrderNumber = paperQuestion.OrderNumber,
                    Answer = answer,
                    IsMarked = Random.Shared.Next(10) < 2, // 20%的概率被标记
                    IsCorrect = isCorrect,
                    Score = questionScore,
                    StartTime = startTime.AddMinutes(Random.Shared.Next((int)(examSetting.Duration * 0.8))),
                    SubmitTime = submitTime.AddMinutes(-Random.Shared.Next(10)),
                    Duration = Random.Shared.Next(60, 300), // 1-5分钟随机答题时间
                    GradedTime = submitTime.AddHours(2),
                    GraderId = "system"
                };
                
                answerRecords.Add(answerRecord);
            }
            
            // 设置考试总分和通过状态
            record.Score = totalScore;
            record.IsPassed = totalScore >= examSetting.ExamPaper.PassScore;
            
            // 如果不及格，添加评语
            if (!record.IsPassed)
            {
                record.Comments = "需要加强基础知识学习，重点关注错题";
            }
        }
        
        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(examRecords);
        await context.SaveChangesAsync();
        
        // 更新答题记录中的考试记录ID并保存
        foreach (var record in examRecords)
        {
            // 获取该考试记录对应的答题记录
            var recordAnswers = new List<ExamAnswerRecord>();
            foreach (var paperQuestion in record.ExamSetting.ExamPaper.ExamPaperQuestions.OrderBy(q => q.OrderNumber))
            {
                // 找到与当前题目匹配的答题记录（尚未分配考试记录ID的）
                var answer = answerRecords.FirstOrDefault(a => 
                    a.ExamRecordId == 0 && 
                    a.QuestionId == paperQuestion.QuestionId && 
                    a.OrderNumber == paperQuestion.OrderNumber);
                    
                if (answer != null)
                {
                    answer.ExamRecordId = record.Id;
                    recordAnswers.Add(answer);
                    // 从未分配列表中移除，避免重复分配
                    answerRecords.Remove(answer);
                }
            }
            
            // 为每个考试记录批量添加对应的答题记录
            if (recordAnswers.Any())
            {
                // 使用DbContext的AddRangeAsync方法
                await context.AddRangeAsync(recordAnswers);
                await context.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// 初始化练习记录数据
    /// </summary>
    private static async Task SeedPracticeRecordsAsync(ExamDbContext context)
    {
        if (await context.PracticeRecords.AnyAsync())
        {
            return;
        }

        var students = await context.Students.ToListAsync();
        var questions = await context.Questions.Include(q => q.Category).ToListAsync();
        if (!students.Any() || !questions.Any())
        {
            return;
        }

        var practiceRecords = new List<PracticeRecord>();
        var random = new Random();
        
        // 为每个学生创建练习记录
        foreach (var student in students)
        {
            // 为学生随机选择5-10个题目进行练习
            var studentQuestions = questions
                .OrderBy(q => random.Next())
                .Take(random.Next(5, 11))
                .ToList();
                
            foreach (var question in studentQuestions)
            {
                // 决定是自由练习还是模拟考试
                var practiceType = random.Next(3) < 2 ? PracticeType.Free : PracticeType.MockExam;
                
                // 随机生成练习时间(过去30天内)
                var practiceTime = DateTime.UtcNow.AddDays(-random.Next(1, 31))
                    .AddHours(-random.Next(0, 24))
                    .AddMinutes(-random.Next(0, 60));
                    
                // 随机决定是否答对
                var isCorrect = random.Next(2) == 1;
                
                // 构造答案
                string answer;
                if (isCorrect)
                {
                    // 正确答案
                    answer = question.CorrectAnswer;
                }
                else
                {
                    // 生成错误答案
                    if (question.Type == QuestionType.SingleChoice)
                    {
                        // 单选题
                        var correctOption = int.Parse(question.CorrectAnswer);
                        var options = new List<int> { 0, 1, 2, 3 };
                        options.Remove(correctOption);
                        answer = options[random.Next(options.Count)].ToString();
                    }
                    else if (question.Type == QuestionType.MultipleChoice)
                    {
                        // 多选题
                        var correctOptions = question.CorrectAnswer.Split(',').Select(int.Parse).ToList();
                        var allOptions = new List<int> { 0, 1, 2, 3 };
                        var wrongOptions = allOptions.Except(correctOptions).ToList();
                        
                        if (random.Next(2) == 0 && correctOptions.Count > 1)
                        {
                            // 少选一个选项
                            var reducedCorrectOptions = correctOptions.ToList();
                            reducedCorrectOptions.RemoveAt(random.Next(reducedCorrectOptions.Count));
                            answer = string.Join(",", reducedCorrectOptions.OrderBy(o => o));
                        }
                        else
                        {
                            // 多选一个错误选项
                            var extendedCorrectOptions = correctOptions.ToList();
                            extendedCorrectOptions.Add(wrongOptions[random.Next(wrongOptions.Count)]);
                            answer = string.Join(",", extendedCorrectOptions.OrderBy(o => o));
                        }
                    }
                    else
                    {
                        // 判断题
                        answer = question.CorrectAnswer == "1" ? "0" : "1";
                    }
                }
                
                // 随机生成练习耗时(10秒到5分钟)
                var timeSpent = random.Next(10, 301);
                
                var practiceRecord = new PracticeRecord
                {
                    StudentId = student.Id,
                    QuestionId = question.Id,
                    PracticeType = practiceType,
                    Answer = answer,
                    IsCorrect = isCorrect,
                    TimeSpent = timeSpent,
                    PracticeTime = practiceTime,
                    MockExamId = practiceType == PracticeType.MockExam ? (long?)1 : null
                };
                
                practiceRecords.Add(practiceRecord);
            }
        }
        
        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(practiceRecords);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 为测试用户（ID为-1）初始化考试数据
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="idGenerator">ID生成器</param>
    private static async Task SeedTestUserDataAsync(ExamDbContext context)
    {
        const long userId = -1;
        
        // 1. 检查是否已有数据
        var hasGroupMapping = await context.StudentGroupMappings
            .AnyAsync(m => m.StudentId == userId);
            
        if (hasGroupMapping)
        {
            return; // 用户组映射已存在，无需重复初始化
        }
        
        // 1.1 检查并创建ID为-1的学生记录
        var testStudent = await context.Students.FindAsync(userId);
        if (testStudent == null)
        {
            testStudent = new Student
            {
                Id = userId,
                UserId = userId, // 设置UserId
                Name = "Admin（测试用户）",
                StudentNumber = "TEST001",
                PhoneNumber = "13800000000",
                AdmissionTicket = "000001",
                IdNo = "4202132322",
                Gender = Gender.Unknown,
                IsActive = true,
                CreatedBy = -1,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };
            
            context.Students.Add(testStudent);
            await context.SaveChangesAsync();            
        }
        
        // 2. 获取所有学生组
        var studentGroups = await context.StudentGroups.ToListAsync();
        if (!studentGroups.Any())
        {
            return; // 没有可用的学生组，无法初始化用户数据
        }
        
        // 3. 为用户ID为-1添加所有学生组映射，使其可以参加所有考试
        var groupMappings = new List<StudentGroupMapping>();
        foreach (var group in studentGroups)
        {
            groupMappings.Add(new StudentGroupMapping
            {
                StudentId = testStudent.Id,
                StudentGroupId = group.Id
            });
        }
        
        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(groupMappings);
        await context.SaveChangesAsync();
        
        // 4. 获取所有考试设置
        var examSettings = await context.ExamSettings
            .Include(e => e.ExamPaper)
            .ThenInclude(p => p.ExamPaperQuestions)
            .ThenInclude(q => q.QuestionVersion)
            .ToListAsync();
            
        if (!examSettings.Any())
        {
            return; // 没有可用的考试设置，无法初始化历史考试记录
        }
        
        // 5. 创建考试记录（包括进行中、已提交和已评分的状态）
        var examRecords = new List<ExamRecord>();
        var random = new Random();
        
        // 5.1 创建一条进行中的考试记录
        var inProgressExam = examSettings.FirstOrDefault();
        if (inProgressExam != null)
        {
            var inProgressRecord = new ExamRecord
            {
                ExamSettingId = inProgressExam.Id,
                StudentId = testStudent.Id, // 使用实际的学生ID
                AttemptNumber = 1,
                StartTime = DateTime.Now.AddMinutes(-15),
                Status = ExamRecordStatus.InProgress,
                IpAddress = "127.0.0.1",
                DeviceInfo = "Mozilla/5.0 (Demo Client)",
                ScreenSwitchCount = 0
            };
            
            examRecords.Add(inProgressRecord);
        }
        
        // 5.2 创建已提交但未评分的考试记录
        var submittedExam = examSettings.Skip(1).FirstOrDefault();
        if (submittedExam != null)
        {
            var startTime = DateTime.Now.AddDays(-1);
            var endTime = startTime.AddMinutes(submittedExam.Duration / 2); // 用一半的时间完成考试
            
            var submittedRecord = new ExamRecord
            {
                ExamSettingId = submittedExam.Id,
                StudentId = testStudent.Id, // 使用实际的学生ID
                AttemptNumber = 1,
                StartTime = startTime,
                SubmitTime = endTime,
                Duration = (int)Math.Ceiling((endTime - startTime).TotalMinutes),
                Status = ExamRecordStatus.Submitted,
                IpAddress = "127.0.0.1",
                DeviceInfo = "Mozilla/5.0 (Demo Client)",
                ScreenSwitchCount = 0
            };
            
            examRecords.Add(submittedRecord);
        }
        
        // 5.3 创建3条已评分的考试记录（通过和不通过的情况）
        for (int i = 0; i < Math.Min(3, examSettings.Count); i++)
        {
            var examSetting = examSettings[i % examSettings.Count];
            var startTime = DateTime.Now.AddDays(-5 - i * 2); // 间隔几天
            var endTime = startTime.AddMinutes(examSetting.Duration - random.Next(10, 30)); // 提前完成
            
            // 随机决定是否通过考试
            var isPassed = random.Next(2) == 1;
            var score = isPassed ? 
                examSetting.ExamPaper.PassScore + random.Next(1, 21) : // 通过：及格分 + 1-20分
                Math.Max(0, examSetting.ExamPaper.PassScore - random.Next(1, 16)); // 未通过：及格分 - 1-15分
            
            var gradedRecord = new ExamRecord
            {
                ExamSettingId = examSetting.Id,
                StudentId = testStudent.Id, // 使用实际的学生ID
                AttemptNumber = 1,
                StartTime = startTime,
                SubmitTime = endTime,
                Duration = (int)Math.Ceiling((endTime - startTime).TotalMinutes),
                Status = ExamRecordStatus.Graded,
                Score = score,
                IsPassed = isPassed,
                IpAddress = "127.0.0.1",
                DeviceInfo = "Mozilla/5.0 (Demo Client)",
                ScreenSwitchCount = 0,
                GradedTime = endTime.AddHours(2), // 2小时后评分
                Comments = isPassed ? "考试通过，继续保持" : "考试未通过，请继续努力"
            };
            
            examRecords.Add(gradedRecord);
        }
        
        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(examRecords);
        await context.SaveChangesAsync();
        
        // 6. 为已评分和已提交的考试记录添加答题记录
        var answerRecords = new List<ExamAnswerRecord>();
        
        foreach (var record in examRecords.Where(r => r.Status != ExamRecordStatus.InProgress))
        {
            // 重新加载考试设置和试卷信息
            await context.Entry(record)
                .Reference(r => r.ExamSetting)
                .LoadAsync();
                
            await context.Entry(record.ExamSetting)
                .Reference(s => s.ExamPaper)
                .LoadAsync();
                
            await context.Entry(record.ExamSetting.ExamPaper)
                .Collection(p => p.ExamPaperQuestions)
                .LoadAsync();
            
            foreach (var paperQuestion in record.ExamSetting.ExamPaper.ExamPaperQuestions)
            {
                // 加载题目版本
                await context.Entry(paperQuestion)
                    .Reference(q => q.QuestionVersion)
                    .LoadAsync();
                    
                await context.Entry(paperQuestion)
                    .Reference(q => q.Question)
                    .LoadAsync();
                
                // 决定答案是否正确（已评分的根据得分情况，已提交的随机）
                var isCorrect = false;
                if (record.Status == ExamRecordStatus.Graded)
                {
                    // 根据总分决定每道题的正确率
                    var correctProbability = record.Score / record.ExamSetting.ExamPaper.TotalScore;
                    isCorrect = random.NextDouble() < correctProbability;
                }
                else
                {
                    // 已提交未评分的随机决定
                    isCorrect = random.Next(2) == 1;
                }
                
                // 构造答案
                string answer;
                if (isCorrect)
                {
                    // 获取正确答案，但检查长度避免数据库截断
                    answer = paperQuestion.QuestionVersion.CorrectAnswer;
                    // 对于系统设计等复杂题目，答案可能很长，需要截断处理
                    if (answer.Length > 1000)
                    {
                        answer = answer.Substring(0, 1000) + "...（答案过长，已截断）";
                    }
                }
                else
                {
                    // 构造错误答案（根据题型简单处理）
                    if (paperQuestion.Question.Type == QuestionType.SingleChoice)
                    {
                        // 单选题：随机选一个不同于正确答案的选项
                        var correctAnswer = paperQuestion.QuestionVersion.CorrectAnswer;
                        // 检查是否是复杂题目（例如代码示例的正确答案）
                        if (correctAnswer.Length > 100)
                        {
                            // 对于复杂题目，简单返回一个固定错误答案
                            answer = "错误答案示例";
                        }
                        else
                        {
                            var correctOption = int.Parse(correctAnswer);
                            var wrongOptions = Enumerable.Range(0, 4) // 假设有4个选项
                                .Where(i => i != correctOption)
                                .ToArray();
                                
                            if (wrongOptions.Length > 0)
                            {
                                answer = wrongOptions[random.Next(wrongOptions.Length)].ToString();
                            }
                            else
                            {
                                answer = "0"; // 默认选第一个
                            }
                        }
                    }
                    else if (paperQuestion.Question.Type == QuestionType.MultipleChoice)
                    {
                        // 多选题：随机选择几个选项，确保与正确答案不同
                        var correctAnswer = paperQuestion.QuestionVersion.CorrectAnswer;
                        // 检查是否是复杂题目
                        if (correctAnswer.Length > 100)
                        {
                            // 对于复杂题目，简单返回一个固定错误答案
                            answer = "0,1"; // 简单返回前两个选项
                        }
                        else
                        {
                            var correctAnswers = correctAnswer.Split(',')
                                .Select(int.Parse).ToList();
                            var allOptions = Enumerable.Range(0, 4).ToList(); // 假设有4个选项
                            var wrongAnswers = new List<int>();
                            
                            // 随机选择1-3个选项
                            var selectCount = random.Next(1, 4);
                            while (wrongAnswers.Count < selectCount)
                            {
                                var option = random.Next(4);
                                if (!wrongAnswers.Contains(option) && 
                                    (wrongAnswers.Count > 0 || !correctAnswers.Contains(option)))
                                {
                                    wrongAnswers.Add(option);
                                }
                            }
                            
                            answer = string.Join(",", wrongAnswers.OrderBy(x => x));
                        }
                    }
                    else if (paperQuestion.Question.Type == QuestionType.TrueFalse)
                    {
                        // 判断题：选择与正确答案相反的选项
                        answer = paperQuestion.QuestionVersion.CorrectAnswer == "0" ? "1" : "0";
                    }
                    else
                    {
                        // 主观题：随机生成简短文本
                        answer = "这是一个示例答案，与正确答案不同。";
                    }
                }
                
                // 最终检查答案长度，确保不超过数据库字段限制（假设为2000字符）
                if (answer.Length > 1800)
                {
                    answer = answer.Substring(0, 1800) + "...（答案过长，已截断）";
                }
                
                // 创建答题记录
                var answerRecord = new ExamAnswerRecord
                {
                    ExamRecordId = record.Id,
                    QuestionId = paperQuestion.QuestionId,
                    QuestionVersionId = paperQuestion.QuestionVersionId,
                    OrderNumber = paperQuestion.OrderNumber,
                    Answer = answer,
                    IsCorrect = isCorrect
                };
                
                // 如果是已评分的，添加得分
                if (record.Status == ExamRecordStatus.Graded)
                {
                    answerRecord.Score = isCorrect ? paperQuestion.Score : 0;
                    answerRecord.GradedTime = record.GradedTime;
                }
                
                answerRecords.Add(answerRecord);
            }
        }
        
        // 使用DbContext的AddRangeAsync方法
        await context.AddRangeAsync(answerRecords);
        await context.SaveChangesAsync();
    }
}