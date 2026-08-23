using Microsoft.Extensions.Options;
using PhotoShoot.Options;

namespace PhotoShoot.Services;

public sealed class ImageFolderMonitorService : BackgroundService
{
    private readonly ILogger<ImageFolderMonitorService> _logger;
    private readonly ImageMonitorOptions _options;
    private readonly IImageCatalog _catalog;
    private readonly IImageThumbnailService _thumbnailService;
    private readonly IImageNotificationService _notificationService;
    private readonly Dictionary<string, FileSignature> _processedFiles = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialScanComplete;

    public ImageFolderMonitorService(
        IOptions<ImageMonitorOptions> options,
        IImageCatalog catalog,
        IImageThumbnailService thumbnailService,
        IImageNotificationService notificationService,
        ILogger<ImageFolderMonitorService> logger)
    {
        _options = options.Value;
        _catalog = catalog;
        _thumbnailService = thumbnailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_options.InputFolder);
        Directory.CreateDirectory(_options.ThumbnailFolder);

        await ScanFolderAsync(stoppingToken);
        _initialScanComplete = true;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ScanFolderAsync(stoppingToken);
        }
    }

    private async Task ScanFolderAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var filePath in Directory.EnumerateFiles(_options.InputFolder))
            {
                if (!IsSupportedImage(filePath))
                {
                    continue;
                }

                await ProcessFileAsync(filePath, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while scanning {Folder}.", _options.InputFolder);
        }
    }

    private async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            return;
        }

        var signature = new FileSignature(fileInfo.Length, fileInfo.LastWriteTimeUtc);
        if (_processedFiles.TryGetValue(filePath, out var existingSignature) && existingSignature == signature)
        {
            return;
        }

        if (!await WaitForReadableFileAsync(filePath, cancellationToken))
        {
            return;
        }

        var relativeImageUrl = CombinePublicPath(_options.PublicImagePath, Path.GetFileName(filePath));
        var thumbnailPath = await _thumbnailService.CreateThumbnailAsync(filePath, cancellationToken);
        var relativeThumbnailUrl = CombinePublicPath(_options.PublicThumbnailPath, Path.GetFileName(thumbnailPath));

        var image = _catalog.Upsert(filePath, relativeImageUrl, relativeThumbnailUrl, fileInfo.LastWriteTimeUtc);
        _processedFiles[filePath] = signature;

        if (_initialScanComplete)
        {
            await _notificationService.NotifyImageAddedAsync(image, cancellationToken);
        }
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Added image {ImageFile}.", fileInfo.Name);
    }

    private bool IsSupportedImage(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return _options.SupportedExtensions.Any(item => string.Equals(item, extension, StringComparison.OrdinalIgnoreCase));
    }

    private static string CombinePublicPath(string basePath, string fileName)
    {
        var trimmedBasePath = basePath.TrimEnd('/');
        return $"{trimmedBasePath}/{fileName}";
    }

    private static async Task<bool> WaitForReadableFileAsync(string filePath, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return stream.Length > 0;
            }
            catch (IOException)
            {
                await Task.Delay(250, cancellationToken);
            }
        }

        return false;
    }

    private readonly record struct FileSignature(long Length, DateTime LastWriteTimeUtc);
}
