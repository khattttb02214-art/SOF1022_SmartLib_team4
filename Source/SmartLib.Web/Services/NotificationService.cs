using Microsoft.AspNetCore.SignalR;
using SmartLib.Web.Hubs;
using SmartLib.Web.Interfaces;

namespace SmartLib.Web.Services;

public class NotificationService
    : INotificationService
{
    private readonly IHubContext<NotificationHub>
        _hubContext;

    public NotificationService(
        IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(
        string message)
    {
        await _hubContext
            .Clients
            .All
            .SendAsync(
                "ReceiveNotification",
                message);
    }
}