using CodeSpirit.Caching.Keys;
using CodeSpirit.Caching.Models;
using CodeSpirit.ExamApi.Dtos.Client;

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
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Level = CacheLevel.Both
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
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Level = CacheLevel.Both
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
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120),
            SlidingExpiration = TimeSpan.FromMinutes(30),
            Level = CacheLevel.L2Only
        };
        
        public IReadOnlyList<string> Tags => [$"record:{RecordId}", $"user:{UserId}"];
    }
    
    /// <summary>
    /// 客户端用户档案缓存键
    /// </summary>
    public record ClientProfile(long UserId) : ICacheKey<ClientProfileDto>
    {
        public string Key => $"{nameof(ExamCacheOptions)}_{nameof(ClientProfile)}_{UserId}";
        
        public CacheOptions Options => new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            Level = CacheLevel.Both,
            EnableBreakthroughProtection = true
        };
        
        public IReadOnlyList<string> Tags => [$"user:{UserId}", "profile"];
    }
}

