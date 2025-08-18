// Controllers/RolesController.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Permission
{
    /// <summary>
    /// DTO 类，用于表示权限树的节点。
    /// </summary>
    public class PermissionTreeDto
    {
        /// <summary>
        /// 权限标识
        /// </summary>
        [Required]
        [DisplayName("节点ID")]
        public string Id { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [Required]
        [DisplayName("节点名称")]
        public string Label { get; set; }

        /// <summary>
        /// 权限值
        /// </summary>
        [Required]
        [DisplayName("权限值")]
        public string Value { get; set; }

        /// <summary>
        /// 是否需要懒加载子节点
        /// </summary>
        [DisplayName("是否需要懒加载子节点")]
        public bool Defer { get; set; }

        /// <summary>
        /// 节点图标
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 子权限列表
        /// </summary>
        [DisplayName("子节点")]
        public List<PermissionTreeDto> Children { get; set; }
    }
}
