using AutoMapper;
using CodeSpirit.Authorization;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Dtos.Permission;
using CodeSpirit.Navigation.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("权限管理")]
    [Navigation(Icon = "fa-solid fa-lock", PlatformType = PlatformType.Tenant, TitleResourceKey = "Controller.Permissions", TitleResourceType = typeof(NavigationResources))]
    public class PermissionsController : ApiControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly IMapper _mapper;

        public PermissionsController(IPermissionService permissionService, IMapper mapper)
        {
            _permissionService = permissionService;
            _mapper = mapper;
        }

        // GET: api/Permissions
        [HttpGet]
        [DisplayName("获取权限列表")]
        public ActionResult<ApiResponse<PageList<PermissionDto>>> GetPermissions()
        {
            List<PermissionNode> permissions = _permissionService.GetPermissionTree();

            // 使用扩展方法过滤出租户平台权限
            List<PermissionNode> tenantPermissions = permissions.GetTenantPermissions();

            List<PermissionDto> permissionDtos = _mapper.Map<List<PermissionDto>>(tenantPermissions);

            PageList<PermissionDto> listData = new(permissionDtos, permissionDtos.Count);

            return SuccessResponse(listData);
        }

        /// <summary>
        /// 获取权限树(根节点)
        /// </summary>
        /// <returns>权限树列表</returns>
        [HttpGet("tree")]
        [DisplayName("获取权限树")]
        public ActionResult<ApiResponse<List<PermissionTreeDto>>> GetPermissionTree()
        {
            var permissions = _permissionService.GetPermissionTree();

            // 使用扩展方法过滤出租户平台权限
            var tenantPermissions = permissions.GetTenantPermissions();

            var treeNodes = tenantPermissions.Where(p => p.Name != "default").Select(p => new PermissionTreeDto
            {
                Id = p.Name,
                Label = p.DisplayName,
                Value = p.Name,
                Defer = p.Children.Any(),
                Icon = "fa fa-folder"
            }).ToList();

            return SuccessResponse(treeNodes);
        }

        /// <summary>
        /// 获取指定权限的子权限
        /// </summary>
        /// <param name="id">父权限标识</param>
        /// <returns>子权限树节点</returns>
        [HttpGet("tree/{id}")]
        [DisplayName("获取子权限")]
        public ActionResult<ApiResponse<PermissionTreeDeferDto>> GetPermissionChildren(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("无效的权限标识");
            }

            var allPermissions = _permissionService.GetPermissionTree();
            // 使用扩展方法过滤出租户平台权限
            var tenantPermissions = allPermissions.GetTenantPermissions();
            var node = FindNode(tenantPermissions, id);
            if (node == null)
            {
                return NotFound("未找到指定权限");
            }

            var result = node.Children.Select(c => new PermissionTreeDto
            {
                Id = c.Name,
                Label = c.DisplayName,
                Value = c.Name,
                Defer = c.Children.Any(),
                Icon = GetMethodIcon(c.RequestMethod)
            }).ToList();

            return SuccessResponse(new PermissionTreeDeferDto { Options = result });
        }

        /// <summary>
        /// 在权限树中查找指定节点
        /// </summary>
        private PermissionNode FindNode(List<PermissionNode> nodes, string id)
        {
            foreach (var node in nodes)
            {
                if (node.Name == id)
                {
                    return node;
                }

                if (node.Children?.Any() == true)
                {
                    var found = FindNode(node.Children, id);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 根据HTTP方法获取对应的图标
        /// </summary>
        private string GetMethodIcon(string method)
        {
            return method?.ToUpper() switch
            {
                "GET" => "fa fa-eye",           // 查看操作
                "POST" => "fa fa-plus",         // 新增操作
                "PUT" => "fa fa-edit",          // 编辑操作
                "DELETE" => "fa fa-trash",      // 删除操作
                _ => "fa fa-cog"                // 默认图标使用齿轮图标
            };
        }
    }
}

