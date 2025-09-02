using System.Threading.Tasks;

namespace CodeSpirit.Shared.Services
{
    /// <summary>
    /// AI表单填充服务接口
    /// </summary>
    public interface IAiFormFillService
    {
        /// <summary>
        /// 填充表单字段
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <param name="triggerValue">触发值</param>
        /// <param name="existingData">现有数据</param>
        /// <returns>填充后的数据</returns>
        Task<T> FillFormAsync<T>(string triggerValue, T? existingData = null) where T : class, new();

        /// <summary>
        /// 验证DTO是否支持AI填充
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <returns>是否支持</returns>
        bool IsAiFillSupported<T>() where T : class;
    }
}
