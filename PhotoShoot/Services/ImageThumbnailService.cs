using Microsoft.Extensions.Options;
using PhotoShoot.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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

        using var image = await Image.LoadAsync(sourceFilePath, cancellationToken);
        image.Mutate(context => context
            .AutoOrient()
            .Resize(new ResizeOptions
            {
                Size = new Size(320, 320),
                Mode = ResizeMode.Max
            }));

        await image.SaveAsJpegAsync(thumbnailPath, cancellationToken);

        return thumbnailPath;
    }
}
