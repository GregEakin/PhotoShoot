using System.Collections.Concurrent;
using PhotoShoot.Models;

namespace PhotoShoot.Services;

public sealed class InMemoryImageCatalog : IImageCatalog
{
    private readonly ConcurrentDictionary<string, MonitoredImage> _images = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<MonitoredImage> GetAll() => _images.Values
        .OrderByDescending(image => image.CreatedUtc)
        .ToArray();

    public MonitoredImage? GetById(string id)
    {
        _images.TryGetValue(id, out var image);
        return image;
    }

    public MonitoredImage Upsert(string filePath, string imageUrl, string thumbnailUrl, DateTimeOffset createdUtc)
    {
        var fileName = Path.GetFileName(filePath);
        var id = CreateId(fileName);

        var image = new MonitoredImage(
            Id: id,
            FileName: fileName,
            DisplayName: Path.GetFileNameWithoutExtension(fileName),
            ImageUrl: imageUrl,
            ThumbnailUrl: thumbnailUrl,
            CreatedUtc: createdUtc);

        _images[id] = image;
        return image;
    }

    private static string CreateId(string fileName)
    {
        var slug = fileName.Trim().ToLowerInvariant();
        var normalized = new string(slug.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
        return string.Join('-', normalized.Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
