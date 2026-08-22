# PhotoShoot Implementation Plan

## Overview

This project will watch a local folder for newly added images, generate thumbnails, expose the images to the web app, and notify connected Blazor clients in real time when new content arrives.

The FTP transfer is handled outside the application. The app only needs to monitor the local landing folder where new image files appear.

## Ten-Step Design

### 1. Keep the app on Blazor Server
Use the existing interactive Blazor app as the main UI so the server can push live updates to connected clients.

Relevant files:
- `PhotoShoot/Program.cs`
- `PhotoShoot/Components/*`

### 2. Add configuration for the image folders
Define settings for the monitored folder, thumbnail folder, supported file types, and refresh interval.

Suggested files:
- `PhotoShoot/appsettings.json`
- `PhotoShoot/appsettings.Development.json`
- `PhotoShoot/Options/ImageMonitorOptions.cs`

### 3. Add a background service to monitor the local folder
Create a hosted service that watches the landing folder for new image files and reacts when files are added or changed.

Suggested files:
- `PhotoShoot/Services/ImageFolderWatcherService.cs`

### 4. Validate new image files before processing
Check that each file is a supported image format, fully written, and not a duplicate before it is added to the catalog.

Suggested files:
- `PhotoShoot/Services/ImageFileValidator.cs`

### 5. Generate thumbnails for quick viewing
When a new image is detected, create a thumbnail copy and store it in a separate folder for faster gallery rendering.

Suggested files:
- `PhotoShoot/Services/ThumbnailService.cs`

### 6. Add an image catalog service
Track metadata for discovered images so the UI can render a gallery and detail pages consistently.

Suggested files:
- `PhotoShoot/Services/IImageCatalog.cs`
- `PhotoShoot/Services/ImageCatalog.cs`

### 7. Serve the image and thumbnail folders as static content
Expose the local image folders through the web app so browsers can load the originals and thumbnails directly.

Relevant files:
- `PhotoShoot/Program.cs`

### 8. Add a SignalR hub for live notifications
Broadcast a message when new images arrive so connected clients can refresh or navigate automatically.

Suggested files:
- `PhotoShoot/Hubs/ImageHub.cs`
- `PhotoShoot/Services/IImageNotificationService.cs`
- `PhotoShoot/Services/ImageNotificationService.cs`

### 9. Build gallery and image detail pages in Blazor
Show the current image set in a gallery and provide a dedicated page for individual image viewing.

Suggested files:
- `PhotoShoot/Components/Pages/Home.razor`
- `PhotoShoot/Components/Pages/Image.razor`

### 10. Add tests for startup, routing, and image workflow behavior
Verify the app starts, routes resolve, and the image-processing workflow behaves as expected.

Suggested files:
- `PhotoShoot.Tests/Tests.cs`
- `PhotoShoot.Tests/WebApplicationFactory.cs`

## Notes

- Prefer Blazor Server behavior for real-time updates.
- Keep the implementation minimal and focused on the local folder workflow.
- If persistence is needed later, the image catalog can be moved from in-memory storage to a database without changing the UI contract.
