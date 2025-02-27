using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CodeSpirit.ConfigCenter.Hubs
{
    /// <summary>
    /// 配置中心集中Hub，处理所有SignalR通信
    /// </summary>
    public class ConfigHub : Hub
    {
        private readonly ILogger<ConfigHub> _logger;

        public ConfigHub(ILogger<ConfigHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 通用组订阅方法
        /// </summary>
        public async Task SubscribeToGroup(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            _logger.LogInformation(
                "Client {ConnectionId} subscribed to group {Group}",
                Context.ConnectionId, group);
        }

        /// <summary>
        /// 通用组取消订阅方法
        /// </summary>
        public async Task UnsubscribeFromGroup(string group)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            _logger.LogInformation(
                "Client {ConnectionId} unsubscribed from group {Group}",
                Context.ConnectionId, group);
        }

        /// <summary>
        /// 订阅应用配置组
        /// </summary>
        public async Task SubscribeToAppConfig(string appId, string environment)
        {
            string groupName = GetAppConfigGroupName(appId, environment);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation(
                "Client {ConnectionId} subscribed to app config for {AppId} in {Environment}",
                Context.ConnectionId, appId, environment);
        }

        /// <summary>
        /// 取消订阅应用配置组
        /// </summary>
        public async Task UnsubscribeFromAppConfig(string appId, string environment)
        {
            string groupName = GetAppConfigGroupName(appId, environment);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation(
                "Client {ConnectionId} unsubscribed from app config for {AppId} in {Environment}",
                Context.ConnectionId, appId, environment);
        }

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected to ConfigHub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 客户端断开连接事件
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation(
                "Client {ConnectionId} disconnected from ConfigHub. Reason: {Reason}", 
                Context.ConnectionId, 
                exception?.Message ?? "Normal disconnection");
                
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 获取应用配置组名称
        /// </summary>
        private string GetAppConfigGroupName(string appId, string environment)
        {
            return $"config:{appId}:{environment}";
        }

        /// <summary>
        /// 注册应用配置监听（与客户端兼容）
        /// </summary>
        public async Task RegisterAppConfigListener(string appId, string environment)
        {
            // 调用已有的订阅方法
            await SubscribeToAppConfig(appId, environment);
        }

        /// <summary>
        /// 取消注册应用配置监听（与客户端兼容）
        /// </summary>
        public async Task UnregisterAppConfigListener(string appId, string environment)
        {
            // 调用已有的取消订阅方法
            await UnsubscribeFromAppConfig(appId, environment);
        }

        /// <summary>
        /// 加入应用配置组（与旧的ConfigChangeHub兼容）
        /// </summary>
        public async Task JoinAppGroup(string appId, string environment)
        {
            string groupName = GetAppConfigGroupName(appId, environment);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation(
                "Client {ConnectionId} joined app group for {AppId} in {Environment}",
                Context.ConnectionId, appId, environment);
        }

        /// <summary>
        /// 离开应用配置组（与旧的ConfigChangeHub兼容）
        /// </summary>
        public async Task LeaveAppGroup(string appId, string environment)
        {
            string groupName = GetAppConfigGroupName(appId, environment);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation(
                "Client {ConnectionId} left app group for {AppId} in {Environment}",
                Context.ConnectionId, appId, environment);
        }
    }
} 