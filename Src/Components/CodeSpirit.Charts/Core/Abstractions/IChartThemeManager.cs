namespace CodeSpirit.Charts.Core.Abstractions;

/// <summary>
/// 图表主题管理器接口，定义了主题管理的抽象能力
/// </summary>
public interface IChartThemeManager
{
    /// <summary>
    /// 获取所有可用主题
    /// </summary>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>主题列表</returns>
    Task<IEnumerable<string>> GetAvailableThemesAsync(string providerName);
    
    /// <summary>
    /// 获取主题配置
    /// </summary>
    /// <param name="themeName">主题名称</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>主题配置</returns>
    Task<object> GetThemeConfigAsync(string themeName, string providerName);
    
    /// <summary>
    /// 注册新主题
    /// </summary>
    /// <param name="themeName">主题名称</param>
    /// <param name="themeConfig">主题配置</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>注册是否成功</returns>
    Task<bool> RegisterThemeAsync(string themeName, object themeConfig, string providerName);
    
    /// <summary>
    /// 更新主题配置
    /// </summary>
    /// <param name="themeName">主题名称</param>
    /// <param name="themeConfig">主题配置</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>更新是否成功</returns>
    Task<bool> UpdateThemeAsync(string themeName, object themeConfig, string providerName);
    
    /// <summary>
    /// 删除主题
    /// </summary>
    /// <param name="themeName">主题名称</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>删除是否成功</returns>
    Task<bool> DeleteThemeAsync(string themeName, string providerName);
    
    /// <summary>
    /// 应用主题到图表配置
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="themeName">主题名称</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>应用主题后的图表配置</returns>
    Task<object> ApplyThemeAsync(object chartConfig, string themeName, string providerName);
    
    /// <summary>
    /// 获取主题预览
    /// </summary>
    /// <param name="themeName">主题名称</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>主题预览数据</returns>
    Task<object> GetThemePreviewAsync(string themeName, string providerName);
    
    /// <summary>
    /// 导出主题配置
    /// </summary>
    /// <param name="themeName">主题名称</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>主题配置数据</returns>
    Task<string> ExportThemeAsync(string themeName, string providerName);
    
    /// <summary>
    /// 导入主题配置
    /// </summary>
    /// <param name="themeData">主题配置数据</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>导入是否成功</returns>
    Task<bool> ImportThemeAsync(string themeData, string providerName);
}