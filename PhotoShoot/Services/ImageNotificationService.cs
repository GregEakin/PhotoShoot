using Microsoft.AspNetCore.SignalR;
using PhotoShoot.Hubs;
using PhotoShoot.Models;

namespace PhotoShoot.Services;

public sealed class ImageNotificationService : IImageNotificationService
{
    private readonly IHubContext<ImageHub> _hubContext;

    public ImageNotificationService(IHubContext<ImageHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public event Action<MonitoredImage>? ImageAdded;

    public async Task NotifyImageAddedAsync(MonitoredImage image, CancellationToken cancellationToken = default)
    {
        ImageAdded?.Invoke(image);
        await _hubContext.Clients.All.SendAsync("ImageAdded", image, cancellationToken);
    }
}
