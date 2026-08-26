using PhotoShoot.Services;

namespace PhotoShoot.Tests.Services;

public class InMemoryImageCatalogTests
{
    [Test]
    public async Task Upsert_creates_expected_image_and_can_be_fetched_by_id()
    {
        var catalog = new InMemoryImageCatalog();
        var createdUtc = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var image = catalog.Upsert(
            filePath: @"C:\photos\My Summer Photo 01.JPG",
            imageUrl: "/images/My%20Summer%20Photo%2001.JPG",
            thumbnailUrl: "/thumbnails/my-summer-photo-01.webp",
            histogramUrl: "/histograms/my-summer-photo-01-hist.png",
            metadata: "{\"camera\":\"Canon\"}",
            createdUtc: createdUtc);

        var fetched = catalog.GetById("my-summer-photo-01-jpg");

        await Assert.That(image.Id).IsEqualTo("my-summer-photo-01-jpg");
        await Assert.That(image.FileName).IsEqualTo("My Summer Photo 01.JPG");
        await Assert.That(image.DisplayName).IsEqualTo("My Summer Photo 01");
        await Assert.That(image.CreatedUtc).IsEqualTo(createdUtc);
        await Assert.That(fetched).IsNotNull();
        await Assert.That(fetched!.Id).IsEqualTo(image.Id);
    }

    [Test]
    public async Task Upsert_with_same_slug_replaces_existing_entry()
    {
        var catalog = new InMemoryImageCatalog();
        var older = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var newer = older.AddMinutes(5);

        catalog.Upsert(
            filePath: @"C:\incoming\party-shot.JPG",
            imageUrl: "/images/party-shot.JPG",
            thumbnailUrl: "/thumbnails/party-shot.webp",
            histogramUrl: "/histograms/party-shot-hist.png",
            metadata: "{\"v\":1}",
            createdUtc: older);

        var updated = catalog.Upsert(
            filePath: @"C:\incoming\Party Shot.jpg",
            imageUrl: "/images/Party%20Shot.jpg",
            thumbnailUrl: "/thumbnails/party-shot.webp",
            histogramUrl: "/histograms/party-shot-hist.png",
            metadata: "{\"v\":2}",
            createdUtc: newer);

        var all = catalog.GetAll();

        await Assert.That(all.Count).IsEqualTo(1);
        await Assert.That(updated.Id).IsEqualTo("party-shot-jpg");
        await Assert.That(updated.Metadata).IsEqualTo("{\"v\":2}");
        await Assert.That(updated.CreatedUtc).IsEqualTo(newer);
    }

    [Test]
    public async Task GetAll_returns_images_sorted_by_createdutc_descending()
    {
        var catalog = new InMemoryImageCatalog();
        var t1 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = t1.AddMinutes(10);
        var t3 = t1.AddMinutes(20);

        catalog.Upsert("a.jpg", "/images/a.jpg", "/thumbnails/a.webp", "/histograms/a-hist.png", "{}", t1);
        catalog.Upsert("b.jpg", "/images/b.jpg", "/thumbnails/b.webp", "/histograms/b-hist.png", "{}", t3);
        catalog.Upsert("c.jpg", "/images/c.jpg", "/thumbnails/c.webp", "/histograms/c-hist.png", "{}", t2);

        var all = catalog.GetAll().ToArray();

        await Assert.That(all[0].FileName).IsEqualTo("b.jpg");
        await Assert.That(all[1].FileName).IsEqualTo("c.jpg");
        await Assert.That(all[2].FileName).IsEqualTo("a.jpg");
    }

    [Test]
    public async Task GetById_returns_null_when_missing()
    {
        var catalog = new InMemoryImageCatalog();

        var result = catalog.GetById("does-not-exist");

        await Assert.That(result).IsNull();
    }
}
