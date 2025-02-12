using CodeSpirit.Amis.App;
using CodeSpirit.Amis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CodeSpirit.Amis
{
    /// <summary>
    /// 提供站点配置相关的服务，包括处理控制器和动作方法上的页面属性，
    /// 以及从配置文件中加载页面信息。
    /// </summary>
    public class SiteConfigurationService : ISiteConfigurationService
    {
        private readonly IPageCollector _pageCollector;
        private readonly ILogger<SiteConfigurationService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// 构造函数，注入页面收集器、日志记录服务和 HTTP 上下文访问器。
        /// </summary>
        public SiteConfigurationService(
            IPageCollector pageCollector,
            ILogger<SiteConfigurationService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _pageCollector = pageCollector;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取站点的整体配置，包括页面层级结构等信息。
        /// </summary>
        /// <returns>包含站点配置的 API 响应。</returns>
        public async Task<ApiResponse<AmisApp>> GetSiteConfigurationAsync()
        {
            Dictionary<string, Page> pageDict = await _pageCollector.CollectPagesAsync();

            // 获取用户权限
            ClaimsPrincipal user = _httpContextAccessor.HttpContext?.User;
            HashSet<string> userPermissions = [];
            bool isAdmin = false;

            if (user?.Claims != null)
            {
                userPermissions = user.FindAll("permissions").Select(c => c.Value).ToHashSet();
                isAdmin = user.FindAll(ClaimTypes.Role).Any(c => c.Value == "Admin");
            }

            // 过滤没有权限的页面
            Dictionary<string, Page> filteredPageDict = pageDict
                .Where(kvp => isAdmin)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            List<Page> topLevelPages = BuildHierarchy(filteredPageDict);

            return new ApiResponse<AmisApp>
            {
                Status = 0,
                Msg = string.Empty,
                Data = new AmisApp
                {
                    Pages =
                    [
                        new PageGroup
                        {
                            Children = topLevelPages
                        }
                    ]
                }
            };
        }

        #region Helper Methods

        /// <summary>
        /// 构建页面层级结构，设置父子关系，并检测循环引用。
        /// </summary>
        /// <param name="pageDict">所有页面的字典。</param>
        /// <returns>顶级页面列表。</returns>
        private List<Page> BuildHierarchy(Dictionary<string, Page> pageDict)
        {
            List<Page> topLevelPages = [];

            foreach (Page page in pageDict.Values)
            {
                if (!string.IsNullOrEmpty(page.ParentLabel))
                {
                    if (pageDict.TryGetValue(page.ParentLabel, out Page parentPage))
                    {
                        // 检测循环引用
                        if (DetectCycle(pageDict, page.Label, parentPage.Label))
                        {
                            _logger.LogWarning("Cycle detected: Page '{Child}' cannot have parent '{Parent}' due to circular reference.", page.Label, parentPage.Label);
                            topLevelPages.Add(page);
                            continue;
                        }

                        parentPage.Children ??= [];
                        parentPage.Children.Add(page);
                    }
                    else
                    {
                        _logger.LogWarning("Parent page with label '{ParentLabel}' not found for page '{PageLabel}'. Treating as top-level page.", page.ParentLabel, page.Label);
                        topLevelPages.Add(page);
                    }
                }
                else
                {
                    topLevelPages.Add(page);
                }
            }

            return topLevelPages;
        }

        /// <summary>
        /// 检测页面层级中是否存在循环引用。
        /// </summary>
        /// <param name="pageDict">所有页面的字典。</param>
        /// <param name="childLabel">子页面标签。</param>
        /// <param name="parentLabel">父页面标签。</param>
        /// <returns>如果存在循环引用，则返回 true；否则，返回 false。</returns>
        private bool DetectCycle(Dictionary<string, Page> pageDict, string childLabel, string parentLabel)
        {
            string currentLabel = parentLabel;
            HashSet<string> visited = [childLabel];

            while (!string.IsNullOrEmpty(currentLabel))
            {
                if (!pageDict.TryGetValue(currentLabel, out Page currentPage))
                {
                    break;
                }

                if (visited.Contains(currentPage.Label))
                {
                    return true;
                }

                visited.Add(currentPage.Label);
                currentLabel = currentPage.ParentLabel;
            }

            return false;
        }

        #endregion
    }
}