using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Settings;

/// <summary>
/// 用户偏好设置DTO
/// </summary>
public class UserPreferencesDto
{
    /// <summary>
    /// 界面语言
    /// </summary>
    [DisplayName("界面语言")]
    [AmisSelectField(Label = "界面语言", Options = "zh-CN:简体中文,en-US:English")]
    public string Language { get; set; } = "zh-CN";
    
    /// <summary>
    /// 主题模式
    /// </summary>
    [DisplayName("主题模式")]
    [AmisSelectField(Label = "主题模式", Options = "light:浅色模式,dark:深色模式,auto:跟随系统")]
    public string ThemeMode { get; set; } = "light";
    
    /// <summary>
    /// 每页显示条数
    /// </summary>
    [DisplayName("每页显示条数")]
    [AmisNumberField(Label = "每页显示条数", Min = 10, Max = 100)]
    public int PageSize { get; set; } = 20;
}

