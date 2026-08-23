namespace PhotoShoot.Models;

public sealed record MonitoredImage(
    string Id,
    string FileName,
    string DisplayName,
    string ImageUrl,
    string ThumbnailUrl,
    string HistogramUrl,
    string Metadata,
    DateTimeOffset CreatedUtc);
