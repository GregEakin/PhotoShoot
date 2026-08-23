using PhotoShoot.Models;

namespace PhotoShoot.Services;

public interface IImageCatalog
{
    IReadOnlyCollection<MonitoredImage> GetAll();

    MonitoredImage? GetById(string id);

    MonitoredImage Upsert(string filePath, string imageUrl, string thumbnailUrl, string histogramUrl, DateTimeOffset createdUtc);
}
