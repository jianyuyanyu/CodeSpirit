using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Resources;
using CodeSpirit.Settings.Data;
using CodeSpirit.Settings.Models;
using CodeSpirit.Settings.Services.Interfaces;
using CodeSpirit.Web.Dtos.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Web.Controllers;

/// <summary>
/// 租户设置控制器
/// 用于管理租户特定的设置
/// </summary>
[Display(Name = "Controller.TenantSettings", ResourceType = typeof(NavigationResources))]
[Navigation(Icon = "fa-solid fa-building-user", Order = 101, PlatformType = PlatformType.Both)]
public class TenantSettingsController : ApiControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly SettingsDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TenantSettingsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="settingsService">设置服务</param>
    /// <param name="context">设置数据库上下文</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="logger">日志记录器</param>
    public TenantSettingsController(
        ISettingsService settingsService,
        SettingsDbContext context,
        ICurrentUser currentUser,
        ILogger<TenantSettingsController> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取租户设置列表
    /// 仅返回租户自定义的设置，不包含全局设置
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>租户设置列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<SettingItemDto>>>> GetTenantSettings([FromQuery] TenantSettingQueryDto query)
    {
        try
        {
            var tenantId = _currentUser.TenantId;

            if (string.IsNullOrEmpty(tenantId))
            {
                return BadResponse<PageList<SettingItemDto>>("租户ID不能为空");
            }

            // 只查询租户自己的设置
            var settingsQuery = _context.SettingItems
                .Where(s => s.Scope == SettingScope.Tenant && s.ScopeId == tenantId);

            // 应用过滤条件
            if (!string.IsNullOrEmpty(query.Module))
            {
                settingsQuery = settingsQuery.Where(s => s.Module.Contains(query.Module));
            }

            if (!string.IsNullOrEmpty(query.Key))
            {
                settingsQuery = settingsQuery.Where(s => s.Key.Contains(query.Key));
            }

            if (!string.IsNullOrEmpty(query.Name))
            {
                settingsQuery = settingsQuery.Where(s => s.Name.Contains(query.Name));
            }

            if (!string.IsNullOrEmpty(query.Group))
            {
                settingsQuery = settingsQuery.Where(s => s.Group != null && s.Group.Contains(query.Group));
            }

            if (!string.IsNullOrEmpty(query.Keywords))
            {
                settingsQuery = settingsQuery.Where(s =>
                    s.Name.Contains(query.Keywords) ||
                    s.Key.Contains(query.Keywords) ||
                    s.Module.Contains(query.Keywords) ||
                    (s.Description != null && s.Description.Contains(query.Keywords)));
            }

            // 应用排序
            if (!string.IsNullOrEmpty(query.OrderBy))
            {
                var propertyInfo = typeof(SettingItem).GetProperty(query.OrderBy);
                if (propertyInfo != null)
                {
                    settingsQuery = query.OrderDir?.ToLower() == "desc"
                        ? settingsQuery.OrderByDescending(s => EF.Property<object>(s, query.OrderBy))
                        : settingsQuery.OrderBy(s => EF.Property<object>(s, query.OrderBy));
                }
            }
            else
            {
                settingsQuery = settingsQuery
                    .OrderBy(s => s.Module)
                    .ThenBy(s => s.Group)
                    .ThenBy(s => s.Key);
            }

            // 获取总数
            var total = await settingsQuery.CountAsync();

            // 分页查询
            var tenantSettings = await settingsQuery
                .Skip((query.Page - 1) * query.PerPage)
                .Take(query.PerPage)
                .ToListAsync();

            // 转换为 DTO
            var dtos = tenantSettings.Select(s => new SettingItemDto
            {
                Id = s.Id,
                Module = s.Module,
                Key = s.Key,
                Value = s.Value,
                Name = s.Name,
                Description = s.Description,
                ValueType = s.ValueType,
                Scope = s.Scope,
                ScopeId = s.ScopeId,
                Group = s.Group,
                IsSystemDefault = false, // 租户设置都不是系统默认值
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            var pageList = new PageList<SettingItemDto>(dtos, total);

            _logger.LogInformation("获取租户 {TenantId} 设置列表成功，总数: {Total}", tenantId, total);

            return SuccessResponse(pageList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户设置列表失败");
            return BadResponse<PageList<SettingItemDto>>("获取租户设置列表失败");
        }
    }

    /// <summary>
    /// 获取指定模块的租户设置
    /// 自动合并全局默认设置和租户自定义设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <returns>租户设置字典</returns>
    [HttpGet("module/{module}")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, SettingValueDto>>>> GetModuleTenantSettings(string module)
    {
        try
        {
            var tenantId = _currentUser.TenantId;

            if (string.IsNullOrEmpty(tenantId))
            {
                return BadResponse<Dictionary<string, SettingValueDto>>("租户ID不能为空");
            }

            // 使用 SettingsService 的方法，自动合并全局和租户设置
            var settings = await _settingsService.GetAllTenantSettingsAsync(module, tenantId);
            var definitions = await _settingsService.GetAllSettingDefinitionsAsync(module);

            var result = settings.Select(kvp =>
            {
                var definition = definitions.FirstOrDefault(d => d.Key == kvp.Key);
                return new SettingValueDto
                {
                    Module = module,
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Name = definition?.Name ?? kvp.Key
                };
            }).ToDictionary(s => s.Key, s => s);

            _logger.LogInformation("获取租户 {TenantId} 模块 {Module} 的设置成功，数量: {Count}", tenantId, module, result.Count);

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            var currentTenantId = _currentUser.TenantId;
            _logger.LogError(ex, "获取租户 {TenantId} 模块 {Module} 的设置失败", currentTenantId, module);
            return BadResponse<Dictionary<string, SettingValueDto>>($"获取租户模块 {module} 的设置失败");
        }
    }

    /// <summary>
    /// 获取单个租户设置
    /// 如果租户未自定义，自动返回全局默认值
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>租户设置</returns>
    [HttpGet("{module}/{key}")]
    public async Task<ActionResult<ApiResponse<SettingValueDto>>> GetTenantSetting(string module, string key)
    {
        try
        {
            var tenantId = _currentUser.TenantId;

            if (string.IsNullOrEmpty(tenantId))
            {
                return BadResponse<SettingValueDto>("租户ID不能为空");
            }

            // 使用 SettingsService 的方法，自动处理租户设置和全局设置的回退逻辑
            var value = await _settingsService.GetTenantSettingAsync(module, key, tenantId);
            var definition = await _settingsService.GetSettingDefinitionAsync(module, key);

            if (value == null)
            {
                return BadResponse<SettingValueDto>("设置不存在");
            }

            var dto = new SettingValueDto
            {
                Module = module,
                Key = key,
                Value = value,
                Name = definition?.Name ?? key
            };

            _logger.LogInformation("获取租户 {TenantId} 设置成功，模块: {Module}, 键: {Key}", tenantId, module, key);

            return SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            var currentTenantId = _currentUser.TenantId;
            _logger.LogError(ex, "获取租户 {TenantId} 设置失败，模块: {Module}, 键: {Key}", currentTenantId, module, key);
            return BadResponse<SettingValueDto>("获取租户设置失败");
        }
    }

    /// <summary>
    /// 更新租户设置
    /// </summary>
    /// <param name="dto">更新参数</param>
    /// <returns>操作结果</returns>
    [HttpPut]
    public async Task<ActionResult<ApiResponse>> UpdateTenantSetting([FromBody] UpdateTenantSettingDto dto)
    {
        try
        {
            var tenantId = _currentUser.TenantId;

            if (string.IsNullOrEmpty(tenantId))
            {
                return BadResponse("租户ID不能为空");
            }

            var success = await _settingsService.SetTenantSettingAsync(dto.Module, dto.Key, dto.Value, tenantId, dto.Reason);

            if (!success)
            {
                return BadResponse("更新租户设置失败");
            }

            _logger.LogInformation("更新租户 {TenantId} 设置成功，模块: {Module}, 键: {Key}, 原因: {Reason}",
                tenantId, dto.Module, dto.Key, dto.Reason);

            return SuccessResponse("更新租户设置成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新租户设置失败，模块: {Module}, 键: {Key}", dto.Module, dto.Key);
            return BadResponse("更新租户设置失败");
        }
    }

    ///// <summary>
    ///// 重置租户设置为全局默认值
    ///// </summary>
    ///// <param name="module">模块名称</param>
    ///// <param name="key">设置键（可选，不指定则重置该模块所有设置）</param>
    ///// <returns>操作结果</returns>
    //[HttpDelete("reset/{module}")]
    //[HeaderOperation("重置为默认", OperationActionType.Ajax, null, "确定要重置为全局默认值吗？")]
    //public async Task<ActionResult<ApiResponse>> ResetTenantSettings(
    //    string module,
    //    [FromQuery] string? key = null)
    //{
    //    try
    //    {
    //        var tenantId = _currentUser.TenantId;

    //        if (string.IsNullOrEmpty(tenantId))
    //        {
    //            return BadResponse("租户ID不能为空");
    //        }

    //        var success = await _settingsService.ResetTenantSettingToDefaultAsync(module, key, tenantId);

    //        if (!success)
    //        {
    //            return BadResponse("重置租户设置失败");
    //        }

    //        _logger.LogInformation("重置租户 {TenantId} 设置成功，模块: {Module}, 键: {Key}", tenantId, module, key ?? "全部");

    //        return SuccessResponse("重置租户设置成功");
    //    }
    //    catch (Exception ex)
    //    {
    //        var currentTenantId = _currentUser.TenantId;
    //        _logger.LogError(ex, "重置租户 {TenantId} 设置失败，模块: {Module}, 键: {Key}", currentTenantId, module, key);
    //        return BadResponse("重置租户设置失败");
    //    }
    //}

    /// <summary>
    /// 获取租户设置历史记录
    /// 只返回当前租户的历史记录
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>设置历史记录</returns>
    [HttpGet("history/{module}/{key}")]
    public async Task<ActionResult<ApiResponse<List<SettingHistory>>>> GetTenantSettingHistory(string module, string key)
    {
        try
        {
            var tenantId = _currentUser.TenantId;

            if (string.IsNullOrEmpty(tenantId))
            {
                return BadResponse<List<SettingHistory>>("租户ID不能为空");
            }

            var allHistory = await _settingsService.GetSettingHistoryAsync(module, key);
            
            // 只返回当前租户的历史记录
            var tenantHistory = allHistory.Where(h => h.TenantId == tenantId).ToList();

            _logger.LogInformation("获取租户 {TenantId} 设置历史成功，模块: {Module}, 键: {Key}, 记录数: {Count}", 
                tenantId, module, key, tenantHistory.Count);

            return SuccessResponse(tenantHistory);
        }
        catch (Exception ex)
        {
            var currentTenantId = _currentUser.TenantId;
            _logger.LogError(ex, "获取租户 {TenantId} 设置历史失败，模块: {Module}, 键: {Key}", currentTenantId, module, key);
            return BadResponse<List<SettingHistory>>("获取租户设置历史失败");
        }
    }

    /// <summary>
    /// 获取设置查询
    /// </summary>
    /// <returns>设置查询</returns>
    private IQueryable<SettingItem> GetSettingsQuery()
    {
        return _context.SettingItems.AsQueryable();
    }
}

