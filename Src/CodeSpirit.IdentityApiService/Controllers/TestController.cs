using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    public class TestController : ApiControllerBase
    {
        private readonly PermissionService permissionService;

        public TestController(PermissionService permissionService)
        {
            this.permissionService = permissionService;
        }

        [HttpGet("")]
        public List<PermissionNode> Index()
        {
            return permissionService.GetPermissionTree();
        }
    }
}
