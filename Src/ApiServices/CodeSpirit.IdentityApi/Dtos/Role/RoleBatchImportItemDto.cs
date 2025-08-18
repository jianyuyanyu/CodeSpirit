using Newtonsoft.Json;
namespace CodeSpirit.IdentityApi.Dtos.Role
{
    /// <summary>
    /// 批量导入角色 DTO
    /// </summary>
    public class RoleBatchImportItemDto
    {
        /// <summary>
        /// 角色名称
        /// </summary>
        [JsonProperty("名称")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 角色描述
        /// </summary>
        [JsonProperty("描述")]
        [MaxLength(256)]
        [Required]
        public string Description { get; set; }
    }
}
