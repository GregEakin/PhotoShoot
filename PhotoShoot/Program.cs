using Microsoft.AspNetCore.Components.Server.Circuits;
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
builder.Services.AddScoped<CircuitHandler, CircuitLoggingHandler>();
builder.Services.AddHostedService<ImageFolderMonitorService>();

var imageMonitorOptions = builder.Configuration.GetSection("ImageMonitor").Get<ImageMonitorOptions>() ?? new ImageMonitorOptions();
ValidateConfiguredPathForLinux(imageMonitorOptions.InputFolder, "InputFolder");
ValidateConfiguredPathForLinux(imageMonitorOptions.ThumbnailFolder, "ThumbnailFolder");
ValidateConfiguredPathForLinux(imageMonitorOptions.HistogramFolder, "HistogramFolder");

var inputFolder = Path.GetFullPath(imageMonitorOptions.InputFolder);
var thumbnailFolder = Path.GetFullPath(imageMonitorOptions.ThumbnailFolder);
var histogramFolder = Path.GetFullPath(imageMonitorOptions.HistogramFolder);
Directory.CreateDirectory(inputFolder);
Directory.CreateDirectory(thumbnailFolder);
Directory.CreateDirectory(histogramFolder);

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
    FileProvider = new PhysicalFileProvider(inputFolder),
    RequestPath = imageMonitorOptions.PublicImagePath
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(thumbnailFolder),
    RequestPath = imageMonitorOptions.PublicThumbnailPath
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(histogramFolder),
    RequestPath = imageMonitorOptions.PublicHistogramPath
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHub<ImageHub>("/imageHub");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (app.Logger.IsEnabled(LogLevel.Information))
    app.Logger.LogInformation("Image monitor watching folder: {InputFolder} (thumbnails: {ThumbnailFolder}, histograms: {HistogramFolder})", inputFolder, thumbnailFolder, histogramFolder);

app.Run();

static void ValidateConfiguredPathForLinux(string configuredPath, string settingName)
{
    if (!OperatingSystem.IsLinux())
    {
        return;
    }

    if (!LooksLikeWindowsPath(configuredPath))
    {
        return;
    }

    throw new InvalidOperationException(
        $"ImageMonitor:{settingName} is set to '{configuredPath}', which looks like a Windows-style path. " +
        "Use a Linux path such as '/home/ftpuser' or a relative path such as 'incoming'.");
}

static bool LooksLikeWindowsPath(string path)
{
    return path.Length >= 2
        && char.IsLetter(path[0])
        && path[1] == ':';
}
