using CodeSpirit.Core.Extensions;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Interfaces;

namespace CodeSpirit.IdentityApi.EventHandlers
{
    public class UserDeletedEventHandler : IEventHandler<UserDeletedEvent>
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserDeletedEventHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UserDeletedEventHandler(
            IUserService userService,
            ILogger<UserDeletedEventHandler> logger)
        {
            _userService = userService;
            _logger = logger;
            _logger.LogInformation("UserDeletedEventHandler 已初始化");
        }

        /// <summary>
        /// 处理用户删除事件
        /// </summary>
        public async Task HandleAsync(UserDeletedEvent @event)
        {
            try
            {
                _logger.LogInformation("开始处理用户删除事件: {@Event}", @event);
                
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

                _logger.LogInformation("正在删除用户: {UserId}", @event.UserId);
                await _userService.DeleteAsync(@event.UserId);
                _logger.LogInformation("用户删除成功: {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理用户删除事件失败: {@Event}", @event);
                throw new AppServiceException(500, $"处理用户删除事件失败: {ex.Message}");
            }
        }
    }
}
