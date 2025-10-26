using System.ComponentModel;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.Amis.Tests.Examples;

/// <summary>
/// AmisSwitchFieldAttribute 使用示例
/// </summary>
[DisplayName("开关字段示例")]
public class SwitchFieldExample
{
    /// <summary>
    /// 基本开关字段
    /// </summary>
    [DisplayName("基本开关")]
    [AmisSwitchField]
    public bool BasicSwitch { get; set; }

    /// <summary>
    /// 带默认值的开关字段
    /// </summary>
    [DisplayName("默认开启")]
    [AmisSwitchField(DefaultValue = true)]
    public bool SwitchWithDefault { get; set; }

    /// <summary>
    /// 带标签和默认值的开关字段
    /// </summary>
    [AmisSwitchField("启用功能", true)]
    public bool SwitchWithLabelAndDefault { get; set; }

    /// <summary>
    /// 自定义开关文本
    /// </summary>
    [DisplayName("自定义文本")]
    [AmisSwitchField(OnText = "开启", OffText = "关闭")]
    public bool SwitchWithCustomText { get; set; }

    /// <summary>
    /// 自定义开关值
    /// </summary>
    [DisplayName("自定义值")]
    [AmisSwitchField(TrueValue = 1, FalseValue = 0)]
    public int SwitchWithCustomValue { get; set; }

    /// <summary>
    /// 不同尺寸的开关
    /// </summary>
    [DisplayName("小尺寸开关")]
    [AmisSwitchField(Size = "sm")]
    public bool SmallSwitch { get; set; }

    /// <summary>
    /// 大尺寸开关
    /// </summary>
    [DisplayName("大尺寸开关")]
    [AmisSwitchField(Size = "lg")]
    public bool LargeSwitch { get; set; }

    /// <summary>
    /// 禁用状态的开关
    /// </summary>
    [DisplayName("禁用开关")]
    [AmisSwitchField(Disabled = true, DefaultValue = true)]
    public bool DisabledSwitch { get; set; }

    /// <summary>
    /// 静态模式的开关
    /// </summary>
    [DisplayName("静态开关")]
    [AmisSwitchField(Static = true, DefaultValue = false)]
    public bool StaticSwitch { get; set; }
} 