using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoShoot.Hubs;
using PhotoShoot.Models;
using PhotoShoot.Services;

namespace PhotoShoot.Tests.Services;

public class ImageNotificationServiceTests
{
    [Test]
    public async Task NotifyImageAddedAsync_raises_image_added_event()
    {
        var hubContext = new TestHubContext();
        var service = new ImageNotificationService(hubContext, NullLogger<ImageNotificationService>.Instance);
        var image = CreateImage("img-1");

        MonitoredImage? raisedImage = null;
        var raisedCount = 0;
        service.ImageAdded += addedImage =>
        {
            raisedImage = addedImage;
            raisedCount++;
        };

        await service.NotifyImageAddedAsync(image);

        await Assert.That(raisedCount).IsEqualTo(1);
        await Assert.That(ReferenceEquals(raisedImage, image)).IsTrue();
    }

    [Test]
    public async Task NotifyImageAddedAsync_sends_signalr_message_to_all_clients()
    {
        var hubContext = new TestHubContext();
        var service = new ImageNotificationService(hubContext, NullLogger<ImageNotificationService>.Instance);
        var image = CreateImage("img-2");

        await service.NotifyImageAddedAsync(image);

        await Assert.That(hubContext.ClientsProxy.Calls.Count).IsEqualTo(1);

        var call = hubContext.ClientsProxy.Calls[0];
        await Assert.That(call.MethodName).IsEqualTo("ImageAdded");
        await Assert.That(call.Arguments.Length).IsEqualTo(1);
        await Assert.That(ReferenceEquals(call.Arguments[0], image)).IsTrue();
    }

    private static MonitoredImage CreateImage(string id) =>
        new(
            id,
            $"{id}.jpg",
            id,
            $"/images/{id}.jpg",
            $"/thumbnails/{id}.webp",
            $"/histograms/{id}-hist.png",
            "{}",
            DateTimeOffset.UtcNow);

    private sealed class TestHubContext : IHubContext<ImageHub>
    {
        private TestHubClients ClientsImpl { get; } = new();

        public TestClientProxy ClientsProxy => ClientsImpl.AllProxy;

        public IHubClients Clients => ClientsImpl;

        public IGroupManager Groups { get; } = new TestGroupManager();
    }

    private sealed class TestHubClients : IHubClients
    {
        public TestClientProxy AllProxy { get; } = new();

        public IClientProxy All => AllProxy;

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => AllProxy;

        public IClientProxy Client(string connectionId) => AllProxy;

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => AllProxy;

        public IClientProxy Group(string groupName) => AllProxy;

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => AllProxy;

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => AllProxy;

        public IClientProxy User(string userId) => AllProxy;

        public IClientProxy Users(IReadOnlyList<string> userIds) => AllProxy;
    }

    private sealed class TestClientProxy : IClientProxy
    {
        public List<HubCall> Calls { get; } = [];

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Calls.Add(new HubCall(method, args, cancellationToken));
            return Task.CompletedTask;
        }
    }

    private sealed class TestGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed record HubCall(string MethodName, object?[] Arguments, CancellationToken CancellationToken);
}
