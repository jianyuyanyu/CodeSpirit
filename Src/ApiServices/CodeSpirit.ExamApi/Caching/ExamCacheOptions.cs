using CodeSpirit.Caching.Keys;
using CodeSpirit.Caching.Models;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Dtos.ExamRecord;

namespace CodeSpirit.ExamApi.Caching;

/// <summary>
/// 考试相关的强类型缓存键
/// </summary>
public static class ExamCacheOptions
{
    /// <summary>
    /// 考试基本信息缓存键
    /// </summary>
    public record BasicInfo(long Id) : ICacheKey<ExamBasicInfoCacheDto>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(BasicInfo)}_{Id}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Level = CacheLevel.L2Only  //仅使用分布式缓存，避免频繁更新时本地缓存不一致
        };
        
        public IReadOnlyList<string> Tags => [$"exam:{Id}"];
    }
    
    /// <summary>
    /// 考试题目数据缓存键
    /// </summary>
    public record Questions(long Id) : ICacheKey<Dictionary<long, ClientExamQuestionDto>>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(Questions)}_{Id}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(300),
            SlidingExpiration = TimeSpan.FromMinutes(90),
            Level = CacheLevel.L2Only, // 题目数据量大，仅使用分布式缓存
            EnableBreakthroughProtection = true // 启用缓存击穿保护
        };
        
        public IReadOnlyList<string> Tags => [$"exam:{Id}", "questions"];
    }
    
    /// <summary>
    /// 用户考试记录缓存键
    /// </summary>
    public record UserRecord(long ExamId, long UserId) : ICacheKey<UserExamRecordCacheDto>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(UserRecord)}_{ExamId}_{UserId}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60),
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Level = CacheLevel.L2Only,
            EnableBreakthroughProtection = true
        };
        
        public IReadOnlyList<string> Tags => [$"exam:{ExamId}", $"user:{UserId}"];
    }
    
    /// <summary>
    /// 用户答案缓存键
    /// </summary>
    public record UserAnswers(long RecordId, long UserId) : ICacheKey<List<ClientExamAnswerDto>>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(UserAnswers)}_{RecordId}_{UserId}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(300),
            SlidingExpiration = TimeSpan.FromMinutes(30),
            Level = CacheLevel.L2Only
        };
        
        public IReadOnlyList<string> Tags => [$"record:{RecordId}", $"user:{UserId}"];
    }
    
    /// <summary>
    /// 客户端用户档案缓存键
    /// </summary>
    /// <remarks>
    /// ✅ 性能优化说明：
    /// - 包含学生的基本信息和所属学生组ID列表
    /// - 用于减少查询学生组的数据库开销
    /// - 缓存时间较长（2小时），学生组信息变化不频繁
    /// - 使用两级缓存提高访问性能
    /// - 学生信息或学生组变更时应主动清除此缓存
    /// </remarks>
    public record ClientProfile(long UserId) : ICacheKey<ClientProfileDto>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(ClientProfile)}_{UserId}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2), // 2小时绝对过期
            SlidingExpiration = TimeSpan.FromMinutes(30), // 30分钟滑动过期
            Level = CacheLevel.Both, // 两级缓存提高性能
            EnableBreakthroughProtection = true // 防止缓存击穿
        };
        
        public IReadOnlyList<string> Tags => [$"user:{UserId}", "profile"];
    }
    
    /// <summary>
    /// 学生信息缓存键
    /// </summary>
    public record StudentInfo(long UserId) : ICacheKey<StudentDto>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(StudentInfo)}_{UserId}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
            SlidingExpiration = TimeSpan.FromMinutes(30),
            Level = CacheLevel.Both,
            EnableBreakthroughProtection = true
        };
        
        public IReadOnlyList<string> Tags => [$"user:{UserId}", "student"];
    }
    
    /// <summary>
    /// 可参加的考试列表缓存键 - 性能优化
    /// </summary>
    /// <remarks>
    /// ✅ 优化说明：
    /// - 缓存时间从60秒延长至24小时，支持提前预热
    /// - 当考试信息变更时，应主动调用清理方法清除相关缓存
    /// - 分布式缓存：降低数据库压力
    /// - 本地缓存：提高高频访问性能
    /// </remarks>
    public record AvailableExams(long StudentId) : ICacheKey<List<ClientExamDto>>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(AvailableExams)}_{StudentId}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), // ✅ 24小时缓存，支持提前预热
            SlidingExpiration = TimeSpan.FromMinutes(30), // 30分钟滑动过期，保持活跃用户缓存
            Level = CacheLevel.Both, // 两级缓存提高性能
            EnableBreakthroughProtection = true // 防止缓存击穿
        };
        
        public IReadOnlyList<string> Tags => [$"student:{StudentId}", "available-exams"];
    }
    
    ///// <summary>
    ///// 题目预览会话缓存键
    ///// </summary>
    //public record QuestionPreviewSession(string SessionId) : ICacheKey<object>
    //{
    //    public string Key => $"{nameof(ExamCacheOptions)}_{nameof(QuestionPreviewSession)}_{SessionId}";
        
    //    public CacheOptions Options => new()
    //    {
    //        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
    //        Level = CacheLevel.L2Only // 临时会话数据只存储在分布式缓存中
    //    };
        
    //    public IReadOnlyList<string> Tags => [$"session:{SessionId}", "preview"];
    //}
    
    /// <summary>
    /// 考试轻量信息缓存键（用于倒计时页面）
    /// </summary>
    /// <remarks>
    /// ✅ 性能优化说明：
    /// - 专为高频轮询的倒计时页面设计
    /// - 使用两级缓存提高响应速度
    /// - 缓存时间较短（5分钟），确保数据相对实时
    /// - 启用击穿保护防止缓存雪崩
    /// </remarks>
    public record LightInfo(long ExamId, long StudentId) : ICacheKey<ClientExamLightInfoDto>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(LightInfo)}_{ExamId}_{StudentId}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), // 5分钟绝对过期
            SlidingExpiration = TimeSpan.FromMinutes(2), // 2分钟滑动过期
            Level = CacheLevel.Both, // 两级缓存提高性能
            EnableBreakthroughProtection = true // 防止缓存击穿
        };
        
        public IReadOnlyList<string> Tags => [$"exam:{ExamId}", $"student:{StudentId}", "light"];
    }
    
    /// <summary>
    /// 考试记录缓存键（轻量级）
    /// </summary>
    /// <remarks>
    /// ✅ 性能优化说明：
    /// - 用于缓存考试记录信息，避免答题过程中频繁查询数据库
    /// - 仅缓存答题验证所需的关键字段（StudentId、Status）
    /// - 使用分布式缓存确保多实例数据一致性
    /// - 缓存时间为考试时长，确保答题过程中不需要重复查询
    /// - 启用击穿保护防止并发访问时的缓存穿透
    /// </remarks>
    public record ExamRecordCache(long RecordId) : ICacheKey<ExamRecordCacheDto>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(ExamRecordCache)}_{RecordId}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(5), // 5小时绝对过期（覆盖大部分考试场景）
            SlidingExpiration = TimeSpan.FromMinutes(30), // 30分钟滑动过期
            Level = CacheLevel.L2Only, // 仅使用分布式缓存，确保多实例一致性
            EnableBreakthroughProtection = true // 防止缓存击穿
        };
        
        public IReadOnlyList<string> Tags => [$"record:{RecordId}"];
    }
    
    /// <summary>
    /// 共享的可用考试列表缓存键（全局共享）
    /// </summary>
    /// <remarks>
    /// ✅ 性能优化说明：
    /// - 所有学生共享同一份可用考试列表缓存，大幅降低数据库压力
    /// - 包含考生组信息，支持在内存中进行个性化过滤
    /// - 缓存时间为30分钟，考试信息变更时主动清除缓存确保实时性
    /// - 使用分布式缓存确保多实例数据一致性
    /// - 启用击穿保护防止高并发时的缓存雪崩
    /// - 考试信息变更时应主动清除此缓存
    /// </remarks>
    public record SharedAvailableExams() : ICacheKey<List<SharedAvailableExamDto>>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(SharedAvailableExams)}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30), // 30分钟绝对过期
            Level = CacheLevel.L2Only, // 仅使用分布式缓存，确保多实例一致性
            EnableBreakthroughProtection = true // 防止缓存击穿
        };
        
        public IReadOnlyList<string> Tags => ["shared-available-exams"];
    }
}

