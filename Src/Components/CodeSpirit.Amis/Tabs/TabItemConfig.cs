using System.Linq.Expressions;
using Newtonsoft.Json;

namespace CodeSpirit.Amis.Tabs;

/// <summary>
/// Tab项配置
/// </summary>
/// <typeparam name="TQueryDto">查询DTO类型</typeparam>
public class TabItemConfig<TQueryDto> where TQueryDto : class, new()
{
    /// <summary>
    /// Tab唯一标识
    /// </summary>
    public string Key { get; internal set; } = "";

    /// <summary>
    /// Tab显示标题
    /// </summary>
    public string Title { get; internal set; } = "";

    /// <summary>
    /// Tab排序顺序
    /// </summary>
    public int Order { get; internal set; } = 0;

    /// <summary>
    /// Tab图标
    /// </summary>
    public string Icon { get; internal set; } = "";

    /// <summary>
    /// Badge样式级别
    /// </summary>
    public string BadgeLevel { get; internal set; } = "";

    /// <summary>
    /// 过滤条件表达式
    /// </summary>
    internal Action<TQueryDto>? FilterAction { get; set; }

    /// <summary>
    /// 统计条件表达式（用于后端查询）
    /// </summary>
    internal Expression<Func<TQueryDto, bool>>? CountExpression { get; set; }

    /// <summary>
    /// 自定义统计方法
    /// </summary>
    internal Func<IQueryable<object>, Task<int>>? CustomCountMethod { get; set; }

    /// <summary>
    /// 获取过滤条件的JSON字符串（用于前端）
    /// </summary>
    public string GetFilterJson()
    {
        if (FilterAction == null)
        {
            return "{}";
        }

        var dto = new TQueryDto();
        FilterAction(dto);

        // 将DTO转为字典，只保留非null的属性
        var properties = typeof(TQueryDto).GetProperties()
            .Where(p => p.CanRead)
            .Select(p => new { p.Name, Value = p.GetValue(dto) })
            .Where(x => x.Value != null)
            .ToDictionary(
                x => char.ToLowerInvariant(x.Name[0]) + x.Name.Substring(1), // 转为驼峰式
                x => x.Value
            );

        return JsonConvert.SerializeObject(properties);
    }

    /// <summary>
    /// 获取统计键名（用于返回的字典）
    /// </summary>
    public string GetCountKey()
    {
        return ConvertKeyToCamelCase(Key) + "Count";
    }

    /// <summary>
    /// 将下划线分隔的key转换为驼峰式命名
    /// </summary>
    private string ConvertKeyToCamelCase(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        if (!key.Contains('_'))
        {
            return key;
        }

        var parts = key.Split('_');
        var result = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                result += char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
        }

        return result;
    }
}

