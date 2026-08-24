using ImageMagick;
using ImageMagick.Formats;
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

    public async Task<string> CreateThumbnailAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_options.ThumbnailFolder);

        var thumbnailFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".webp";
        var thumbnailPath = Path.Combine(_options.ThumbnailFolder, thumbnailFileName);

        if (File.Exists(thumbnailPath))
        {
            return thumbnailPath;
        }

        var defines = new WebPWriteDefines { AutoFilter = true, ThreadLevel = true };
        using var image = new MagickImage(sourceFilePath);
        image.Format = MagickFormat.WebP;
        image.Quality = 85;
        image.AutoOrient();
        image.Thumbnail(new MagickGeometry(320, 320) { IgnoreAspectRatio = false });
        image.Strip();

        await image.WriteAsync(thumbnailPath, defines, cancellationToken);

        return thumbnailPath;
    }

    public Task<string> CreateHistogramAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_options.HistogramFolder);

        var histogramFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + "-hist.png";
        var histogramPath = Path.Combine(_options.HistogramFolder, histogramFileName);

        if (File.Exists(histogramPath))
        {
            return Task.FromResult(histogramPath);
        }

        using var sourceImage = new MagickImage(sourceFilePath);
        sourceImage.AutoOrient();
        sourceImage.Write($"histogram:{histogramPath}");

        return Task.FromResult(histogramPath);
    }
}
