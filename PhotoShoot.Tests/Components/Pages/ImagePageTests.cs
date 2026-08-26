namespace PhotoShoot.Tests.Components.Pages;

public class ImagePageTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task MissingImagePage_renders_not_found_message()
    {
        var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/image/example");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(content.Contains("Image not found")).IsTrue();
    }
}
