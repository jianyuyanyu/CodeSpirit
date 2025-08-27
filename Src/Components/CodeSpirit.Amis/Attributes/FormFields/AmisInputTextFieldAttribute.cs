using System;

namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的 input-text 类型字段，支持附加组件。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AmisInputTextFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 是否启用右侧附加组件
        /// </summary>
        public bool EnableAddOn { get; set; } = false;

        /// <summary>
        /// 附加组件的类型，支持 "text", "button", "submit" 等
        /// </summary>
        public string AddOnType { get; set; } = "button";

        /// <summary>
        /// 附加组件的标签文本
        /// </summary>
        public string AddOnLabel { get; set; }

        /// <summary>
        /// 附加组件的图标，支持FontAwesome图标类名
        /// </summary>
        public string AddOnIcon { get; set; }

        /// <summary>
        /// 附加组件的CSS类名
        /// </summary>
        public string AddOnClassName { get; set; }

        /// <summary>
        /// 附加组件的按钮级别，支持 "info", "success", "warning", "danger", "light", "dark", "link" 等
        /// </summary>
        public string AddOnLevel { get; set; } = "info";

        /// <summary>
        /// 附加组件的按钮尺寸，支持 "xs", "sm", "md", "lg" 等
        /// </summary>
        public string AddOnSize { get; set; }

        /// <summary>
        /// 附加组件点击时的动作类型，支持 "ajax", "dialog", "drawer" 等
        /// </summary>
        public string AddOnActionType { get; set; } = "ajax";

        /// <summary>
        /// 附加组件点击时请求的API地址
        /// </summary>
        public string AddOnApi { get; set; }

        /// <summary>
        /// 附加组件的确认信息，设置后点击时会弹出确认框
        /// </summary>
        public string AddOnConfirmText { get; set; }

        /// <summary>
        /// 附加组件是否禁用
        /// </summary>
        public bool AddOnDisabled { get; set; } = false;

        /// <summary>
        /// 附加组件的显示条件表达式
        /// </summary>
        public string AddOnVisibleOn { get; set; }

        /// <summary>
        /// 附加组件的禁用条件表达式
        /// </summary>
        public string AddOnDisabledOn { get; set; }

        /// <summary>
        /// 附加组件的加载状态文本
        /// </summary>
        public string AddOnLoadingText { get; set; }

        /// <summary>
        /// 初始化一个新的 <see cref="AmisInputTextFieldAttribute"/> 实例。
        /// </summary>
        public AmisInputTextFieldAttribute() : base("input-text")
        {
        }

        /// <summary>
        /// 初始化一个新的 <see cref="AmisInputTextFieldAttribute"/> 实例，并启用附加组件。
        /// </summary>
        /// <param name="addOnLabel">附加组件的标签文本</param>
        /// <param name="addOnApi">附加组件的API地址</param>
        public AmisInputTextFieldAttribute(string addOnLabel, string addOnApi) : this()
        {
            EnableAddOn = true;
            AddOnLabel = addOnLabel;
            AddOnApi = addOnApi;
        }
    }
}
