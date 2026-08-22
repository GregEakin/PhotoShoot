using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Options;
using PhotoShoot.Options;

namespace PhotoShoot.Services;

public sealed class ImageThumbnailService : IImageThumbnailService
{
    private readonly ImageMonitorOptions _options;

    public ImageThumbnailService(IOptions<ImageMonitorOptions> options)
    {
        _options = options.Value;
    }

    public Task<string> CreateThumbnailAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_options.ThumbnailFolder);

        var thumbnailFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".jpg";
        var thumbnailPath = Path.Combine(_options.ThumbnailFolder, thumbnailFileName);

        using var sourceImage = Image.FromFile(sourceFilePath);
        using var thumbnailImage = CreateThumbnail(sourceImage, 320, 320);
        thumbnailImage.Save(thumbnailPath, ImageFormat.Jpeg);

        return Task.FromResult(thumbnailPath);
    }

    private static Bitmap CreateThumbnail(Image sourceImage, int maxWidth, int maxHeight)
    {
        var ratio = Math.Min((double)maxWidth / sourceImage.Width, (double)maxHeight / sourceImage.Height);
        var width = Math.Max(1, (int)Math.Round(sourceImage.Width * ratio));
        var height = Math.Max(1, (int)Math.Round(sourceImage.Height * ratio));

        var thumbnail = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(thumbnail);
        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        graphics.DrawImage(sourceImage, 0, 0, width, height);

        return thumbnail;
    }
}
