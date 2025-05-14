namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 表单字段默认值类型
    /// </summary>
    public enum DefaultValueType
    {
        /// <summary>
        /// 静态值
        /// </summary>
        Static,

        /// <summary>
        /// 表达式（支持 ${xxx} 语法）
        /// </summary>
        Expression,

        /// <summary>
        /// 当前时间
        /// </summary>
        CurrentDateTime,

        /// <summary>
        /// 当前用户
        /// </summary>
        CurrentUser,

        /// <summary>
        /// 自定义
        /// </summary>
        Custom
    }
}