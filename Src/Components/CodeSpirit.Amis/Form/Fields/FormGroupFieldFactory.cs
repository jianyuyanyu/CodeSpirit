using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// 表单项组字段工厂
    /// <para>用于创建AMIS表单项组配置</para>
    /// </summary>
    public class FormGroupFieldFactory : IAmisFieldFactory
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public bool CanHandle(Type attributeType)
        {
            return attributeType == typeof(FormGroupAttribute);
        }

        /// <summary>
        /// 创建表单项组字段配置
        /// </summary>
        /// <param name="member">成员信息（参数或属性）</param>
        /// <param name="utilityHelper">实用工具类</param>
        /// <returns>表单项组字段配置</returns>
        public JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            var groupAttr = member.GetAttribute<FormGroupAttribute>();
            if (groupAttr == null)
            {
                return null;
            }

            // 创建基础组配置
            var groupConfig = new JObject
            {
                ["type"] = "group"
            };

            // 设置组标题
            if (!string.IsNullOrEmpty(groupAttr.Title))
            {
                groupConfig["label"] = groupAttr.Title;
            }

            // 设置组描述
            if (!string.IsNullOrEmpty(groupAttr.Description))
            {
                groupConfig["description"] = groupAttr.Description;
            }

            // 设置组名称
            if (!string.IsNullOrEmpty(groupAttr.Name))
            {
                groupConfig["name"] = groupAttr.Name;
            }

            // 设置显示模式
            if (groupAttr.Mode != FormGroupMode.Normal)
            {
                groupConfig["mode"] = GetModeString(groupAttr.Mode);
            }

            // 设置间距
            if (groupAttr.Gap != FormGroupGap.Normal)
            {
                groupConfig["gap"] = GetGapString(groupAttr.Gap);
            }

            // 设置方向
            if (groupAttr.Direction == FormGroupDirection.Horizontal)
            {
                groupConfig["direction"] = "horizontal";
            }

            // 设置边框
            if (groupAttr.ShowBorder)
            {
                groupConfig["showBorder"] = true;
            }

            // 设置CSS类名
            if (!string.IsNullOrEmpty(groupAttr.ClassName))
            {
                groupConfig["className"] = groupAttr.ClassName;
            }

            // 设置显示条件
            if (!string.IsNullOrEmpty(groupAttr.VisibleOn))
            {
                groupConfig["visibleOn"] = groupAttr.VisibleOn;
            }

            // 设置隐藏状态
            if (groupAttr.Hidden)
            {
                groupConfig["hidden"] = true;
            }

            // 设置禁用状态
            if (groupAttr.Disabled)
            {
                groupConfig["disabled"] = true;
            }

            // 设置禁用条件
            if (!string.IsNullOrEmpty(groupAttr.DisabledOn))
            {
                groupConfig["disabledOn"] = groupAttr.DisabledOn;
            }

            // 处理自定义配置
            if (!string.IsNullOrEmpty(groupAttr.AdditionalConfig))
            {
                try
                {
                    var additionalConfig = JObject.Parse(groupAttr.AdditionalConfig);
                    groupConfig.Merge(additionalConfig, new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Union
                    });
                }
                catch (Exception)
                {
                    // 忽略JSON解析错误
                }
            }

            // 初始化body数组，用于存放组内的字段
            groupConfig["body"] = new JArray();

            return groupConfig;
        }

        /// <summary>
        /// 获取模式字符串
        /// </summary>
        /// <param name="mode">模式枚举</param>
        /// <returns>模式字符串</returns>
        private string GetModeString(FormGroupMode mode)
        {
            return mode switch
            {
                FormGroupMode.Inline => "inline",
                FormGroupMode.Horizontal => "horizontal",
                _ => "normal"
            };
        }

        /// <summary>
        /// 获取间距字符串
        /// </summary>
        /// <param name="gap">间距枚举</param>
        /// <returns>间距字符串</returns>
        private string GetGapString(FormGroupGap gap)
        {
            return gap switch
            {
                FormGroupGap.None => "none",
                FormGroupGap.Small => "sm",
                FormGroupGap.Large => "lg",
                _ => "base"
            };
        }
    }
}
