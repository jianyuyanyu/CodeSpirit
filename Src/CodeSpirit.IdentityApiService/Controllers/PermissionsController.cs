using AutoMapper;
using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [DisplayName("权限管理")]
    [Page(Label = "权限管理", ParentLabel = "用户中心", Icon = "fa-solid fa-user-plus")]
    public class PermissionsController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PermissionsController> _logger;
        private readonly PermissionService _permissionService;
        private readonly IMapper _mapper;

        public PermissionsController(PermissionService permissionService, IDistributedCache distributedCache, IMapper mapper)
        {
            _cache = distributedCache;
            _permissionService = permissionService;
            _mapper = mapper;
        }

        // GET: api/Permissions
        [HttpGet]
        public async Task<ActionResult<ApiResponse<ListData<PermissionDto>>>> GetPermissions()
        {
            var permissions = _permissionService.GetPermissionTree();

            List<PermissionDto> permissionDtos = _mapper.Map<List<PermissionDto>>(permissions)
                .Where(p => p.ParentId == null) // 获取顶级权限
                .ToList();

            ListData<PermissionDto> listData = new(permissionDtos, permissionDtos.Count);

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
            var permissions = _permissionService.GetPermissionTree();

            var tree = _mapper.Map<List<PermissionTreeDto>>(permissions);

            return Ok(tree);
        }
    }
}
