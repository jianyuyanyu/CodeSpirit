using System.Text.Json.Serialization;

namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    /// <summary>
    /// 批量导入角色 DTO
    /// </summary>
    public class RoleBatchImportDto
    {
        /// <summary>
        /// 角色名称
        /// </summary>
        [JsonPropertyName("名称")]
        public string Name { get; set; }

        /// <summary>
        /// 角色描述
        /// </summary>
        [JsonPropertyName("描述")]
        public string Description { get; set; }
    }
}
