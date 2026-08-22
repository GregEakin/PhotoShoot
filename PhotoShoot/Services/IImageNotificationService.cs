using PhotoShoot.Models;

namespace PhotoShoot.Services;

public interface IImageNotificationService
{
    event Action<MonitoredImage>? ImageAdded;

    Task NotifyImageAddedAsync(MonitoredImage image, CancellationToken cancellationToken = default);
}
