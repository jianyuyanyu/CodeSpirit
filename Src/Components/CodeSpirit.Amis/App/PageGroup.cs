using Newtonsoft.Json;

namespace CodeSpirit.Amis.App
{
    public class PageGroup
    {
        /// <summary>
        /// 页面标签
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// 子页面列表
        /// </summary>
        [JsonProperty("children")]
        public List<Page> Children { get; set; }
    }
}
