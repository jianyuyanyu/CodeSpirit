using CodeSpirit.Amis.App;
using Newtonsoft.Json;

namespace CodeSpirit.Amis.Configuration
{
    /// <summary>
    /// 表示通过配置文件定义的页面。
    /// </summary>
    public class ConfigurationPage
    {
        /// <summary>
        /// 页面标签
        /// </summary>
        [JsonProperty("Label")]
        public string Label { get; set; }

        /// <summary>
        /// 页面 URL
        /// </summary>
        [JsonProperty("Url")]
        public string Url { get; set; }

        /// <summary>
        /// 重定向 URL
        /// </summary>
        [JsonProperty("Redirect")]
        public string Redirect { get; set; }

        /// <summary>
        /// 父级页面标签
        /// </summary>
        [JsonProperty("ParentLabel")]
        public string ParentLabel { get; set; }

        /// <summary>
        /// Schema API 的 URL
        /// </summary>
        [JsonProperty("SchemaApi")]
        public string SchemaApi { get; set; }

        /// <summary>
        /// Schema 内容（JSON 字符串）
        /// </summary>
        [JsonProperty("Schema")]
        public Schema Schema { get; set; }

        /// <summary>
        /// 子页面列表
        /// </summary>
        [JsonProperty("children")]
        public List<Page> Children { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }
}
