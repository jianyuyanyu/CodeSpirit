using CodeSpirit.Amis.Attributes.Columns;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Amis.Column
{
    /// <summary>
    /// 长文本字段列处理器，专门处理描述、备注、内容等长文本字段的显示优化
    /// </summary>
    public class LongTextColumnHandler
    {
        /// <summary>
        /// 判断属性是否为长文本字段
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>如果是长文本字段则返回true，否则返回false</returns>
        public bool IsLongTextField(PropertyInfo prop)
        {
            string propName = prop.Name.ToLowerInvariant();
            
            return propName.Contains("description") || 
                   propName.Contains("content") ||
                   propName.Contains("note") || 
                   propName.Contains("remark") || 
                   propName.Contains("comment") ||
                   propName.Contains("summary") ||
                   propName.Contains("detail") ||
                   propName.Contains("text") ||
                   propName.Contains("message") ||
                   propName.Contains("reason") ||
                   propName.Contains("explanation") ||
                   propName.Contains("instruction") ||
                   propName.Contains("feedback") ||
                   propName.Contains("review");
        }

        /// <summary>
        /// 应用长文本列优化配置
        /// </summary>
        /// <param name="column">列对象</param>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        public void ApplyLongTextColumnOptimization(JObject column, PropertyInfo prop, string fieldName)
        {
            // 检查是否有长文本列特性
            var longTextAttr = prop.GetCustomAttribute<LongTextColumnAttribute>();
            
            // 获取MaxLength特性以确定截断长度
            var maxLengthAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
            int maxDisplayLength = longTextAttr?.CustomDisplayLength ?? GetOptimalDisplayLength(prop, maxLengthAttr);
            
            // 设置列宽度以优化显示
            column["width"] = longTextAttr?.CustomWidth ?? GetOptimalColumnWidth(prop);
            
            // 确定触发方式
            bool enableClickPopOver = longTextAttr?.EnableClickPopOver ?? false;
            string trigger = enableClickPopOver ? "click" : "hover";
            
            // 设置列类型和模板
            if (ShouldUseTruncateTemplate(maxLengthAttr, maxDisplayLength))
            {
                column["type"] = "tpl";
                column["tpl"] = $"${{{fieldName} | truncate:{maxDisplayLength}}}";
            }
            else
            {
                column["type"] = "text";
            }
            
            // 添加弹窗配置
            if (enableClickPopOver)
            {
                // 使用tooltip方案（表格列中点击更可靠）
                column["tooltip"] = CreateTooltipConfig(prop, fieldName);
            }
            else
            {
                column["popOver"] = CreateSimplePopOverConfig(prop, fieldName, "hover");
            }
            
            // 设置基本配置
            column["breakWord"] = true;
            column["quickEdit"] = false;
            column["align"] = "left";
        }

        /// <summary>
        /// 创建简化的弹窗配置（使用TPL组件）
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="trigger">触发方式：hover(悬停) 或 click(点击)</param>
        /// <returns>弹窗配置对象</returns>
        public JObject CreateSimplePopOverConfig(PropertyInfo prop, string fieldName, string trigger = "hover")
        {
            var config = new JObject
            {
                ["trigger"] = trigger,
                ["body"] = new JObject
                {
                    ["type"] = "tpl",
                    ["tpl"] = CreatePopOverBody(prop, fieldName)
                },
                ["showArrow"] = true,
                ["placement"] = "top"
            };

            // 如果是点击触发，添加showIcon
            if (trigger == "click")
            {
                config["showIcon"] = true;
            }

            return config;
        }


        /// <summary>
        /// 创建tooltip配置（表格列中点击触发的替代方案）
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>tooltip配置对象</returns>
        public JObject CreateTooltipConfig(PropertyInfo prop, string fieldName)
        {
            return new JObject
            {
                ["content"] = $"<div style='max-width: 400px; max-height: 200px; overflow-y: auto; white-space: pre-wrap; word-break: break-word; line-height: 1.4; padding: 8px;'><strong>{GetPopOverTitle(prop)}:</strong><br/><br/>${{{fieldName}}}</div>",
                ["trigger"] = "click",
                ["placement"] = "top",
                ["showArrow"] = true,
                ["enterable"] = true,  // 允许鼠标进入tooltip
                ["hideDelay"] = 300    // 延迟隐藏
            };
        }

        /// <summary>
        /// 创建查看详情按钮配置（操作列方案）
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>按钮配置对象</returns>
        public JObject CreateViewDetailButton(PropertyInfo prop, string fieldName)
        {
            return new JObject
            {
                ["type"] = "button",
                ["label"] = "查看",
                ["icon"] = "fa fa-eye",
                ["size"] = "xs",
                ["level"] = "link",
                ["actionType"] = "dialog",
                ["dialog"] = new JObject
                {
                    ["title"] = GetPopOverTitle(prop),
                    ["size"] = "md",
                    ["body"] = new JObject
                    {
                        ["type"] = "tpl",
                        ["tpl"] = $"<div style='white-space: pre-wrap; word-break: break-word; line-height: 1.6; padding: 16px;'>${{{fieldName}}}</div>"
                    },
                    ["actions"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = "关闭",
                            ["actionType"] = "close"
                        }
                    }
                }
            };
        }


        /// <summary>
        /// 创建弹窗配置（复杂版本，使用TPL组件）
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>弹窗配置对象</returns>
        public JObject CreatePopOverConfig(PropertyInfo prop, string fieldName)
        {
            return new JObject
            {
                ["title"] = GetPopOverTitle(prop),
                ["body"] = new JObject
                {
                    ["type"] = "tpl",
                    ["tpl"] = CreatePopOverHtml(fieldName)
                },
                ["trigger"] = "hover",
                ["showArrow"] = true,
                ["offset"] = new JObject
                {
                    ["x"] = 0,
                    ["y"] = 10
                }
            };
        }

        /// <summary>
        /// 创建简化的弹窗内容（用于text列自带的popOver）
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>弹窗内容字符串</returns>
        private string CreatePopOverBody(PropertyInfo prop, string fieldName)
        {
            var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            string title = displayNameAttr?.DisplayName ?? GetPopOverTitle(prop);

            // 使用简洁的格式，支持换行和长文本显示
            return $"<div style='max-width: 400px; max-height: 300px; overflow-y: auto; white-space: pre-wrap; word-break: break-word; line-height: 1.4;'>" +
                   $"<strong>{title}:</strong><br/>" +
                   $"${{{fieldName}}}" +
                   "</div>";
        }

        /// <summary>
        /// 创建弹窗HTML内容
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <returns>HTML内容字符串</returns>
        private string CreatePopOverHtml(string fieldName)
        {
            return $@"<div style='max-height: 300px; overflow-y: auto; white-space: pre-wrap; word-break: break-word; padding: 8px; line-height: 1.5;'>
                <div class='text-content'>${{{fieldName} | raw}}</div>
            </div>";
        }

        /// <summary>
        /// 获取最优显示长度
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <param name="maxLengthAttr">最大长度特性</param>
        /// <returns>最优显示长度</returns>
        public int GetOptimalDisplayLength(PropertyInfo prop, MaxLengthAttribute maxLengthAttr)
        {
            string propName = prop.Name.ToLowerInvariant();
            
            // 根据字段类型设置不同的显示长度
            if (propName.Contains("summary") || propName.Contains("title"))
            {
                return 80; // 摘要和标题可以稍长
            }
            else if (propName.Contains("description") || propName.Contains("content"))
            {
                return 60; // 描述和内容中等长度
            }
            else if (propName.Contains("note") || propName.Contains("remark") || propName.Contains("comment"))
            {
                return 50; // 备注和评论较短
            }
            else if (propName.Contains("reason") || propName.Contains("message"))
            {
                return 40; // 原因和消息更短
            }
            
            // 如果有MaxLength特性，根据其值智能调整
            if (maxLengthAttr != null)
            {
                if (maxLengthAttr.Length <= 100)
                    return Math.Min(maxLengthAttr.Length, 50);
                else if (maxLengthAttr.Length <= 500)
                    return 60;
                else
                    return 80;
            }
            
            return 50; // 默认长度
        }

        /// <summary>
        /// 获取最优列宽度
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>列宽度</returns>
        public int GetOptimalColumnWidth(PropertyInfo prop)
        {
            string propName = prop.Name.ToLowerInvariant();
            
            // 根据字段类型设置不同的列宽
            if (propName.Contains("description") || propName.Contains("content"))
            {
                return 300; // 描述和内容需要更宽的列
            }
            else if (propName.Contains("summary") || propName.Contains("detail"))
            {
                return 250; // 摘要和详情中等宽度
            }
            else if (propName.Contains("note") || propName.Contains("remark") || propName.Contains("comment"))
            {
                return 200; // 备注和评论较窄
            }
            
            return 200; // 默认宽度
        }

        /// <summary>
        /// 判断是否应该使用截断模板
        /// </summary>
        /// <param name="maxLengthAttr">最大长度特性</param>
        /// <param name="displayLength">显示长度</param>
        /// <returns>是否使用截断模板</returns>
        public bool ShouldUseTruncateTemplate(MaxLengthAttribute maxLengthAttr, int displayLength)
        {
            // 如果没有MaxLength特性，默认使用截断
            if (maxLengthAttr == null)
                return true;
                
            // 如果MaxLength大于显示长度的2倍，使用截断
            return maxLengthAttr.Length > displayLength * 2;
        }

        /// <summary>
        /// 获取弹窗标题
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>弹窗标题</returns>
        public string GetPopOverTitle(PropertyInfo prop)
        {
            var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttr != null && !string.IsNullOrEmpty(displayNameAttr.DisplayName))
            {
                return displayNameAttr.DisplayName;
            }
            
            string propName = prop.Name.ToLowerInvariant();
            
            if (propName.Contains("description"))
                return "详细描述";
            else if (propName.Contains("content"))
                return "详细内容";
            else if (propName.Contains("note"))
                return "备注信息";
            else if (propName.Contains("remark"))
                return "备注说明";
            else if (propName.Contains("comment"))
                return "评论内容";
            else if (propName.Contains("summary"))
                return "摘要信息";
            else if (propName.Contains("reason"))
                return "详细原因";
            else if (propName.Contains("message"))
                return "消息详情";
                
            return "详细信息";
        }

        /// <summary>
        /// 获取长文本字段的列类型
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>列类型</returns>
        public string GetLongTextColumnType(PropertyInfo prop)
        {
            var maxLengthAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
            if (ShouldUseTruncateTemplate(maxLengthAttr, GetOptimalDisplayLength(prop, maxLengthAttr)))
            {
                return "tpl"; // 使用模板进行截断显示
            }
            return "text";
        }
    }
}
