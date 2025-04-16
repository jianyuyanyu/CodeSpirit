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

namespace CodeSpirit.IdentityApi.EventHandlers;

/// <summary>
/// 用户创建事件处理器
/// </summary>
public class UserCreatedOrUpdatedEventHandler : IEventHandler<UserCreatedOrUpdatedEvent>
{
    private readonly IUserService _userService;
    private readonly ILogger<UserCreatedOrUpdatedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserCreatedOrUpdatedEventHandler(
        IUserService userService,
        ILogger<UserCreatedOrUpdatedEventHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 处理用户创建事件
    /// </summary>
    public async Task HandleAsync(UserCreatedOrUpdatedEvent @event)
    {
        try
        {
            // 查询用户是否存在
            var existingUser = await _userService.GetUserByUserNameAsync(@event.IdNo);

            Gender gender = @event.Gender switch
            {
                "男" => Gender.Male,
                "女" => Gender.Female,
                _ => Gender.Unknown,
            };

            if (existingUser != null)
            {
                // 更新已存在的用户信息
                _logger.LogInformation("用户更新事件：更新用户信息, 用户ID: {@UserId}", @event.UserId);

                if (@event.UserId != existingUser.Id)
                {
                    _logger.LogWarning("用户更新事件：用户ID不匹配, 用户ID: {@UserId}", @event.UserId);
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

                    await _userService.UpdateAsync(@event.UserId, updateUserDto);
                }
            }
            else
            {
                // 创建新用户
                _logger.LogInformation("用户创建事件：创建新用户, 姓名: {@Name}", @event.Name);
                var createUserDto = new CreateUserDto
                {
                    UserName = @event.UserName,
                    Name = @event.Name,
                    PhoneNumber = @event.PhoneNumber,
                    Email = @event.Email,
                    IdNo = @event.IdNo,
                    Gender = gender,
                };
                var pwd = @event.IdNo.@IsNullOrWhiteSpace() || @event.IdNo.Length < 6 ? "123456" : @event.IdNo[^6..];
                await _userService.CreateAdvancedUserAsync(createUserDto, pwd.ToUpper(), -1, @event.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理用户创建事件失败: {@Event}", @event);
            throw new AppServiceException(500, "处理用户创建事件失败！");
        }
    }
}