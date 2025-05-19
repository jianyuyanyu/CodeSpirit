namespace CodeSpirit.Charts.Core.Abstractions;

/// <summary>
/// 图表渲染器接口，定义了图表渲染的抽象能力
/// </summary>
public interface IChartRenderer
{
    /// <summary>
    /// 获取渲染器支持的图表提供者名称
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// 生成图表渲染所需的配置
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="options">渲染选项</param>
    /// <returns>渲染配置</returns>
    Task<object> GenerateRenderConfigAsync(object chartConfig, object? options = null);
    
    /// <summary>
    /// 生成图表的 Amis 配置
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="options">Amis 配置选项</param>
    /// <returns>Amis 配置对象</returns>
    Task<object> GenerateAmisConfigAsync(object chartConfig, object? options = null);
    
    /// <summary>
    /// 应用主题到图表配置
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="theme">主题名称或配置</param>
    /// <returns>应用主题后的图表配置</returns>
    Task<object> ApplyThemeAsync(object chartConfig, object theme);
    
    /// <summary>
    /// 生成图表的预览图
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="options">预览选项</param>
    /// <returns>预览图数据</returns>
    Task<byte[]> GeneratePreviewImageAsync(object chartConfig, object? options = null);
    
    /// <summary>
    /// 获取图表的响应式配置
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="containerSize">容器尺寸</param>
    /// <returns>响应式配置</returns>
    Task<object> GetResponsiveConfigAsync(object chartConfig, (int Width, int Height) containerSize);
}