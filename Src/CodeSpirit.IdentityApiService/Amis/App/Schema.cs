using Newtonsoft.Json;

namespace CodeSpirit.IdentityApi.Amis.App
{
    /// <summary>
    /// 表示页面的 Schema 对象。
    /// </summary>
    public class Schema
    {
        /// <summary>
        /// Schema 类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Schema 的主体部分
        /// </summary>
        [JsonProperty("body")]
        public Body Body { get; set; }
    }

}
