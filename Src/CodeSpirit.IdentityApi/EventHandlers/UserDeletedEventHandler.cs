using CodeSpirit.Core.Extensions;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.EventBus.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.IdentityApi.EventHandlers
{
    /// <summary>
    /// 用户删除事件处理器（租户感知）
    /// </summary>
    public class UserDeletedEventHandler : ITenantAwareEventHandler<UserDeletedEvent>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserDeletedEventHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="logger">日志记录器</param>
        public UserDeletedEventHandler(
            IServiceProvider serviceProvider,
            ILogger<UserDeletedEventHandler> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("UserDeletedEventHandler 已初始化");
        }

        /// <summary>
        /// 验证事件处理权限
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <returns>是否有权限处理该事件</returns>
        public Task<bool> CanHandleEventAsync(UserDeletedEvent @event)
        {
            try
            {
                // 检查是否有权限处理该租户的用户删除事件
                if (string.IsNullOrEmpty(@event.TenantId))
                {
                    _logger.LogWarning("事件缺少租户ID，拒绝处理: 事件ID={EventId}", @event.EventId);
                    return Task.FromResult(false);
                }
                
                // 简化验证 - 只要有租户ID就可以处理
                _logger.LogDebug("用户删除事件权限验证通过: 租户={TenantId}, 事件ID={EventId}", 
                    @event.TenantId, @event.EventId);
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户删除事件处理权限时发生异常: 租户={TenantId}, 事件ID={EventId}", 
                    @event.TenantId, @event.EventId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 处理事件（标准接口实现）
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <returns>处理任务</returns>
        public async Task HandleAsync(UserDeletedEvent @event)
        {
            // 验证处理权限
            if (!await CanHandleEventAsync(@event))
            {
                _logger.LogWarning("无权限处理用户删除事件，跳过处理: 租户={TenantId}, 事件ID={EventId}", 
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
        /// 处理租户感知的用户删除事件
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <param name="tenantContext">租户上下文</param>
        /// <returns>处理任务</returns>
        public async Task HandleWithTenantContextAsync(UserDeletedEvent @event, ITenantEventContext tenantContext)
        {
            try
            {
                _logger.LogInformation("开始处理租户用户删除事件: 租户={TenantId}, 用户ID={UserId}, 事件ID={EventId}", 
                    tenantContext.TenantId, @event.UserId, @event.EventId);
                
                if (@event == null)
                {
                    _logger.LogError("收到的事件为空");
                    return;
                }

                if (@event.UserId <= 0)
                {
                    _logger.LogError("事件中的用户ID无效: {UserId}", @event.UserId);
                    return;
                }

                // 在租户上下文中获取用户服务
                var userService = tenantContext.GetTenantService<IUserService>();

                _logger.LogInformation("正在删除用户: 租户={TenantId}, 用户ID={UserId}", 
                    tenantContext.TenantId, @event.UserId);
                await userService.DeleteAsync(@event.UserId);
                _logger.LogInformation("用户删除成功: 租户={TenantId}, 用户ID={UserId}", 
                    tenantContext.TenantId, @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理租户用户删除事件失败: 租户={TenantId}, 事件={@Event}", 
                    tenantContext.TenantId, @event);
                throw new AppServiceException(500, $"处理用户删除事件失败: {ex.Message}");
            }
        }
    }
}
