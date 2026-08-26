namespace PhotoShoot.Tests.Components.Pages;

public class HomePageTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task HomePage_renders_gallery()
    {
        var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(content.Contains("PhotoShoot Gallery")).IsTrue();
    }
}
