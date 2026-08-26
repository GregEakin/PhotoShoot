using ImageMagick;
using PhotoShoot.Options;
using PhotoShoot.Services;

namespace PhotoShoot.Tests.Services;

public class ImageThumbnailServiceTests
{
    [Test]
    public async Task CreateThumbnailAsync_creates_webp_thumbnail_in_configured_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"photoshoot-tests-{Guid.NewGuid():N}");
        var inputFolder = Path.Combine(root, "incoming");
        var thumbnailFolder = Path.Combine(root, "thumbnails");
        var histogramFolder = Path.Combine(root, "histograms");

        Directory.CreateDirectory(inputFolder);

        var sourceFilePath = Path.Combine(inputFolder, "sample.jpg");
        CreateSourceImage(sourceFilePath);

        var service = CreateService(thumbnailFolder, histogramFolder);

        try
        {
            var thumbnailPath = await service.CreateThumbnailAsync(sourceFilePath);

            await Assert.That(File.Exists(thumbnailPath)).IsTrue();
            await Assert.That(Path.GetExtension(thumbnailPath).Equals(".webp", StringComparison.OrdinalIgnoreCase)).IsTrue();
            await Assert.That(thumbnailPath.StartsWith(thumbnailFolder, StringComparison.OrdinalIgnoreCase)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Test]
    public async Task CreateThumbnailAsync_returns_existing_thumbnail_without_reading_source_file()
    {
        var root = Path.Combine(Path.GetTempPath(), $"photoshoot-tests-{Guid.NewGuid():N}");
        var thumbnailFolder = Path.Combine(root, "thumbnails");
        var histogramFolder = Path.Combine(root, "histograms");
        var sourceFilePath = Path.Combine(root, "missing-source.jpg");

        Directory.CreateDirectory(thumbnailFolder);

        var existingThumbnailPath = Path.Combine(thumbnailFolder, "missing-source.webp");
        await File.WriteAllTextAsync(existingThumbnailPath, "existing");

        var service = CreateService(thumbnailFolder, histogramFolder);

        try
        {
            var returnedPath = await service.CreateThumbnailAsync(sourceFilePath);

            await Assert.That(returnedPath == existingThumbnailPath).IsTrue();
            await Assert.That(File.Exists(existingThumbnailPath)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Test]
    public async Task CreateHistogramAsync_creates_histogram_in_configured_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"photoshoot-tests-{Guid.NewGuid():N}");
        var inputFolder = Path.Combine(root, "incoming");
        var thumbnailFolder = Path.Combine(root, "thumbnails");
        var histogramFolder = Path.Combine(root, "histograms");

        Directory.CreateDirectory(inputFolder);

        var sourceFilePath = Path.Combine(inputFolder, "sample.jpg");
        CreateSourceImage(sourceFilePath);

        var service = CreateService(thumbnailFolder, histogramFolder);

        try
        {
            var histogramPath = await service.CreateHistogramAsync(sourceFilePath);

            await Assert.That(File.Exists(histogramPath)).IsTrue();
            await Assert.That(Path.GetExtension(histogramPath).Equals(".png", StringComparison.OrdinalIgnoreCase)).IsTrue();
            await Assert.That(histogramPath.StartsWith(histogramFolder, StringComparison.OrdinalIgnoreCase)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ImageThumbnailService CreateService(string thumbnailFolder, string histogramFolder)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ImageMonitorOptions
        {
            ThumbnailFolder = thumbnailFolder,
            HistogramFolder = histogramFolder
        });

        return new ImageThumbnailService(options);
    }

    private static void CreateSourceImage(string path)
    {
        using var image = new MagickImage(MagickColors.Red, 800, 600);
        image.Format = MagickFormat.Jpeg;
        image.Write(path);
    }
}
