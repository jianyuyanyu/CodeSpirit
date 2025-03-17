using CodeSpirit.ExamApi.Data.Models;
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
    public static async Task SeedAsync(ExamDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.UserId = -1;

        // 确保数据库已创建
        await context.Database.EnsureCreatedAsync();

        // 初始化基础数据
        await SeedQuestionCategoriesAsync(context);
        await SeedStudentGroupsAsync(context);
        await SeedQuestionsAsync(context);

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
            new()
            {
                Name = "编程基础",
                Description = "包含基本的编程概念和语法知识"
            },
            new()
            {
                Name = "数据结构",
                Description = "包含常见数据结构的概念和应用"
            },
            new()
            {
                Name = "算法",
                Description = "包含基础和高级算法题目"
            },
            new()
            {
                Name = "系统设计",
                Description = "包含架构设计和系统设计相关题目"
            },
            new()
            {
                Name = "数据库",
                Description = "包含数据库原理和SQL相关题目"
            }
        };

        await context.QuestionCategories.AddRangeAsync(categories);
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

        await context.StudentGroups.AddRangeAsync(groups);
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
                Category = categoryDict["编程基础"],
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
                Category = categoryDict["数据结构"],
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
                Category = categoryDict["算法"],
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
                Type = QuestionType.SingleChoice,
                Difficulty = QuestionDifficulty.Hard,
                Content = "请设计一个高并发的缓存系统，要求：\n1. 支持LRU淘汰策略\n2. 支持过期时间设置\n3. 支持并发访问\n请给出关键代码的实现。",
                Options = new List<string>(),
                CorrectAnswer = "public class ConcurrentCache<TKey, TValue>\n{\n    private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache;\n    private readonly ReaderWriterLockSlim _lock;\n    private readonly LinkedList<TKey> _lruList;\n    private readonly int _capacity;\n\n    public ConcurrentCache(int capacity)\n    {\n        _cache = new ConcurrentDictionary<TKey, CacheItem<TValue>>();\n        _lock = new ReaderWriterLockSlim();\n        _lruList = new LinkedList<TKey>();\n        _capacity = capacity;\n    }\n\n    public void Set(TKey key, TValue value, TimeSpan? expiry = null)\n    {\n        var item = new CacheItem<TValue>(value, expiry);\n        _lock.EnterWriteLock();\n        try\n        {\n            if (_cache.Count >= _capacity)\n            {\n                RemoveLeastUsed();\n            }\n            _cache[key] = item;\n            UpdateLRU(key);\n        }\n        finally\n        {\n            _lock.ExitWriteLock();\n        }\n    }\n\n    public bool TryGet(TKey key, out TValue value)\n    {\n        value = default;\n        _lock.EnterUpgradeableReadLock();\n        try\n        {\n            if (_cache.TryGetValue(key, out var item) && !item.IsExpired)\n            {\n                _lock.EnterWriteLock();\n                try\n                {\n                    UpdateLRU(key);\n                }\n                finally\n                {\n                    _lock.ExitWriteLock();\n                }\n                value = item.Value;\n                return true;\n            }\n            return false;\n        }\n        finally\n        {\n            _lock.ExitUpgradeableReadLock();\n        }\n    }\n\n    private void UpdateLRU(TKey key)\n    {\n        _lruList.Remove(key);\n        _lruList.AddFirst(key);\n    }\n\n    private void RemoveLeastUsed()\n    {\n        var key = _lruList.Last.Value;\n        _lruList.RemoveLast();\n        _cache.TryRemove(key, out _);\n    }\n}\n\nprivate class CacheItem<T>\n{\n    public T Value { get; }\n    public DateTime? ExpiryTime { get; }\n\n    public bool IsExpired => ExpiryTime.HasValue && DateTime.UtcNow >= ExpiryTime.Value;\n\n    public CacheItem(T value, TimeSpan? expiry = null)\n    {\n        Value = value;\n        ExpiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null;\n    }\n}",
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

        await context.Questions.AddRangeAsync(questions);

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

        await context.QuestionVersions.AddRangeAsync(questionVersions);
        await context.SaveChangesAsync();
    }
}