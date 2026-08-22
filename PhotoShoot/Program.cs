using Microsoft.Extensions.FileProviders;
using PhotoShoot.Components;
using PhotoShoot.Hubs;
using PhotoShoot.Options;
using PhotoShoot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.Configure<ImageMonitorOptions>(builder.Configuration.GetSection("ImageMonitor"));
builder.Services.AddSingleton<IImageCatalog, InMemoryImageCatalog>();
builder.Services.AddSingleton<IImageThumbnailService, ImageThumbnailService>();
builder.Services.AddSingleton<IImageNotificationService, ImageNotificationService>();
builder.Services.AddHostedService<ImageFolderMonitorService>();

var imageMonitorOptions = builder.Configuration.GetSection("ImageMonitor").Get<ImageMonitorOptions>() ?? new ImageMonitorOptions();
Directory.CreateDirectory(imageMonitorOptions.InputFolder);
Directory.CreateDirectory(imageMonitorOptions.ThumbnailFolder);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageMonitorOptions.InputFolder),
    RequestPath = imageMonitorOptions.PublicImagePath
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageMonitorOptions.ThumbnailFolder),
    RequestPath = imageMonitorOptions.PublicThumbnailPath
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHub<ImageHub>("/imageHub");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
