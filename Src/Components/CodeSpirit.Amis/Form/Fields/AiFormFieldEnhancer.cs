using CodeSpirit.Amis.Helpers;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Helpers;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AI表单字段增强器
    /// </summary>
    public class AiFormFieldEnhancer
    {
        private readonly UtilityHelper _utilityHelper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="utilityHelper">实用工具类</param>
        public AiFormFieldEnhancer(UtilityHelper utilityHelper)
        {
            _utilityHelper = utilityHelper;
        }

        /// <summary>
        /// 增强字段配置，自动添加AI填充功能
        /// </summary>
        /// <param name="field">字段配置</param>
        /// <param name="member">成员信息</param>
        /// <param name="dtoType">DTO类型</param>
        /// <returns>增强后的字段配置</returns>
        public JObject EnhanceField(JObject field, ICustomAttributeProvider member, Type dtoType)
        {
            if (field == null || dtoType == null) return field;

            // 检查DTO是否标记了AI填充特性
            var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
            if (aiFormFillAttr == null) return field;

            // 如果是全局模式，不在字段上添加AI按钮
            if (aiFormFillAttr.IsGlobalMode) return field;

            var fieldName = field["name"]?.ToString();
            if (string.IsNullOrEmpty(fieldName) || !fieldName.Equals(aiFormFillAttr.TriggerField, StringComparison.CurrentCultureIgnoreCase))
                return field;

            // 只对文本输入字段添加AI功能
            var fieldType = field["type"]?.ToString();
            if (fieldType != "input-text") return field;

            // 检查是否已经配置了addOn，避免覆盖现有配置
            if (field["addOn"] != null) return field;

            // 自动添加AI填充按钮
            var apiEndpoint = GetApiEndpoint(dtoType, aiFormFillAttr);

            field["addOn"] = CreateAiFillButton(apiEndpoint, "AI正在生成中...");

            return field;
        }

        /// <summary>
        /// 创建全局AI填充组件（用于表单顶部）
        /// </summary>
        /// <param name="dtoType">DTO类型</param>
        /// <returns>全局AI填充组件配置，如果不支持则返回null</returns>
        public JObject CreateGlobalAiFillComponent(Type dtoType)
        {
            Console.WriteLine($"[AI组件调试] CreateGlobalAiFillComponent - DTO类型: {dtoType?.Name ?? "NULL"}");

            if (dtoType == null)
            {
                Console.WriteLine("[AI组件调试] DTO类型为NULL，返回null");
                return null;
            }

            // 检查DTO是否标记了AI填充特性并且为全局模式
            var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
            Console.WriteLine($"[AI组件调试] AI填充特性: {(aiFormFillAttr != null ? "找到" : "未找到")}");

            if (aiFormFillAttr != null)
            {
                Console.WriteLine($"[AI组件调试] TriggerField: '{aiFormFillAttr.TriggerField}', IsGlobalMode: {aiFormFillAttr.IsGlobalMode}");
                Console.WriteLine($"[AI组件调试] GlobalFillPrompt: '{aiFormFillAttr.GlobalFillPrompt}'");
            }

            if (aiFormFillAttr == null || !aiFormFillAttr.IsGlobalMode)
            {
                Console.WriteLine("[AI组件调试] 不满足全局模式条件，返回null");
                return null;
            }

            // 创建独立的全局AI填充控件
            var apiEndpoint = GetApiEndpoint(dtoType, aiFormFillAttr);
            Console.WriteLine($"[AI组件调试] 生成API端点: {apiEndpoint}");

            var component = new JObject
            {
                ["type"] = "container",
                //["className"] = "mb-4",
                ["style"] = new JObject
                {
                    ["background"] = "#ffffff",
                    ["borderRadius"] = "8px",
                    ["border"] = "1px solid #e8f2ff",
                    ["padding"] = "16px",
                    ["boxShadow"] = "0 2px 8px rgba(0, 0, 0, 0.06)"
                },
                ["body"] = new JArray
                {
                    // 输入框和按钮区域 - 保证高度一致
                    new JObject
                    {
                        ["type"] = "input-group",
                        ["className"] = "ai-fill-group",
                        ["body"] = new JArray
                        {
                            new JObject
                            {
                                ["type"] = "input-text",
                                ["name"] = "_aiCustomPrompt",
                                ["placeholder"] = aiFormFillAttr.GlobalFillPrompt ?? "请输入您的需求描述...",
                                ["clearable"] = true,
                                ["label"] = false
                            },
                            new JObject
                            {
                                ["type"] = "button",
                                ["label"] = "",
                                ["level"] = "info",
                                ["icon"] = "fa fa-magic",
                                ["tooltip"] = "点击生成AI内容",
                                ["actionType"] = "ajax",
                                ["loadingText"] = "AI生成中...",
                                ["api"] = new JObject
                                {
                                    ["method"] = "post",
                                    ["url"] = apiEndpoint,
                                    ["data"] = new JObject
                                    {
                                        ["customPrompt"] = "${_aiCustomPrompt}",
                                        ["&"] = "$$"
                                    },
                                    ["responseData"] = new JObject
                                    {
                                        ["&"] = "$$"
                                    }
                                }
                            }
                        }
                    },
                    
                    // 简洁的提示信息
                    new JObject
                    {
                        ["type"] = "html",
                        ["html"] = "<div class=\"mt-2\">" +
                                  "<small style=\"color: #8392a5; font-size: 12px; line-height: 1.4;\">" +
                                  "<i class=\"fa fa-info-circle mr-1\" style=\"color: #5b73e8;\"></i>" +
                                  "描述您的需求，AI将智能生成表单内容" +
                                  "</small>" +
                                  "</div>"
                    }
                }
            };

            return component;
        }

        /// <summary>
        /// 创建AI填充按钮的通用方法
        /// </summary>
        /// <param name="apiEndpoint">API端点</param>
        /// <param name="loadingText">加载文本</param>
        /// <returns>AI填充按钮配置</returns>
        private JObject CreateAiFillButton(string apiEndpoint, string loadingText)
        {
            return new JObject
            {
                ["type"] = "button",
                ["label"] = " ",
                ["icon"] = "fa fa-magic", // 魔法棒图标
                ["level"] = "info",
                ["actionType"] = "ajax",
                ["loadingText"] = loadingText,
                ["api"] = new JObject
                {
                    ["method"] = "post",
                    ["url"] = apiEndpoint,
                    ["data"] = new JObject
                    {
                        ["&"] = "$$" // 传递整个表单数据
                    },
                    ["responseData"] = new JObject
                    {
                        ["&"] = "$$" // 将API返回的数据合并到表单中
                    }
                }
            };
        }

        /// <summary>
        /// 获取AI填充API端点路径
        /// </summary>
        /// <param name="dtoType">DTO类型</param>
        /// <param name="aiFormFillAttr">AI填充特性</param>
        /// <returns>API端点路径</returns>
        private string GetApiEndpoint(Type dtoType, AiFormFillAttribute aiFormFillAttr)
        {
            // 使用共享的路由辅助类生成前端调用路由
            // 前端路由包含服务前缀，用于网关或反向代理的路由转发
            return AiFormFillRouteHelper.GenerateFrontendRoute(dtoType, aiFormFillAttr);
        }
    }


}
