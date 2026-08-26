using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PhotoShoot.Models;
using PhotoShoot.Services;

namespace PhotoShoot.Tests.Components.Pages;

public class ImageComponentTests
{
    [Test]
    public async Task ImageComponent_renders_not_found_when_catalog_has_no_image()
    {
        await using var context = new BunitContext();
        var notifications = new TestImageNotificationService();

        context.Services.AddSingleton<IImageCatalog>(new TestImageCatalog());
        context.Services.AddSingleton<IImageNotificationService>(notifications);

        var cut = context.Render<PhotoShoot.Components.Pages.Image>(parameters =>
            parameters.Add(component => component.Id, "missing"));

        await Assert.That(cut.Markup.Contains("Image not found")).IsTrue();
    }

    [Test]
    public async Task ImageComponent_renders_image_details_when_catalog_contains_id()
    {
        await using var context = new BunitContext();
        var notifications = new TestImageNotificationService();
        var catalog = new TestImageCatalog();

        var image = CreateImage("image-1", "first.jpg", "First");
        catalog.Add(image);

        context.Services.AddSingleton<IImageCatalog>(catalog);
        context.Services.AddSingleton<IImageNotificationService>(notifications);

        var cut = context.Render<PhotoShoot.Components.Pages.Image>(parameters =>
            parameters.Add(component => component.Id, "image-1"));

        await Assert.That(cut.Markup.Contains("First")).IsTrue();
        await Assert.That(cut.Markup.Contains("first.jpg")).IsTrue();
        await Assert.That(cut.Markup.Contains("/images/first.jpg")).IsTrue();
        await Assert.That(cut.Markup.Contains("/histograms/first-hist.png")).IsTrue();
    }

    [Test]
    public async Task ImageComponent_navigates_to_new_image_when_notification_is_raised()
    {
        await using var context = new BunitContext();
        var notifications = new TestImageNotificationService();
        var catalog = new TestImageCatalog();

        var currentImage = CreateImage("image-1", "first.jpg", "First");
        var newImage = CreateImage("image-2", "second.jpg", "Second");
        catalog.Add(currentImage);
        catalog.Add(newImage);

        context.Services.AddSingleton<IImageCatalog>(catalog);
        context.Services.AddSingleton<IImageNotificationService>(notifications);

        var cut = context.Render<PhotoShoot.Components.Pages.Image>(parameters =>
            parameters.Add(component => component.Id, "image-1"));

        var navigationManager = context.Services.GetRequiredService<NavigationManager>();

        notifications.RaiseImageAdded(newImage);

        await cut.WaitForAssertionAsync(() =>
        {
            if (!navigationManager.Uri.EndsWith("/image/image-2", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unexpected uri: {navigationManager.Uri}");
            }
        });

        await Assert.That(navigationManager.Uri.EndsWith("/image/image-2", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    private static MonitoredImage CreateImage(string id, string fileName, string displayName) =>
        new(
            Id: id,
            FileName: fileName,
            DisplayName: displayName,
            ImageUrl: $"/images/{fileName}",
            ThumbnailUrl: $"/thumbnails/{Path.GetFileNameWithoutExtension(fileName)}.webp",
            HistogramUrl: $"/histograms/{Path.GetFileNameWithoutExtension(fileName)}-hist.png",
            Metadata: "{}",
            CreatedUtc: DateTimeOffset.UtcNow);

    private sealed class TestImageCatalog : IImageCatalog
    {
        private readonly Dictionary<string, MonitoredImage> _images = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<MonitoredImage> GetAll() => _images.Values.ToArray();

        public MonitoredImage? GetById(string id) => _images.TryGetValue(id, out var image) ? image : null;

        public MonitoredImage Upsert(string filePath, string imageUrl, string thumbnailUrl, string histogramUrl, string metadata, DateTimeOffset createdUtc)
        {
            var fileName = Path.GetFileName(filePath);
            var image = new MonitoredImage(
                Id: Path.GetFileNameWithoutExtension(fileName),
                FileName: fileName,
                DisplayName: Path.GetFileNameWithoutExtension(fileName),
                ImageUrl: imageUrl,
                ThumbnailUrl: thumbnailUrl,
                HistogramUrl: histogramUrl,
                Metadata: metadata,
                CreatedUtc: createdUtc);

            _images[image.Id] = image;
            return image;
        }

        public void Add(MonitoredImage image)
        {
            _images[image.Id] = image;
        }
    }

    private sealed class TestImageNotificationService : IImageNotificationService
    {
        public event Action<MonitoredImage>? ImageAdded;

        public Task NotifyImageAddedAsync(MonitoredImage image, CancellationToken cancellationToken = default)
        {
            ImageAdded?.Invoke(image);
            return Task.CompletedTask;
        }

        public void RaiseImageAdded(MonitoredImage image)
        {
            ImageAdded?.Invoke(image);
        }
    }
}
