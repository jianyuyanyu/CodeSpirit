using AutoMapper;
using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Dtos.Permission;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("权限管理")]
    [Page(Label = "权限管理", ParentLabel = "用户中心", Icon = "fa-solid fa-key", PermissionCode = PermissionCodes.PermissionManagement)]
    [Permission(code: PermissionCodes.PermissionManagement)]
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
        public ActionResult<ApiResponse<PageList<PermissionDto>>> GetPermissions()
        {
            List<PermissionNode> permissions = _permissionService.GetPermissionTree();

            List<PermissionDto> permissionDtos = _mapper.Map<List<PermissionDto>>(permissions);

            PageList<PermissionDto> listData = new(permissionDtos, permissionDtos.Count);

            return SuccessResponse(listData);
        }

        // 新增的 GET: api/Permissions/Tree 端点
        /// <summary>
        /// 获取权限树结构，供前端（如 AMIS InputTree）使用。
        /// </summary>
        /// <returns>权限树的 JSON 结构。</returns>
        [HttpGet("Tree")]
        public ActionResult<List<PermissionTreeDto>> GetPermissionTree()
        {
            List<PermissionNode> permissions = _permissionService.GetPermissionTree();

            List<PermissionTreeDto> tree = _mapper.Map<List<PermissionTreeDto>>(permissions);

            return Ok(tree);
        }
    }
}
