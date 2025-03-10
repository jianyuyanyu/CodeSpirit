﻿using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Role
{
    // DTO 用于创建角色
    public class RoleCreateDto
    {
        [Required]
        [MaxLength(100)]
        [DisplayName("名称")]
        public string Name { get; set; }

        [MaxLength(256)]
        [DisplayName("描述")]
        public string Description { get; set; }

        // 权限ID列表
        [DisplayName("权限")]
        [AmisInputTreeField(
        DataSource = "${API_HOST}/api/identity/permissions/tree",
        LabelField = "label",
        ValueField = "id",
        Multiple = true,
        JoinValues = false,
        ExtractValue = true,
        Required = true,
        Placeholder = "请选择权限"
        )]
        public List<string> PermissionAssignments { get; set; }
    }
}
