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
        /// <returns>导航节点列表</returns>
        Task<List<NavigationNode>> GetNavigationTreeAsync();

        /// <summary>
        /// 初始化导航树
        /// </summary>
        Task InitializeNavigationTree();

        /// <summary>
        /// 清除指定模块的导航缓存
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        Task ClearModuleNavigationCacheAsync(string moduleName);
    }
}
