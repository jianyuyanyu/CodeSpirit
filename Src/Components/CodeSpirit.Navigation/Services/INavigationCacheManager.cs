using CodeSpirit.Navigation.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航缓存管理器接口
    /// </summary>
    public interface INavigationCacheManager
    {
        /// <summary>
        /// 获取缓存的导航树
        /// </summary>
        /// <returns>缓存的导航节点列表，如果不存在则返回null</returns>
        Task<List<NavigationNode>> GetCachedNavigationAsync();

        /// <summary>
        /// 设置导航树缓存
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        Task SetCachedNavigationAsync(List<NavigationNode> nodes);

        /// <summary>
        /// 清除所有导航缓存
        /// </summary>
        Task ClearAllCacheAsync();

        /// <summary>
        /// 清除指定模块的缓存（需要重建整个缓存）
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        Task ClearModuleCacheAsync(string moduleName);
    }
}
