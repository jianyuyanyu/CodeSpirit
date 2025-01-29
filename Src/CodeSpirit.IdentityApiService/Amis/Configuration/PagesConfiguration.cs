using Newtonsoft.Json;

namespace CodeSpirit.IdentityApi.Amis.Configuration
{
    /// <summary>
    /// 表示通过配置文件定义的页面集合。
    /// </summary>
    public class PagesConfiguration
    {
        /// <summary>
        /// 页面列表
        /// </summary>
        [JsonProperty("Pages")]
        public List<ConfigurationPage> Pages { get; set; }
    }
}
