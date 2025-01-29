using Newtonsoft.Json;

namespace CodeSpirit.IdentityApi.Amis.App
{
    /// <summary>
    /// </summary>
    public class App
    {
        /// <summary>
        /// 页面列表
        /// </summary>
        [JsonProperty("pages")]
        public List<PageGroup> Pages { get; set; }
    }
}
