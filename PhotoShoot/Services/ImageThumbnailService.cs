using System.Collections.Concurrent;
using ImageMagick;
using ImageMagick.Formats;
using Microsoft.Extensions.Options;
using PhotoShoot.Options;

namespace PhotoShoot.Services;

public sealed class ImageThumbnailService : IImageThumbnailService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> OutputLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly ImageMonitorOptions _options;

    public ImageThumbnailService(IOptions<ImageMonitorOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> CreateThumbnailAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_options.ThumbnailFolder);

        var sourceFileInfo = new FileInfo(sourceFilePath);
        var thumbnailFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".webp";
        var thumbnailPath = Path.Combine(_options.ThumbnailFolder, thumbnailFileName);
        var thumbnailLock = OutputLocks.GetOrAdd(thumbnailPath, static _ => new SemaphoreSlim(1, 1));

        await thumbnailLock.WaitAsync(cancellationToken);
        try
        {
            if (IsOutputUpToDate(thumbnailPath, sourceFileInfo.LastWriteTimeUtc))
            {
                return thumbnailPath;
            }

            var defines = new WebPWriteDefines { AutoFilter = true, ThreadLevel = true };
            var tempThumbnailPath = Path.Combine(_options.ThumbnailFolder, $".{thumbnailFileName}.{Guid.NewGuid():N}.tmp");

            try
            {
                using var image = new MagickImage(sourceFilePath);
                image.Format = MagickFormat.WebP;
                image.Quality = 85;
                image.AutoOrient();
                image.Thumbnail(new MagickGeometry(320, 320) { IgnoreAspectRatio = false });
                image.Strip();

                await image.WriteAsync(tempThumbnailPath, defines, cancellationToken);
                File.Move(tempThumbnailPath, thumbnailPath, true);
            }
            catch
            {
                if (File.Exists(tempThumbnailPath))
                {
                    File.Delete(tempThumbnailPath);
                }

                throw;
            }

            return thumbnailPath;
        }
        finally
        {
            thumbnailLock.Release();
        }
    }

    public async Task<string> CreateHistogramAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_options.HistogramFolder);

        var sourceFileInfo = new FileInfo(sourceFilePath);
        var histogramFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + "-hist.png";
        var histogramPath = Path.Combine(_options.HistogramFolder, histogramFileName);
        var histogramLock = OutputLocks.GetOrAdd(histogramPath, static _ => new SemaphoreSlim(1, 1));

        await histogramLock.WaitAsync(cancellationToken);
        try
        {
            if (IsOutputUpToDate(histogramPath, sourceFileInfo.LastWriteTimeUtc))
            {
                return histogramPath;
            }

            var tempHistogramPath = Path.Combine(_options.HistogramFolder, $".{histogramFileName}.{Guid.NewGuid():N}.tmp");
            try
            {
                using var sourceImage = new MagickImage(sourceFilePath);
                sourceImage.AutoOrient();
                sourceImage.Write($"histogram:{tempHistogramPath}");
                File.Move(tempHistogramPath, histogramPath, true);
            }
            catch
            {
                if (File.Exists(tempHistogramPath))
                {
                    File.Delete(tempHistogramPath);
                }

                throw;
            }

            return histogramPath;
        }
        finally
        {
            histogramLock.Release();
        }
    }

    private static bool IsOutputUpToDate(string outputPath, DateTime sourceLastWriteTimeUtc)
    {
        if (!File.Exists(outputPath))
        {
            return false;
        }

        var outputLastWriteTimeUtc = File.GetLastWriteTimeUtc(outputPath);
        return outputLastWriteTimeUtc >= sourceLastWriteTimeUtc;
    }
}
