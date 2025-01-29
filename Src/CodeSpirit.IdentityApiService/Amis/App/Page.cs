using Newtonsoft.Json;

namespace CodeSpirit.IdentityApi.Amis.App
{
    /// <summary>
    /// 表示单个页面的对象，可以包含子页面、Schema 或 Schema API。
    /// </summary>
    public class Page
    {
        /// <summary>
        /// 页面标签
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// 页面 URL
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// 重定向 URL
        /// </summary>
        [JsonProperty("redirect")]
        public string Redirect { get; set; }

        /// <summary>
        /// 子页面列表
        /// </summary>
        [JsonProperty("children")]
        public List<Page> Children { get; set; }

        /// <summary>
        /// 页面 Schema 对象
        /// </summary>
        [JsonProperty("schema")]
        public Schema Schema { get; set; }

        /// <summary>
        /// Schema API 的 URL
        /// </summary>
        [JsonProperty("schemaApi")]
        public string SchemaApi { get; set; }

        /// <summary>
        /// 父级页面标签（用于内部处理，不会序列化到 JSON）
        /// </summary>
        [JsonIgnore]
        public string ParentLabel { get; set; }
    }
}
