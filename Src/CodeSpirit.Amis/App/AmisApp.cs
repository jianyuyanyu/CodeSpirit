using Newtonsoft.Json;

namespace CodeSpirit.Amis.App
{
    /// <summary>
    /// </summary>
    public class AmisApp
    {
        /// <summary>
        /// 页面列表
        /// </summary>
        [JsonProperty("pages")]
        public List<PageGroup> Pages { get; set; }
    }
}
