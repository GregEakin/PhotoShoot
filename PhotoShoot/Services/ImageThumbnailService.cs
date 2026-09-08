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

        var thumbnailFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".webp";
        var thumbnailPath = Path.Combine(_options.ThumbnailFolder, thumbnailFileName);
        if (CanReuseOutput(thumbnailPath, sourceFilePath))
        {
            return thumbnailPath;
        }

        var thumbnailLock = OutputLocks.GetOrAdd(thumbnailPath, static _ => new SemaphoreSlim(1, 1));

        await thumbnailLock.WaitAsync(cancellationToken);
        try
        {
            if (CanReuseOutput(thumbnailPath, sourceFilePath))
            {
                return thumbnailPath;
            }

            await EnsureSourceFileStableAsync(sourceFilePath, cancellationToken);

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
                File.Delete(tempThumbnailPath);
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

        var histogramFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + "-hist.png";
        var histogramPath = Path.Combine(_options.HistogramFolder, histogramFileName);
        if (CanReuseOutput(histogramPath, sourceFilePath))
        {
            return histogramPath;
        }

        var histogramLock = OutputLocks.GetOrAdd(histogramPath, static _ => new SemaphoreSlim(1, 1));

        await histogramLock.WaitAsync(cancellationToken);
        try
        {
            if (CanReuseOutput(histogramPath, sourceFilePath))
            {
                return histogramPath;
            }

            await EnsureSourceFileStableAsync(sourceFilePath, cancellationToken);

            var tempHistogramFileName = $".{Path.GetFileNameWithoutExtension(histogramFileName)}.{Guid.NewGuid():N}.png";
            var tempHistogramPath = Path.Combine(_options.HistogramFolder, tempHistogramFileName);
            try
            {
                using var sourceImage = new MagickImage(sourceFilePath);
                sourceImage.AutoOrient();
                sourceImage.Write($"histogram:{tempHistogramPath}");
                File.Move(tempHistogramPath, histogramPath, true);
            }
            catch
            {
                File.Delete(tempHistogramPath);
                throw;
            }

            return histogramPath;
        }
        finally
        {
            histogramLock.Release();
        }
    }

    private static bool CanReuseOutput(string outputPath, string sourceFilePath)
    {
        if (!File.Exists(outputPath))
        {
            return false;
        }

        if (!File.Exists(sourceFilePath))
        {
            return true;
        }

        var sourceLastWriteTimeUtc = File.GetLastWriteTimeUtc(sourceFilePath);
        return IsOutputUpToDate(outputPath, sourceLastWriteTimeUtc);
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

    private static async Task EnsureSourceFileStableAsync(string sourceFilePath, CancellationToken cancellationToken)
    {
        const int maxAttempts = 20;
        const int stableDelayMs = 300;

        var previousLength = -1L;
        var previousLastWriteTimeUtc = DateTime.MinValue;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}", sourceFilePath);
            }

            var fileInfo = new FileInfo(sourceFilePath);
            fileInfo.Refresh();

            var currentLength = fileInfo.Length;
            var currentLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;

            var canOpenForRead = false;
            try
            {
                await using var stream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                canOpenForRead = true;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            if (canOpenForRead &&
                currentLength > 0 &&
                currentLength == previousLength &&
                currentLastWriteTimeUtc == previousLastWriteTimeUtc)
            {
                return;
            }

            previousLength = currentLength;
            previousLastWriteTimeUtc = currentLastWriteTimeUtc;

            await Task.Delay(stableDelayMs, cancellationToken);
        }

        throw new IOException($"Source file did not become stable in time: {sourceFilePath}");
    }
}
