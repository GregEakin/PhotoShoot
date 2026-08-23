namespace PhotoShoot.Options;

public sealed class ImageMonitorOptions
{
    public string InputFolder { get; init; } = @"C:\PhotoShoot\Incoming";

    public string ThumbnailFolder { get; init; } = @"C:\PhotoShoot\Incoming\thumbnails";

    public string PublicImagePath { get; init; } = "/images";

    public string PublicThumbnailPath { get; init; } = "/thumbnails";

    public int PollIntervalSeconds { get; init; } = 5;

    public string[] SupportedExtensions { get; init; } = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"];
}
