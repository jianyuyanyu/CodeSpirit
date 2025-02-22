using CodeSpirit.Core.Extensions;
using CodeSpirit.Navigation;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 导航控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NavigationController : ControllerBase
    {
        private readonly INavigationService _navigationService;
        private readonly ILogger<NavigationController> _logger;

        public NavigationController(
            INavigationService navigationService,
            ILogger<NavigationController> logger)
        {
            _navigationService = navigationService;
            _logger = logger;
        }

        /// <summary>
        /// 获取导航树
        /// </summary>
        /// <returns>导航树节点列表</returns>
        [HttpGet("tree")]
        public async Task<IActionResult> GetNavigationTree()
        {
            try
            {
                var tree = await _navigationService.GetNavigationTreeAsync();
                return Ok(tree);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取导航树时发生错误");
                return StatusCode(500, "获取导航树失败");
            }
        }

        /// <summary>
        /// 获取导航树（Page格式）
        /// </summary>
        /// <returns>Page格式的导航树JSON</returns>
        [HttpGet("site")]
        public async Task<IActionResult> GetNavigationPageTree()
        {
            try
            {
                var tree = await _navigationService.GetNavigationTreeAsync();
                var pageTree = ConvertToPageFormat(tree);

                return Ok(new
                {
                    Pages = pageTree
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取导航树时发生错误");
                return StatusCode(500, "获取导航树失败");
            }
        }

        private IEnumerable<object> ConvertToPageFormat(IEnumerable<NavigationNode> nodes)
        {
            if (nodes == null) return Array.Empty<object>();

            return nodes.Select(node => new
            {
                label = node.Title,
                url = node.IsExternal ? null : node.Path,
                link = node.IsExternal ? node.Path : null,
                icon = node.Icon,
                permission = node.Permission,
                children = ConvertToPageFormat(node.Children),
                schemaApi = !string.IsNullOrEmpty(node.Route) ? $"options:/{node.ModuleName.ToCamelCase()}/{node.Route}?amis" : null
            });
        }
    }
}