namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义属性，用于配置 AMIS 的 InputImage 字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisInputImageFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 图片上传的目标 URL。
        /// </summary>
        public string Receiver { get; set; }

        /// <summary>
        /// 接受的文件类型（例如 "image/png,image/jpeg"）。
        /// </summary>
        public string Accept { get; set; } = "image/*";

        /// <summary>
        /// 最大文件大小（字节）。
        /// </summary>
        public long MaxSize { get; set; }

        /// <summary>
        /// 是否允许上传多张图片。
        /// </summary>
        public bool Multiple { get; set; } = false;

        /// <summary>
        /// 初始化 AmisInputImageFieldAttribute 实例。
        /// </summary>
        public AmisInputImageFieldAttribute()
        {
            Type = "input-image"; // 确保类型为 "input-image"
        }
    }
}
