using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 图标选择器字段工厂，用于创建图标选择功能的表单字段
    /// </summary>
    public class AmisIconFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisIconFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建 AMIS 图标选择器字段配置
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）</param>
        /// <param name="utilityHelper">实用工具类</param>
        /// <returns>AMIS 字段的 JSON 对象</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisIconFieldAttribute attr) = CreateField<AmisIconFieldAttribute>(member, utilityHelper);
            if (field != null && attr != null)
            {
                // 创建图标选择器的基础配置
                field["type"] = "input-text";
                
                // 获取字段名称
                var fieldName = GetFieldName(member);
                
                // 设置字段名称以便对话框中的按钮能够定位到此字段
                field["name"] = fieldName;
                
                // 创建图标选择器的附加按钮
                var dialog = CreateIconSelectorDialog(attr, fieldName);
                dialog["data"] = new JObject
                {
                    ["fromTarget"] = fieldName
                };
                
                var addOn = new JObject
                {
                    ["type"] = "button",
                    ["icon"] = "fa fa-search",
                    ["label"] = "选择图标",
                    ["actionType"] = "dialog",
                    ["dialog"] = dialog
                };

                field["addOn"] = addOn;

                // 如果启用预览，添加图标预览显示
                if (attr.ShowPreview)
                {
                    field["addOn"]["icon"] = "${" + GetFieldName(member) + " || 'fa fa-search'}";
                }

                // 添加清除功能
                if (attr.Clearable)
                {
                    field["clearable"] = true;
                }
            }

            return field;
        }

        /// <summary>
        /// 创建图标选择器对话框配置
        /// </summary>
        /// <param name="attr">图标字段特性</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>对话框配置</returns>
        private JObject CreateIconSelectorDialog(AmisIconFieldAttribute attr, string fieldName)
        {
            var dialog = new JObject
            {
                ["title"] = "选择图标",
                ["size"] = "lg",
                ["closeOnEsc"] = true,
                ["closeOnOutside"] = true,
                ["showCloseButton"] = true,
                ["className"] = "icon-selector-dialog",
                ["style"] = new JObject
                {
                    ["width"] = "800px",
                    ["maxWidth"] = "90vw"
                },
                ["body"] = CreateIconSelectorBody(attr, fieldName)
            };

            return dialog;
        }

        /// <summary>
        /// 创建图标选择器主体内容
        /// </summary>
        /// <param name="attr">图标字段特性</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>选择器主体配置</returns>
        private JObject CreateIconSelectorBody(AmisIconFieldAttribute attr, string fieldName)
        {
            var apiUrl = !string.IsNullOrEmpty(attr.IconSource) 
                ? attr.IconSource 
                : "/api/common/icons";

            var body = new JObject
            {
                ["type"] = "service",
                ["className"] = "icon-selector-service",
                ["style"] = new JObject
                {
                    ["height"] = "500px",
                    ["display"] = "flex",
                    ["flexDirection"] = "column"
                },
                ["api"] = new JObject
                {
                    ["method"] = "get",
                    ["url"] = apiUrl,
                    ["data"] = new JObject
                    {
                        ["iconType"] = attr.IconType,
                        ["search"] = "${search}"
                    }
                },
                ["body"] = new JArray
                {
                    // 搜索框 - 固定在顶部
                    new JObject
                    {
                        ["type"] = "wrapper",
                        ["className"] = "search-header",
                        ["style"] = new JObject
                        {
                            ["flexShrink"] = "0",
                            ["borderBottom"] = "1px solid #e6e6e6",
                            ["backgroundColor"] = "white"
                        },
                        ["body"] = CreateSearchField(attr)
                    },
                    // 图标网格 - 可滚动区域
                    new JObject
                    {
                        ["type"] = "wrapper",
                        ["className"] = "icons-content",
                        ["style"] = new JObject
                        {
                            ["flex"] = "1",
                            ["overflow"] = "hidden"
                        },
                        ["body"] = CreateIconGrid(attr, fieldName)
                    }
                }
            };

            return body;
        }

        /// <summary>
        /// 创建搜索框配置
        /// </summary>
        /// <param name="attr">图标字段特性</param>
        /// <returns>搜索框配置</returns>
        private JObject CreateSearchField(AmisIconFieldAttribute attr)
        {
            var searchField = new JObject
            {
                ["type"] = "input-text",
                ["name"] = "search",
                ["placeholder"] = "搜索图标...",
                ["clearable"] = true,
                ["className"] = "mb-4",
                ["style"] = new JObject
                {
                    ["margin"] = "0 16px 16px 16px"
                },
                ["addOn"] = new JObject
                {
                    ["type"] = "button",
                    ["icon"] = "fa fa-search",
                    ["level"] = "light"
                }
            };

            if (attr.Searchable)
            {
                searchField["submitOnChange"] = true;
            }

            return searchField;
        }

        /// <summary>
        /// 创建图标网格配置
        /// </summary>
        /// <param name="attr">图标字段特性</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>图标网格配置</returns>
        private JObject CreateIconGrid(AmisIconFieldAttribute attr, string fieldName)
        {
            var previewSize = GetPreviewSize(attr.PreviewSize);
            
            var iconGrid = new JObject
            {
                ["type"] = "wrapper",
                ["className"] = "icon-selector-container",
                ["style"] = new JObject
                {
                    ["maxHeight"] = "400px",
                    ["overflowY"] = "auto",
                    ["padding"] = "16px",
                    ["backgroundColor"] = "#fafafa"
                },
                ["body"] = new JObject
                {
                    ["type"] = "each",
                    ["name"] = "items",
                    ["className"] = "icon-grid-wrapper",
                    ["style"] = new JObject
                    {
                        ["display"] = "grid",
                        ["gridTemplateColumns"] = "repeat(auto-fill, minmax(120px, 1fr))",
                        ["gap"] = "12px",
                        ["justifyContent"] = "center"
                    },
                    ["items"] = new JObject
                    {
                        ["type"] = "container",
                        ["className"] = "icon-item cursor-pointer text-center border border-gray-200 rounded-lg hover:border-blue-400 hover:shadow-md transition-all duration-200 bg-white",
                        ["style"] = new JObject
                        {
                            ["padding"] = "16px 8px",
                            ["minHeight"] = "80px",
                            ["display"] = "flex",
                            ["flexDirection"] = "column",
                            ["alignItems"] = "center",
                            ["justifyContent"] = "center"
                        },
                        ["onEvent"] = new JObject
                        {
                            ["click"] = new JObject
                            {
                                ["actions"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["actionType"] = "setValue",
                                        ["componentName"] = fieldName,
                                        ["args"] = new JObject
                                        {
                                            ["value"] = "${className}"
                                        }
                                    },
                                    new JObject
                                    {
                                        ["actionType"] = "closeDialog"
                                    }
                                }
                            }
                        },
                        ["body"] = new JArray
                        {
                            new JObject
                            {
                                ["type"] = "html",
                                ["html"] = "<i class=\"${className}\" style=\"font-size: " + previewSize + "px; color: #666; display: block; text-align: center; margin-bottom: 8px;\"></i>",
                                ["className"] = "icon-preview"
                            },
                            new JObject
                            {
                                ["type"] = "tpl",
                                ["tpl"] = "${name || className}",
                                ["className"] = "icon-name",
                                ["style"] = new JObject
                                {
                                    ["fontSize"] = "11px",
                                    ["color"] = "#888",
                                    ["lineHeight"] = "1.2",
                                    ["wordBreak"] = "break-all",
                                    ["textAlign"] = "center"
                                }
                            }
                        }
                    }
                }
            };

            return iconGrid;
        }

        /// <summary>
        /// 获取预览尺寸的像素值
        /// </summary>
        /// <param name="size">尺寸标识</param>
        /// <returns>像素值</returns>
        private int GetPreviewSize(string size)
        {
            return size?.ToLower() switch
            {
                "sm" => 16,
                "md" => 24,
                "lg" => 32,
                _ => 24
            };
        }

        /// <summary>
        /// 获取字段名称
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <returns>字段名称</returns>
        private string GetFieldName(ICustomAttributeProvider member)
        {
            return member switch
            {
                PropertyInfo property => property.Name,
                ParameterInfo parameter => parameter.Name,
                _ => "icon"
            };
        }
    }
}
