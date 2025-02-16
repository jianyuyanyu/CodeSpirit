using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CodeSpirit.ConfigCenter.Hubs
{
    public class ConfigHub : Hub
    {
        private readonly ILogger<ConfigHub> _logger;

        public ConfigHub(ILogger<ConfigHub> logger)
        {
            _logger = logger;
        }

        public async Task SubscribeToGroup(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            _logger.LogInformation(
                "Client {ConnectionId} subscribed to group {Group}",
                Context.ConnectionId, group);
        }

        public async Task UnsubscribeFromGroup(string group)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            _logger.LogInformation(
                "Client {ConnectionId} unsubscribed from group {Group}",
                Context.ConnectionId, group);
        }
    }
} 