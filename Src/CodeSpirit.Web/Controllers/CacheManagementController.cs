using CodeSpirit.Amis.Attributes;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using CodeSpirit.Web.Dtos.Cache;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Web;

namespace CodeSpirit.Web.Controllers;

/// <summary>
/// 缓存管理控制器
/// 系统平台功能，仅系统管理员可访问
/// 提供缓存键的查询、删除等管理功能，支持管理所有租户的缓存
/// </summary>
[DisplayName("缓存管理")]
[Navigation(Icon = "fa-solid fa-database", PlatformType = PlatformType.System)]
[Platform(PlatformType.System)]
public class CacheManagementController : ApiControllerBase
{
    private readonly ICacheManagementService _cacheManagementService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CacheManagementController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cacheManagementService">缓存管理服务</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="logger">日志记录器</param>
    public CacheManagementController(
        ICacheManagementService cacheManagementService,
        ICurrentUser currentUser,
        ILogger<CacheManagementController> logger)
    {
        _cacheManagementService = cacheManagementService ?? throw new ArgumentNullException(nameof(cacheManagementService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取缓存键列表
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>缓存键列表</returns>
    [HttpGet]
    [DisplayName("获取缓存键列表")]
    public async Task<ActionResult<ApiResponse<PageList<CacheKeyDto>>>> GetCacheKeys([FromQuery] CacheKeyQueryDto query)
    {
        // 系统平台功能，仅系统管理员可访问
        if (!IsSystemAdmin())
        {
            return BadResponse<PageList<CacheKeyDto>>("仅系统管理员可以访问缓存管理功能");
        }

        var result = await _cacheManagementService.GetKeysAsync(
            pattern: query.Pattern,
            tenantId: query.TenantId, // 租户ID作为可选过滤参数
            page: query.Page,
            perPage: query.PerPage);

        var dtos = result.Items.Select(k => new CacheKeyDto
        {
            Key = k.Key,
            Type = k.Type,
            Ttl = k.Ttl,
            Size = k.Size
        }).ToList();

        var pageList = new PageList<CacheKeyDto>(dtos, result.Total);

        _logger.LogInformation("系统管理员 {UserId} 获取缓存键列表成功，模式: {Pattern}, 租户: {TenantId}, 总数: {Total}, 当前页: {Page}",
            _currentUser.Id, query.Pattern ?? "全部", query.TenantId ?? "全部", result.Total, query.Page);

        return SuccessResponse(pageList);
    }

    /// <summary>
    /// 获取缓存值详情
    /// </summary>
    /// <param name="key">缓存键（需要URL编码）</param>
    /// <returns>缓存值详情</returns>
    [HttpGet("{*key}")]
    [DisplayName("获取缓存值详情")]
    public async Task<ActionResult<ApiResponse<CacheValueDto>>> GetCacheValue(string key)
    {
        // 系统平台功能，仅系统管理员可访问
        if (!IsSystemAdmin())
        {
            return BadResponse<CacheValueDto>("仅系统管理员可以访问缓存管理功能");
        }

        // URL解码
        key = HttpUtility.UrlDecode(key);

        var result = await _cacheManagementService.GetValueAsync(key);
        if (result == null)
        {
            return BadResponse<CacheValueDto>("缓存键不存在");
        }

        var dto = new CacheValueDto
        {
            Key = result.Key,
            Type = result.Type,
            Value = result.Value,
            Ttl = result.Ttl,
            Size = result.Size
        };

        _logger.LogInformation("系统管理员 {UserId} 获取缓存值详情成功，键: {Key}",
            _currentUser.Id, key);

        return SuccessResponse(dto);
    }

    /// <summary>
    /// 删除缓存键
    /// </summary>
    /// <param name="key">缓存键（需要URL编码）</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{*key}")]
    [DisplayName("删除缓存键")]
    public async Task<ActionResult<ApiResponse>> DeleteCacheKey(string key)
    {
        // 系统平台功能，仅系统管理员可访问
        if (!IsSystemAdmin())
        {
            return BadResponse("仅系统管理员可以访问缓存管理功能");
        }

        // URL解码
        key = HttpUtility.UrlDecode(key);

        var deleted = await _cacheManagementService.DeleteKeyAsync(key);
        if (!deleted)
        {
            return BadResponse("缓存键不存在或删除失败");
        }

        _logger.LogWarning("系统管理员 {UserId} 删除缓存键成功，键: {Key}",
            _currentUser.Id, key);

        return SuccessResponse("删除成功");
    }

    /// <summary>
    /// 按模式批量删除缓存
    /// </summary>
    /// <param name="dto">批量删除参数</param>
    /// <returns>操作结果</returns>
    [HttpDelete("pattern")]
    [Operation("按模式删除", OperationActionType.Ajax, null, "确定要删除匹配该模式的所有缓存吗？")]
    [DisplayName("按模式批量删除缓存")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteByPattern([FromBody] BatchDeleteCacheDto dto)
    {
        // 系统平台功能，仅系统管理员可访问
        if (!IsSystemAdmin())
        {
            return BadResponse<object>("仅系统管理员可以访问缓存管理功能");
        }

        var deletedCount = await _cacheManagementService.DeleteByPatternAsync(
            pattern: dto.Pattern,
            tenantId: dto.TenantId); // 租户ID作为可选过滤参数

        _logger.LogWarning("系统管理员 {UserId} 按模式批量删除缓存成功，模式: {Pattern}, 租户: {TenantId}, 删除数量: {DeletedCount}",
            _currentUser.Id, dto.Pattern, dto.TenantId ?? "全部", deletedCount);

        var result = new Dictionary<string, object>
        {
            { "DeletedCount", deletedCount },
            { "Pattern", dto.Pattern },
            { "TenantId", dto.TenantId ?? "全部" }
        };

        return SuccessResponse<object>(result, $"成功删除 {deletedCount} 个缓存键");
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    /// <returns>操作结果</returns>
    [HttpDelete("all")]
    [HeaderOperation("清空所有缓存", OperationActionType.Ajax, null, "警告：此操作将清空所有缓存数据，确定继续吗？")]
    [DisplayName("清空所有缓存")]
    public async Task<ActionResult<ApiResponse>> ClearAllCache()
    {
        // 系统平台功能，仅系统管理员可访问
        if (!IsSystemAdmin())
        {
            return BadResponse("仅系统管理员可以访问缓存管理功能");
        }

        var success = await _cacheManagementService.ClearAllAsync();
        if (!success)
        {
            return BadResponse("清空缓存失败");
        }

        _logger.LogWarning("系统管理员 {UserId} 清空所有缓存成功",
            _currentUser.Id);

        return SuccessResponse("清空所有缓存成功");
    }

    /// <summary>
    /// 检查是否为系统管理员
    /// </summary>
    /// <returns>如果是系统管理员返回true，否则返回false</returns>
    private bool IsSystemAdmin()
    {
        // 检查用户是否属于系统平台
        // 系统平台控制器只允许系统管理员访问
        return _currentUser.IsInRole("SystemAdmin") || 
               _currentUser.IsInRole("Admin") ||
               string.IsNullOrEmpty(_currentUser.TenantId); // 没有租户ID的用户可能是系统管理员
    }
}

