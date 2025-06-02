using System.Collections.Generic;
using CodeSpirit.Navigation;

namespace CodeSpirit.Navigation.Tests.Extensions
{
    /// <summary>
    /// NavigationService扩展类，用于在测试中访问私有方法
    /// </summary>
    public static class NavigationServiceExtensions
    {
        /// <summary>
        /// 获取当前模块列表（测试专用）
        /// </summary>
        /// <param name="service">导航服务</param>
        /// <returns>模块列表</returns>
        public static List<string> GetCurrentModulesForTest(this NavigationService service)
        {
            // 测试扩展方法，实际实现将被模拟
            return new List<string>();
        }

        /// <summary>
        /// 获取配置文件中的模块列表（测试专用）
        /// </summary>
        /// <param name="service">导航服务</param>
        /// <returns>模块列表</returns>
        public static List<string> GetConfigModulesForTest(this NavigationService service)
        {
            // 测试扩展方法，实际实现将被模拟
            return new List<string>();
        }
    }
} 