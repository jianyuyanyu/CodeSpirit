using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 站点导航服务接口
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 获取导航树
        /// </summary>
        /// <param name="platformType">平台类型</param>
        /// <returns>导航节点列表</returns>
        Task<List<NavigationNode>> GetNavigationTreeAsync(PlatformType platformType = PlatformType.Both);

        /// <summary>
        /// 初始化导航树
        /// </summary>
        Task InitializeNavigationTree();

        /// <summary>
        /// 清除指定模块的导航缓存
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="platformType">平台类型，null表示清除所有平台缓存</param>
        Task ClearModuleNavigationCacheAsync(string moduleName, PlatformType? platformType = null);

        /// <summary>
        /// 根据权限过滤导航节点
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="hasPermissionService">权限服务</param>
        /// <returns>过滤后的导航节点列表</returns>
        List<NavigationNode> FilterNodesByPermission(List<NavigationNode> nodes, IHasPermissionService hasPermissionService);

        /// <summary>
        /// 根据平台类型过滤导航节点
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="platformType">平台类型</param>
        /// <returns>过滤后的导航节点列表</returns>
        List<NavigationNode> FilterNodesByPlatform(List<NavigationNode> nodes, PlatformType platformType);

        /// <summary>
        /// 根据上下文过滤导航节点（包含平台、权限、版本、设备等）
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>过滤后的导航节点列表</returns>
        List<NavigationNode> FilterNodesByContext(List<NavigationNode> nodes, NavigationFilterContext context);

        /// <summary>
        /// 清除所有导航缓存
        /// </summary>
        Task ClearAllNavigationCacheAsync();

        /// <summary>
        /// 获取当前导航版本号
        /// </summary>
        /// <returns>版本哈希值，如果不存在则返回null</returns>
        Task<string> GetNavigationVersionAsync();
    }

    /// <summary>
    /// 导航过滤上下文
    /// </summary>
    public class NavigationFilterContext
    {
        /// <summary>
        /// 平台类型
        /// </summary>
        public PlatformType PlatformType { get; set; } = PlatformType.Both;

        /// <summary>
        /// 权限服务
        /// </summary>
        public IHasPermissionService PermissionService { get; set; }

        /// <summary>
        /// 当前版本
        /// </summary>
        public string CurrentVersion { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string DeviceType { get; set; } = "desktop";

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        public bool IsDevelopment { get; set; } = false;

        /// <summary>
        /// 是否认证用户
        /// </summary>
        public bool IsAuthenticated { get; set; } = false;

        /// <summary>
        /// 用户标签（用于基于标签的过滤）
        /// </summary>
        public string[] UserTags { get; set; } = [];

        /// <summary>
        /// 分组过滤器
        /// </summary>
        public string[] GroupFilter { get; set; } = [];
    }
}
