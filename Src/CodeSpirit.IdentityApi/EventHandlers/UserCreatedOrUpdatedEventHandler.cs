using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeSpirit.Core;
using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using Microsoft.Extensions.Logging;
using CodeSpirit.Core.Extensions;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.Shared.EventBus.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.IdentityApi.EventHandlers;

/// <summary>
/// 用户创建或更新事件处理器（租户感知）
/// </summary>
public class UserCreatedOrUpdatedEventHandler : ITenantAwareEventHandler<UserCreatedOrUpdatedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserCreatedOrUpdatedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="logger">日志记录器</param>
    public UserCreatedOrUpdatedEventHandler(
        IServiceProvider serviceProvider,
        ILogger<UserCreatedOrUpdatedEventHandler> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 验证事件处理权限
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <returns>是否有权限处理该事件</returns>
    public Task<bool> CanHandleEventAsync(UserCreatedOrUpdatedEvent @event)
    {
        try
        {
            // 检查是否有权限处理该租户的用户事件
            if (string.IsNullOrEmpty(@event.TenantId))
            {
                _logger.LogWarning("事件缺少租户ID，拒绝处理: 事件ID={EventId}", @event.EventId);
                return Task.FromResult(false);
            }
            
            // 简化验证 - 只要有租户ID就可以处理
            _logger.LogDebug("事件权限验证通过: 租户={TenantId}, 事件ID={EventId}", 
                @event.TenantId, @event.EventId);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证事件处理权限时发生异常: 租户={TenantId}, 事件ID={EventId}", 
                @event.TenantId, @event.EventId);
            return Task.FromResult(false);
        }
    }
    
    /// <summary>
    /// 处理事件（标准接口实现）
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(UserCreatedOrUpdatedEvent @event)
    {
        // 验证处理权限
        if (!await CanHandleEventAsync(@event))
        {
            _logger.LogWarning("无权限处理事件，跳过处理: 租户={TenantId}, 事件ID={EventId}", 
                @event.TenantId, @event.EventId);
            return;
        }
        
        // 创建作用域和租户上下文
        using var scope = _serviceProvider.CreateScope();
        var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TenantEventContext>>();
        
        using var tenantContext = new TenantEventContext(
            scope.ServiceProvider, currentUser, @event.TenantId, logger);
            
        await HandleWithTenantContextAsync(@event, tenantContext);
    }
    
    /// <summary>
    /// 处理租户感知事件
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <returns>处理任务</returns>
    public async Task HandleWithTenantContextAsync(
        UserCreatedOrUpdatedEvent @event, 
        ITenantEventContext tenantContext)
    {
        try
        {
            _logger.LogInformation("处理租户用户事件: 租户={TenantId}, 用户ID={UserId}, 事件ID={EventId}", 
                tenantContext.TenantId, @event.UserId, @event.EventId);
            
            // 在租户上下文中获取用户服务（租户ID已自动设置）
            var userService = tenantContext.GetTenantService<IUserService>();
            
            // 根据业务需要设置具体的用户信息
            tenantContext.SetCurrentUserInfo(@event.UserId, @event.UserName);
            
            // 查询用户是否存在（自动应用租户过滤）
            var existingUser = await userService.GetUserByIdIgnoreFiltersAsync(@event.UserId);

            Gender gender = @event.Gender switch
            {
                "男" => Gender.Male,
                "女" => Gender.Female,
                _ => Gender.Unknown,
            };

            if (existingUser != null)
            {
                // 更新已存在的用户信息
                _logger.LogInformation("用户更新事件：更新用户信息, 租户={TenantId}, 用户ID={UserId}", 
                    tenantContext.TenantId, @event.UserId);

                if (@event.UserId != existingUser.Id)
                {
                    _logger.LogWarning("用户更新事件：用户ID不匹配, 租户={TenantId}, 用户ID={UserId}", 
                        tenantContext.TenantId, @event.UserId);
                }
                else
                {
                    var updateUserDto = new UpdateUserDto
                    {
                        Name = @event.Name,
                        PhoneNumber = @event.PhoneNumber,
                        IdNo = @event.IdNo,
                        IsActive = @event.IsActive,
                        Gender = gender,
                    };

                    await userService.UpdateAsync(@event.UserId, updateUserDto);
                    _logger.LogInformation("用户更新完成: 租户={TenantId}, 用户ID={UserId}", 
                        tenantContext.TenantId, @event.UserId);
                }
            }
            else
            {
                // 创建新用户
                _logger.LogInformation("用户创建事件：创建新用户, 租户={TenantId}, 姓名={Name}", 
                    tenantContext.TenantId, @event.Name);
                var createUserDto = new CreateUserDto
                {
                    UserName = @event.UserName,
                    Name = @event.Name,
                    PhoneNumber = @event.PhoneNumber,
                    Email = @event.Email,
                    IdNo = @event.IdNo,
                    Gender = gender,
                };
                var pwd = @event.IdNo.IsNullOrWhiteSpace() || @event.IdNo.Length < 6 ? "123456" : @event.IdNo[^6..];
                
                try
                {
                    // 跨租户事件处理时跳过验证，避免用户名重复检查误报
                    // 租户ID通过EventCurrentUser自动设置，不需要显式传递
                    await userService.CreateAdvancedUserAsync(
                        createUserDto, 
                        pwd.ToUpper(), 
                        -1, 
                        @event.UserId, 
                        skipValidation: true);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx 
                    && sqlEx.Number == 2601 && (sqlEx.Message.Contains("IX_ApplicationUser_TenantId_IdNo") || sqlEx.Message.Contains("IX_ApplicationUser_IdNo")))
                {
                    // 身份证号码唯一索引冲突：在同一租户内身份证号码重复
                    _logger.LogWarning("租户内身份证号码重复：身份证号码 {IdNo} 在租户 {TenantId} 中已存在，跳过用户创建: 用户ID={UserId}", 
                        @event.IdNo, tenantContext.TenantId, @event.UserId);
                    
                    // 不抛出异常，认为处理成功（因为用户已经存在）
                    return;
                }
                _logger.LogInformation("用户创建完成: 租户={TenantId}, 用户ID={UserId}", 
                    tenantContext.TenantId, @event.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理租户用户事件失败: 租户={TenantId}, 事件={@Event}", 
                tenantContext.TenantId, @event);
            throw new Core.AppServiceException(500, "处理用户创建事件失败！");
        }
        // 注意：CurrentUser状态会在tenantContext销毁时自动重置，无需手动处理
    }
}