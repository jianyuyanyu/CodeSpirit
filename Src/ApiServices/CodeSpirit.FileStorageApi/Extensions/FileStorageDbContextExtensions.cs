namespace CodeSpirit.FileStorageApi.Extensions;

/// <summary>
/// 文件存储数据库上下文扩展方法
/// </summary>
public static class FileStorageDbContextExtensions
{
    /// <summary>
    /// 初始化数据库
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <returns>初始化任务</returns>
    public static async Task InitializeDatabaseAsync(this FileStorageDbContext context)
    {
        try
        {
            // 确保数据库已创建
            await context.Database.EnsureCreatedAsync();
            
            // 这里可以添加初始数据种子
            await SeedDataAsync(context);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("初始化文件存储数据库失败", ex);
        }
    }
    
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <returns>种子数据任务</returns>
    private static async Task SeedDataAsync(FileStorageDbContext context)
    {
        // 检查是否已有数据
        if (await context.Files.AnyAsync())
        {
            return; // 已有数据，跳过种子数据
        }
        
        // 这里可以添加一些初始的系统文件或配置
        // 目前暂时留空
        
        await context.SaveChangesAsync();
    }
}
