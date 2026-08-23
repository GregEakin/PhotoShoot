namespace PhotoShoot.Services;

public interface IImageThumbnailService
{
    Task<string> CreateThumbnailAsync(string sourceFilePath, CancellationToken cancellationToken = default);

    Task<string> CreateHistogramAsync(string sourceFilePath, CancellationToken cancellationToken = default);
}
