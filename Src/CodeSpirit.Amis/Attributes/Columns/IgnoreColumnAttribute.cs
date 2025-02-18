namespace CodeSpirit.Amis.Attributes.Columns
{
    /// <summary>
    /// 特性：用于标记在生成 AMIS 列配置时应被忽略的属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class IgnoreColumnAttribute : Attribute
    {
        // 可以根据需要添加属性，如忽略的原因等
        public string Reason { get; }

        public IgnoreColumnAttribute(string reason = "")
        {
            Reason = reason;
        }
    }
}
