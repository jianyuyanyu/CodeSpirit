namespace CodeSpirit.Amis.Tabs;

/// <summary>
/// Tabs统计生成器，用于根据Tab配置自动生成统计结果
/// </summary>
public class TabsCountGenerator
{
    /// <summary>
    /// 根据Tabs配置生成统计结果
    /// </summary>
    /// <typeparam name="TQueryDto">查询DTO类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">基础查询</param>
    /// <param name="configType">Tabs配置类型</param>
    /// <returns>统计结果字典</returns>
    public static async Task<Dictionary<string, int>> GenerateCountsAsync<TQueryDto, TEntity>(
        IQueryable<TEntity> query,
        Type configType)
        where TQueryDto : class, new()
        where TEntity : class
    {
        // 创建配置实例
        var config = (TabsConfigBase<TQueryDto>)Activator.CreateInstance(configType)!;
        var configuration = config.GetConfiguration();

        var result = new Dictionary<string, int>();

        foreach (var tabItem in configuration.TabItems)
        {
            int count;

            if (tabItem.CustomCountMethod != null)
            {
                // 使用自定义统计方法
                count = await tabItem.CustomCountMethod(query);
            }
            else if (tabItem.FilterAction != null)
            {
                // 根据Filter条件自动生成统计
                // 注意：这里使用FilterAction应用到DTO，然后需要将DTO条件转换为Entity查询
                // 这需要一个映射机制，暂时使用自定义方法
                throw new NotSupportedException(
                    $"Tab '{tabItem.Key}' 未指定自定义统计方法。请使用 WithCustomCount 方法指定统计逻辑。");
            }
            else
            {
                throw new InvalidOperationException($"Tab '{tabItem.Key}' 未配置过滤条件或统计方法");
            }

            result[tabItem.GetCountKey()] = count;
        }

        return result;
    }

    /// <summary>
    /// 根据Tabs配置生成统计结果（使用配置实例）
    /// </summary>
    /// <typeparam name="TQueryDto">查询DTO类型</typeparam>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">基础查询</param>
    /// <param name="config">Tabs配置实例</param>
    /// <returns>统计结果字典</returns>
    public static async Task<Dictionary<string, int>> GenerateCountsAsync<TQueryDto, TEntity>(
        IQueryable<TEntity> query,
        TabsConfigBase<TQueryDto> config)
        where TQueryDto : class, new()
        where TEntity : class
    {
        var configuration = config.GetConfiguration();
        var result = new Dictionary<string, int>();

        foreach (var tabItem in configuration.TabItems)
        {
            int count;

            if (tabItem.CustomCountMethod != null)
            {
                // 使用自定义统计方法
                count = await tabItem.CustomCountMethod(query);
            }
            else
            {
                throw new NotSupportedException(
                    $"Tab '{tabItem.Key}' 未指定自定义统计方法。请使用 WithCustomCount 方法指定统计逻辑。");
            }

            result[tabItem.GetCountKey()] = count;
        }

        return result;
    }
}

