namespace PhotoShoot.Options;

public sealed class ImageMonitorOptions
{
    public string InputFolder { get; set; } = @"C:\PhotoShoot\Incoming";

    public string ThumbnailFolder { get; set; } = @"C:\PhotoShoot\Incoming\thumbnails";

    public string PublicImagePath { get; set; } = "/images";

    public string PublicThumbnailPath { get; set; } = "/thumbnails";

    public int PollIntervalSeconds { get; set; } = 5;

    public string[] SupportedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"];
}
