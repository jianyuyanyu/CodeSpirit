using CodeSpirit.Core.Extensions;
using CodeSpirit.Navigation;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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
                var pageTree = ConvertToPageFormat(tree).ToList();
                if (pageTree.Any())
                {
                    pageTree.Insert(0, new
                    {
                        label = "控制台",
                        url = "/",
                        icon = "fa-solid fa-gauge-high",
                        schema = new
                        {
                            type = "page",
                            body = new
                            {
                                type = "markdown",
                                value = "## 框架概览\r\n\r\nCodeSpirit（码灵）是一款革命性的全栈低代码开发框架，通过智能代码生成引擎与AI深度协同，实现**后端驱动式全栈开发范式**。基于.NET 9技术栈构建，将具备企业级技术深度与云原生扩展能力，提供从界面生成、业务逻辑编排到系统运维的全生命周期支持。\r\n- Github：[xin-lai/CodeSpirit](https://github.com/xin-lai/CodeSpirit)\r\n- Gitee：[magicodes/CodeSpirit](https://gitee.com/magicodes/code-spirit)",
                                options = new
                                {
                                    html = true,
                                    linkify = true,
                                    breaks = false
                                }
                            }
                        }
                    });
                }

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

            return nodes.Where(node => !string.IsNullOrEmpty(node.Title)).Select(node => new
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