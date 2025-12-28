using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
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
/// 系统设置控制器
/// 用于管理全局的系统级设置
/// </summary>
[Display(Name = "Controller.SystemSettings", ResourceType = typeof(NavigationResources))]
[Navigation(Icon = "fa-solid fa-gears", Order = 100, PlatformType = PlatformType.System)]
public class SystemSettingsController : ApiControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly SettingsDbContext _context;
    private readonly ILogger<SystemSettingsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="settingsService">设置服务</param>
    /// <param name="context">设置数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    public SystemSettingsController(
        ISettingsService settingsService,
        SettingsDbContext context,
        ILogger<SystemSettingsController> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取系统设置列表
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>系统设置列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<SettingItemDto>>>> GetSystemSettings([FromQuery] SettingQueryDto query)
    {
        try
        {
            // 构建查询
            var settingsQuery = GetSettingsQuery();

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

            if (query.Scope.HasValue)
            {
                settingsQuery = settingsQuery.Where(s => s.Scope == query.Scope.Value);
            }
            else
            {
                // 默认只显示全局设置
                settingsQuery = settingsQuery.Where(s => s.Scope == SettingScope.Global);
            }

            if (query.IsSystemDefault.HasValue)
            {
                settingsQuery = settingsQuery.Where(s => s.IsSystemDefault == query.IsSystemDefault.Value);
            }

            // 应用关键字搜索
            if (!string.IsNullOrEmpty(query.Keywords))
            {
                settingsQuery = settingsQuery.Where(s =>
                    s.Name.Contains(query.Keywords) ||
                    s.Key.Contains(query.Keywords) ||
                    s.Module.Contains(query.Keywords) ||
                    (s.Description != null && s.Description.Contains(query.Keywords)));
            }

            // 排序
            if (!string.IsNullOrEmpty(query.OrderBy))
            {
                settingsQuery = query.OrderDir?.ToLower() == "desc"
                    ? settingsQuery.OrderByDescending(e => EF.Property<object>(e, query.OrderBy))
                    : settingsQuery.OrderBy(e => EF.Property<object>(e, query.OrderBy));
            }
            else
            {
                settingsQuery = settingsQuery.OrderBy(s => s.Module).ThenBy(s => s.Group).ThenBy(s => s.Key);
            }

            // 分页
            var total = await settingsQuery.CountAsync();
            var items = await settingsQuery
                .Skip((query.Page - 1) * query.PerPage)
                .Take(query.PerPage)
                .ToListAsync();

            var dtos = items.Select(s => new SettingItemDto
            {
                Id = s.Id,
                Module = s.Module,
                Key = s.Key,
                Value = s.Value,
                Name = s.Name,
                Description = s.Description,
                ValueType = s.ValueType,
                Scope = s.Scope,
                Group = s.Group,
                IsSystemDefault = s.IsSystemDefault,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            var pageList = new PageList<SettingItemDto>(dtos, total);

            _logger.LogInformation("获取系统设置列表成功，总数: {Total}, 当前页: {Page}", total, query.Page);

            return SuccessResponse(pageList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统设置列表失败");
            return BadResponse<PageList<SettingItemDto>>("获取系统设置列表失败");
        }
    }

    /// <summary>
    /// 获取指定模块的系统设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <returns>系统设置列表</returns>
    [HttpGet("module/{module}")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, SettingValueDto>>>> GetModuleSettings(string module)
    {
        try
        {
            var settings = await _settingsService.GetAllGlobalSettingsAsync(module);
            var definitions = await _settingsService.GetAllSettingDefinitionsAsync(module);

            var result = settings.Select(s =>
            {
                var definition = definitions.FirstOrDefault(d => d.Key == s.Key);
                return new SettingValueDto
                {
                    Module = module,
                    Key = s.Key,
                    Value = s.Value,
                    Name = definition?.Name ?? s.Key
                };
            }).ToDictionary(s => s.Key, s => s);

            _logger.LogInformation("获取模块 {Module} 的系统设置成功，数量: {Count}", module, result.Count);

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模块 {Module} 的系统设置失败", module);
            return BadResponse<Dictionary<string, SettingValueDto>>($"获取模块 {module} 的系统设置失败");
        }
    }

    /// <summary>
    /// 获取单个系统设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>系统设置</returns>
    [HttpGet("{module}/{key}")]
    public async Task<ActionResult<ApiResponse<SettingValueDto>>> GetSystemSetting(string module, string key)
    {
        try
        {
            var value = await _settingsService.GetGlobalSettingAsync(module, key);
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

            _logger.LogInformation("获取系统设置成功，模块: {Module}, 键: {Key}", module, key);

            return SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统设置失败，模块: {Module}, 键: {Key}", module, key);
            return BadResponse<SettingValueDto>("获取系统设置失败");
        }
    }

    /// <summary>
    /// 更新系统设置
    /// </summary>
    /// <param name="dto">更新参数</param>
    /// <returns>操作结果</returns>
    [HttpPut]
    public async Task<ActionResult<ApiResponse>> UpdateSystemSetting([FromBody] UpdateSettingDto dto)
    {
        try
        {
            var success = await _settingsService.SetGlobalSettingAsync(dto.Module, dto.Key, dto.Value, dto.Reason);

            if (!success)
            {
                return BadResponse("更新系统设置失败");
            }

            _logger.LogInformation("更新系统设置成功，模块: {Module}, 键: {Key}, 原因: {Reason}", dto.Module, dto.Key, dto.Reason);

            return SuccessResponse("更新系统设置成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新系统设置失败，模块: {Module}, 键: {Key}", dto.Module, dto.Key);
            return BadResponse("更新系统设置失败");
        }
    }

    /// <summary>
    /// 获取设置历史记录
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>设置历史记录</returns>
    [HttpGet("history/{module}/{key}")]
    public async Task<ActionResult<ApiResponse<List<SettingHistory>>>> GetSettingHistory(string module, string key)
    {
        try
        {
            var history = await _settingsService.GetSettingHistoryAsync(module, key);

            _logger.LogInformation("获取设置历史成功，模块: {Module}, 键: {Key}, 记录数: {Count}", module, key, history.Count);

            return SuccessResponse(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设置历史失败，模块: {Module}, 键: {Key}", module, key);
            return BadResponse<List<SettingHistory>>("获取设置历史失败");
        }
    }

    /// <summary>
    /// 导出系统设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <returns>设置导出数据</returns>
    [HttpGet("export/{module}")]
    [HeaderOperation("导出设置", OperationActionType.Ajax)]
    public async Task<ActionResult<ApiResponse<string>>> ExportSettings(string module)
    {
        try
        {
            var json = await _settingsService.ExportSettingsAsync(module);

            _logger.LogInformation("导出系统设置成功，模块: {Module}", module);

            return SuccessResponse(json, "导出系统设置成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出系统设置失败，模块: {Module}", module);
            return BadResponse<string>("导出系统设置失败");
        }
    }

    /// <summary>
    /// 导入系统设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="settingsJson">设置数据</param>
    /// <returns>操作结果</returns>
    [HttpPost("import/{module}")]
    [HeaderOperation("导入设置", OperationActionType.Ajax)]
    public async Task<ActionResult<ApiResponse>> ImportSettings(string module, [FromBody] string settingsJson)
    {
        try
        {
            var success = await _settingsService.ImportSettingsAsync(module, settingsJson);

            if (!success)
            {
                return BadResponse("导入系统设置失败");
            }

            _logger.LogInformation("导入系统设置成功，模块: {Module}", module);

            return SuccessResponse("导入系统设置成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入系统设置失败，模块: {Module}", module);
            return BadResponse("导入系统设置失败");
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

