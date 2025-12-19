using CodeSpirit.Navigation.Models;
using System.Collections.Generic;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航本地化服务接口
    /// </summary>
    public interface INavigationLocalizationService
    {
        /// <summary>
        /// 本地化导航树，将导航树中的文本字段根据当前语言进行转换
        /// </summary>
        /// <param name="nodes">原始导航节点列表</param>
        /// <returns>本地化后的导航节点列表（深拷贝，不修改原始数据）</returns>
        List<NavigationNode> LocalizeNavigationTree(List<NavigationNode> nodes);
    }
}

