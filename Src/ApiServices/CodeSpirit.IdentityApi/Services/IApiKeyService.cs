using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.ApiKey;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// API密钥服务接口
/// </summary>
public interface IApiKeyService : IScopedDependency
{
    /// <summary>
    /// 创建API密钥
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <param name="userId">用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>创建结果（包含明文密钥）</returns>
    Task<CreateApiKeyResultDto> CreateAsync(CreateApiKeyDto createDto, long userId, string tenantId);

    /// <summary>
    /// 验证API密钥
    /// </summary>
    /// <param name="apiKey">明文API密钥</param>
    /// <returns>验证成功返回ApiKeyValidationDto，失败返回null</returns>
    Task<ApiKeyValidationDto?> ValidateAsync(string apiKey);

    /// <summary>
    /// 获取API密钥列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <param name="userId">用户ID</param>
    /// <returns>分页列表</returns>
    Task<PageList<ApiKeyDto>> GetListAsync(ApiKeyQueryDto queryDto, long userId);

    /// <summary>
    /// 获取API密钥详情
    /// </summary>
    /// <param name="id">密钥ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>密钥详情</returns>
    Task<ApiKeyDto> GetAsync(long id, long userId);

    /// <summary>
    /// 更新API密钥
    /// </summary>
    /// <param name="id">密钥ID</param>
    /// <param name="updateDto">更新DTO</param>
    /// <param name="userId">用户ID</param>
    Task UpdateAsync(long id, UpdateApiKeyDto updateDto, long userId);

    /// <summary>
    /// 删除API密钥
    /// </summary>
    /// <param name="id">密钥ID</param>
    /// <param name="userId">用户ID</param>
    Task DeleteAsync(long id, long userId);

    /// <summary>
    /// 撤销API密钥（软删除）
    /// </summary>
    /// <param name="id">密钥ID</param>
    /// <param name="userId">用户ID</param>
    Task RevokeAsync(long id, long userId);

    /// <summary>
    /// 重新生成API密钥
    /// </summary>
    /// <param name="id">密钥ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>新的密钥信息（包含明文密钥）</returns>
    Task<CreateApiKeyResultDto> RegenerateAsync(long id, long userId, string tenantId);
}

