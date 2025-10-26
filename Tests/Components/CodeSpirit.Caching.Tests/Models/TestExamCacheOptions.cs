using CodeSpirit.Caching.Keys;
using CodeSpirit.Caching.Models;

namespace CodeSpirit.Caching.Tests.Models;

/// <summary>
/// 测试用的考试缓存选项（简化版本，避免依赖ExamApi）
/// </summary>
public static class TestExamCacheOptions
{
    /// <summary>
    /// 考试基本信息缓存键
    /// </summary>
    /// <param name="Id">考试ID</param>
    public record BasicInfo(long Id) : ICacheKey<string>
    {
        /// <summary>
        /// 缓存键
        /// </summary>
        public string Key => $"{nameof(TestExamCacheOptions)}_{nameof(BasicInfo)}_{Id}";

        /// <summary>
        /// 缓存选项
        /// </summary>
        public CacheOptions Options => new CacheOptions
        {
            L1Expiration = TimeSpan.FromMinutes(5),
            L2Expiration = TimeSpan.FromMinutes(30)
        };
    }

    /// <summary>
    /// 考试题目缓存键
    /// </summary>
    /// <param name="Id">考试ID</param>
    public record Questions(long Id) : ICacheKey<string>
    {
        /// <summary>
        /// 缓存键
        /// </summary>
        public string Key => $"{nameof(TestExamCacheOptions)}_{nameof(Questions)}_{Id}";

        /// <summary>
        /// 缓存选项
        /// </summary>
        public CacheOptions Options => new CacheOptions        {
            L1Expiration = TimeSpan.FromMinutes(10),
            L2Expiration = TimeSpan.FromHours(1)
        };
    }

    /// <summary>
    /// 用户考试记录缓存键
    /// </summary>
    /// <param name="ExamId">考试ID</param>
    /// <param name="UserId">用户ID</param>
    public record UserRecord(long ExamId, long UserId) : ICacheKey<string>
    {
        /// <summary>
        /// 缓存键
        /// </summary>
        public string Key => $"{nameof(TestExamCacheOptions)}_{nameof(UserRecord)}_{ExamId}_{UserId}";

        /// <summary>
        /// 缓存选项
        /// </summary>
        public CacheOptions Options => new CacheOptions        {
            L1Expiration = TimeSpan.FromMinutes(2),
            L2Expiration = TimeSpan.FromMinutes(15)
        };
    }

    /// <summary>
    /// 用户答案缓存键
    /// </summary>
    /// <param name="RecordId">记录ID</param>
    /// <param name="UserId">用户ID</param>
    public record UserAnswers(long RecordId, long UserId) : ICacheKey<string>
    {
        /// <summary>
        /// 缓存键
        /// </summary>
        public string Key => $"{nameof(TestExamCacheOptions)}_{nameof(UserAnswers)}_{RecordId}_{UserId}";

        /// <summary>
        /// 缓存选项
        /// </summary>
        public CacheOptions Options => new CacheOptions        {
            L1Expiration = TimeSpan.FromMinutes(1),
            L2Expiration = TimeSpan.FromMinutes(10)
        };
    }

    /// <summary>
    /// 客户端配置缓存键
    /// </summary>
    /// <param name="UserId">用户ID</param>
    public record ClientProfile(long UserId) : ICacheKey<string>
    {
        /// <summary>
        /// 缓存键
        /// </summary>
        public string Key => $"{nameof(TestExamCacheOptions)}_{nameof(ClientProfile)}_{UserId}";

        /// <summary>
        /// 缓存选项
        /// </summary>
        public CacheOptions Options => new CacheOptions        {
            L1Expiration = TimeSpan.FromMinutes(15),
            L2Expiration = TimeSpan.FromHours(2)
        };
    }
}
