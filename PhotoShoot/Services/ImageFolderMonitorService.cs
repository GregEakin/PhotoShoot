using System.Globalization;
using ImageMagick;
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
        Directory.CreateDirectory(_options.HistogramFolder);

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
        var histogramPath = await _thumbnailService.CreateHistogramAsync(filePath, cancellationToken);
        var relativeHistogramUrl = CombinePublicPath(_options.PublicHistogramPath, Path.GetFileName(histogramPath));
        var metadata = BuildMetadata(filePath);

        var image = _catalog.Upsert(filePath, relativeImageUrl, relativeThumbnailUrl, relativeHistogramUrl, metadata, fileInfo.LastWriteTimeUtc);
        _processedFiles[filePath] = signature;

        if (_initialScanComplete)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Notifying image added for {ImageId} ({ImageFile}) on instance {InstanceName}.",
                    image.Id,
                    fileInfo.Name,
                    Environment.MachineName);
            }

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

    private static string BuildMetadata(string filePath)
    {
        using var image = new MagickImage(filePath);

        var profile = image.GetExifProfile();
        if (profile == null)
        {
            return "Metadata not available";
        }

        // foreach (var value in profile.Values)
        // {
        //     Console.WriteLine("{0}({1}): {2}", value.Tag, value.DataType, FormatExifValue(value));
        // }

        // var stackCount = 30 images;

        var exposureValue = GetExifTagValue(profile, ExifTag.ExposureTime);
        var exposure = exposureValue is Rational exposureRational
            ? $"{exposureRational.Numerator}/{exposureRational.Denominator}"
            : image.GetAttribute("exif:ExposureTime") ?? "exp";

        var fNumberValue = GetExifTagValue(profile, ExifTag.FNumber);
        var fNumber = fNumberValue is Rational fNumRational && fNumRational.Denominator != 0
            ? ((double)fNumRational.Numerator / fNumRational.Denominator).ToString("0.###", CultureInfo.InvariantCulture)
            : image.GetAttribute("exif:FNumber") ?? "fnum";

        var isoValue = GetExifTagValue(profile, ExifTag.ISOSpeedRatings);
        var iso = isoValue switch
        {
            ushort isoUShort => isoUShort.ToString(CultureInfo.InvariantCulture),
            short isoShort => isoShort.ToString(CultureInfo.InvariantCulture),
            uint isoUInt => isoUInt.ToString(CultureInfo.InvariantCulture),
            int isoInt => isoInt.ToString(CultureInfo.InvariantCulture),
            ushort[] { Length: > 0 } isoUShorts => string.Join(", ", isoUShorts.Select(value => value.ToString(CultureInfo.InvariantCulture))),
            short[] { Length: > 0 } isoShorts => string.Join(", ", isoShorts.Select(value => value.ToString(CultureInfo.InvariantCulture))),
            uint[] { Length: > 0 } isoUInts => string.Join(", ", isoUInts.Select(value => value.ToString(CultureInfo.InvariantCulture))),
            int[] { Length: > 0 } isoInts => string.Join(", ", isoInts.Select(value => value.ToString(CultureInfo.InvariantCulture))),
            Rational isoRational when isoRational.Denominator != 0 => ((double)isoRational.Numerator / isoRational.Denominator).ToString("0.###", CultureInfo.InvariantCulture),
            _ => image.GetAttribute("exif:ISOSpeedRatings") ?? image.GetAttribute("exif:PhotographicSensitivity") ?? "iso"
        };

        var focalLengthValue = GetExifTagValue(profile, ExifTag.FocalLength);
        var focalLength = focalLengthValue is Rational focalLengthRational && focalLengthRational.Denominator != 0
            ? ((double)focalLengthRational.Numerator / focalLengthRational.Denominator).ToString("0.###", CultureInfo.InvariantCulture)
            : image.GetAttribute("exif:FocalLength") ?? "focal";

        var flashValue = GetExifTagValue(profile, ExifTag.Flash);
        var flash = flashValue is ushort flashShort
            ? flashShort.ToString(CultureInfo.InvariantCulture)
            : image.GetAttribute("exif:Flash");
        var flashSuffix = IsFlashFired(flash) ? "- w/ flash" : string.Empty;

        return $"ISO {iso} - {focalLength} mm - f/{fNumber} - {exposure} sec{flashSuffix}";
    }

    private static object? GetExifTagValue(IExifProfile profile, ExifTag tag)
    {
        var exifValue = profile.Values.FirstOrDefault(value => value.Tag.ToString() == tag.ToString());
        return exifValue?.GetValue();
    }

    private static string FormatExifValue(IExifValue value)
    {
        var rawValue = value.GetType().GetProperty("Value")?.GetValue(value);
        if (rawValue is null)
        {
            return string.Empty;
        }

        var dataType = value.DataType.ToString();
        switch (rawValue)
        {
            case Rational rational:
                return rational.Denominator != 0 
                    ? $"{rational.Numerator / rational.Denominator}" 
                    : $"{rational.Numerator}/{rational.Denominator}";
            case SignedRational signedRational:
                return signedRational.Denominator != 0
                    ? $"{(double)signedRational.Numerator / signedRational.Denominator}"
                    : $"{signedRational.Numerator}/{signedRational.Denominator}";
            case byte[] bytes when (dataType.Equals("Byte", StringComparison.OrdinalIgnoreCase) || dataType.Equals("Undefined", StringComparison.OrdinalIgnoreCase)):
                return bytes.Length > 16 
                    ? $"byte array {bytes.Length} bytes" 
                    : Convert.ToHexString(bytes);
            case Array values:
            {
                var parts = values.Cast<object?>().Select(item => item?.ToString() ?? string.Empty);
                return string.Join(", ", parts);
            }
            default:
                return rawValue.ToString() ?? string.Empty;
        }
    }

    private static bool IsFlashFired(string? flash)
    {
        if (string.IsNullOrWhiteSpace(flash))
        {
            return false;
        }

        if (flash.Contains("fired", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (int.TryParse(flash, NumberStyles.Integer, CultureInfo.InvariantCulture, out var flashCode))
        {
            return (flashCode & 0x01) != 0x00;
        }

        return false;
    }

    private readonly record struct FileSignature(long Length, DateTime LastWriteTimeUtc);
}
