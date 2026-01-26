namespace CodeSpirit.Shared.Data;

/// <summary>
/// 可初始化的数据库上下文接口
/// </summary>
/// <remarks>
/// 实现此接口的 DbContext 可以在应用启动时自动初始化种子数据
/// </remarks>
public interface IInitializableDbContext
{
    /// <summary>
    /// 初始化数据库（应用种子数据等）
    /// </summary>
    /// <returns>异步任务</returns>
    Task InitializeDatabaseAsync();
}
