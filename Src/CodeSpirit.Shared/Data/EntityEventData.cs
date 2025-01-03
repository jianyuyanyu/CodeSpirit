using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CodeSpirit.Shared.Data
{
    /// <summary>
    /// 实体事件数据
    /// </summary>
    public class EntityEventData
    {
        /// <summary>
        /// 实体状态
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityState EntityState { get; set; }

        /// <summary>
        /// 实体名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 实体数据
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// ES Index 名称
        /// </summary>
        public string Index { get; set; }

    }
}