// Controllers/RolesController.cs
namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    /// <summary>
    /// DTO 类，用于表示权限树的节点。
    /// </summary>
    public class PermissionTreeDto
    {
        /// <summary>
        /// 节点的唯一标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 节点的显示名称。
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 子节点列表。
        /// </summary>
        public List<PermissionTreeDto> Children { get; set; }
    }
}
