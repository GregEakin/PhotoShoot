using ImageMagick;
using ImageMagick.Drawing;
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

        var thumbnailFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".jpg";
        var thumbnailPath = Path.Combine(_options.ThumbnailFolder, thumbnailFileName);

        using var image = new MagickImage(sourceFilePath);
        image.AutoOrient();
        image.Thumbnail(new MagickGeometry(320, 320) { IgnoreAspectRatio = false });
        image.Format = MagickFormat.Jpeg;
        image.Quality = 85;

        await image.WriteAsync(thumbnailPath, cancellationToken);

        return thumbnailPath;
    }

    public async Task<string> CreateHistogramAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_options.HistogramFolder);

        var histogramFileName = Path.GetFileNameWithoutExtension(sourceFilePath) + "-hist.png";
        var histogramPath = Path.Combine(_options.HistogramFolder, histogramFileName);

        using var sourceImage = new MagickImage(sourceFilePath);
        sourceImage.AutoOrient();

        const int width = 256;
        const int height = 180;
        const int channelHeight = 56;
        var channelStride = channelHeight + 4;

        var red = new double[width];
        var green = new double[width];
        var blue = new double[width];

        foreach (var entry in sourceImage.Histogram())
        {
            var color = entry.Key;
            var count = entry.Value;
            red[ToBucket(color.R)] += count;
            green[ToBucket(color.G)] += count;
            blue[ToBucket(color.B)] += count;
        }

        var maxCount = Math.Max(red.Max(), Math.Max(green.Max(), blue.Max()));
        if (maxCount <= 0)
        {
            maxCount = 1;
        }

        using var histogramImage = new MagickImage(MagickColors.Black, width, height);

        DrawChannel(histogramImage, red, MagickColors.Red, 2, channelHeight, maxCount);
        DrawChannel(histogramImage, green, MagickColors.Lime, 2 + channelStride, channelHeight, maxCount);
        DrawChannel(histogramImage, blue, MagickColors.DodgerBlue, 2 + (channelStride * 2), channelHeight, maxCount);

        histogramImage.Format = MagickFormat.Png;
        await histogramImage.WriteAsync(histogramPath, cancellationToken);

        return histogramPath;
    }

    private static int ToBucket(ushort channel)
    {
        return (int)Math.Round(channel * 255.0 / Quantum.Max);
    }

    private static void DrawChannel(MagickImage canvas, double[] values, MagickColor color, int yOffset, int channelHeight, double maxCount)
    {
        for (var x = 0; x < values.Length; x++)
        {
            var barHeight = (int)Math.Round((values[x] / maxCount) * channelHeight);
            if (barHeight <= 0)
            {
                continue;
            }

            var y1 = yOffset + channelHeight;
            var y2 = y1 - barHeight;

            canvas.Draw(
                new DrawableStrokeColor(color),
                new DrawableStrokeWidth(1),
                new DrawableLine(x, y1, x, y2));
        }
    }
}
