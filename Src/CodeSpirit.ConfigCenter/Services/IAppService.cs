using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Models;

namespace CodeSpirit.ConfigCenter.Services
{
    /// <summary>
    /// 应用程序服务接口
    /// </summary>
    public interface IAppService
    {
        /// <summary>
        /// 批量删除应用
        /// </summary>
        /// <param name="appIds">要删除的应用ID集合</param>
        /// <returns>成功删除数量和失败的应用ID列表</returns>
        Task<(int successCount, List<string> failedAppIds)> BatchDeleteAppsAsync(IEnumerable<string> appIds);

        /// <summary>
        /// 批量导入应用
        /// </summary>
        /// <param name="importData">要导入的应用数据集合</param>
        /// <returns>成功导入数量和失败的应用ID列表</returns>
        Task<(int successCount, List<string> failedAppIds)> BatchImportAppsAsync(IEnumerable<AppBatchImportItemDto> importData);

        /// <summary>
        /// 创建新应用
        /// </summary>
        /// <param name="appDto">应用创建数据传输对象</param>
        /// <returns>创建的应用实体</returns>
        Task<AppDto> CreateAppAsync(CreateAppDto appDto);

        /// <summary>
        /// 删除指定应用
        /// </summary>
        /// <param name="appId">要删除的应用ID</param>
        Task DeleteAppAsync(string appId);

        /// <summary>
        /// 获取指定应用的详细信息
        /// </summary>
        /// <param name="appId">应用ID</param>
        /// <returns>应用详细信息</returns>
        Task<AppDto> GetAppAsync(string appId);

        /// <summary>
        /// 分页查询应用列表
        /// </summary>
        /// <param name="queryDto">查询条件数据传输对象</param>
        /// <returns>分页的应用列表</returns>
        Task<PageList<AppDto>> GetAppsAsync(AppQueryDto queryDto);

        /// <summary>
        /// 更新应用信息
        /// </summary>
        /// <param name="appDto">应用更新数据传输对象</param>
        Task UpdateAppAsync(string id, UpdateAppDto appDto);

        /// <summary>
        /// 验证应用密钥
        /// </summary>
        /// <param name="appId">应用ID</param>
        /// <param name="secret">待验证的密钥</param>
        /// <returns>验证是否通过</returns>
        Task<bool> ValidateAppSecretAsync(string appId, string secret);

        /// <summary>
        /// 快速保存应用信息
        /// </summary>
        /// <param name="request">快速保存请求数据</param>
        Task QuickSaveAppsAsync(QuickSaveRequestDto request);
    }
}