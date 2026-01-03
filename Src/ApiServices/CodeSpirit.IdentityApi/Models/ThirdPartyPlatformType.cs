using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Models;

/// <summary>
/// 第三方平台类型
/// </summary>
public enum ThirdPartyPlatformType
{
    /// <summary>
    /// 微信小程序
    /// </summary>
    [Display(Name = "微信小程序")]
    WeChatMiniProgram = 1,
    
    /// <summary>
    /// 支付宝小程序
    /// </summary>
    [Display(Name = "支付宝小程序")]
    AlipayMiniProgram = 2,
    
    /// <summary>
    /// 抖音小程序
    /// </summary>
    [Display(Name = "抖音小程序")]
    DouyinMiniProgram = 3,
    
    /// <summary>
    /// 微信开放平台（UnionId来源）
    /// </summary>
    [Display(Name = "微信开放平台")]
    WeChatOpenPlatform = 10
}

