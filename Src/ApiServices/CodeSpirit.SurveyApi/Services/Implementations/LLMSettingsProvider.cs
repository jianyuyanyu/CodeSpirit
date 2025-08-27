using CodeSpirit.LLM.Settings;
using CodeSpirit.Settings.Services.Interfaces;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// LLM设置提供者实现
/// </summary>
/// <remarks>
/// 该类将 ISettingsService 适配为 CodeSpirit.LLM.Settings.ISettingsProvider
/// </remarks>
public class LLMSettingsProvider : ISettingsProvider
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<LLMSettingsProvider> _logger;
    private const string ModuleName = "LLM";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="settingsService">设置服务</param>
    /// <param name="logger">日志记录器</param>
    public LLMSettingsProvider(ISettingsService settingsService, ILogger<LLMSettingsProvider> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取设置
    /// </summary>
    /// <typeparam name="T">设置类型</typeparam>
    /// <param name="settingsKey">设置键</param>
    /// <returns>设置对象</returns>
    public async Task<T?> GetSettingsAsync<T>(string settingsKey) where T : class, new()
    {
        try
        {
            _logger.LogDebug("获取LLM设置: {SettingsKey}", settingsKey);
            
            // 使用全局设置获取LLM配置
            var result = await _settingsService.GetGlobalSettingAsync<T>(ModuleName, settingsKey);
            
            if (result == null)
            {
                _logger.LogWarning("未找到LLM设置: {SettingsKey}，返回默认值", settingsKey);
                result = new T();
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM设置时出错: {SettingsKey}", settingsKey);
            return new T(); // 返回默认值
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    /// <typeparam name="T">设置类型</typeparam>
    /// <param name="settingsKey">设置键</param>
    /// <param name="settings">设置对象</param>
    /// <returns>是否成功</returns>
    public async Task<bool> SaveSettingsAsync<T>(string settingsKey, T settings) where T : class, new()
    {
        try
        {
            _logger.LogDebug("保存LLM设置: {SettingsKey}", settingsKey);
            
            var result = await _settingsService.SetGlobalSettingAsync(ModuleName, settingsKey, settings, "LLM设置更新");
            
            if (result)
            {
                _logger.LogInformation("成功保存LLM设置: {SettingsKey}", settingsKey);
            }
            else
            {
                _logger.LogWarning("保存LLM设置失败: {SettingsKey}", settingsKey);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存LLM设置时出错: {SettingsKey}", settingsKey);
            return false;
        }
    }
}
