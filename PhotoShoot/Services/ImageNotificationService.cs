using Microsoft.AspNetCore.SignalR;
using PhotoShoot.Hubs;
using PhotoShoot.Models;

namespace PhotoShoot.Services;

public sealed class ImageNotificationService : IImageNotificationService
{
    private readonly IHubContext<ImageHub> _hubContext;
    private readonly ILogger<ImageNotificationService> _logger;

    public ImageNotificationService(IHubContext<ImageHub> hubContext, ILogger<ImageNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public event Action<MonitoredImage>? ImageAdded;

    public async Task NotifyImageAddedAsync(MonitoredImage image, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Dispatching ImageAdded for {ImageId} on instance {InstanceName} (local subscribers and hub clients).",
                image.Id,
                Environment.MachineName);
        }

        ImageAdded?.Invoke(image);
        await _hubContext.Clients.All.SendAsync("ImageAdded", image, cancellationToken);
    }
}
