using Newtonsoft.Json;

namespace CodeSpirit.Amis.App
{
    /// <summary>
    /// 表示 Schema 主体的对象。
    /// </summary>
    public class Body
    {
        /// <summary>
        /// 主体类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 资源源地址（例如 iframe 的 src）
        /// </summary>
        [JsonProperty("src")]
        public string Src { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        [JsonProperty("height")]
        public string Height { get; set; }
    }
}
