# PhotoShoot

PhotoShoot is a Blazor Server app that watches a local folder for new photos, generates thumbnails, and updates the web UI in real time.

It was built to make photos appear on-screen as soon as they are taken.

## Story

I bought a new toy:

- [Canon Wireless File Transmitter (WFT-E1A)](https://www.usa.canon.com/support/p/wireless-file-transmitter-wft-e1a)
- [Canon Wireless File Transmitter (WFT-E7A)](https://www.usa.canon.com/support/p/wireless-file-transmitter-wft-e7a)
- [Canon Wireless File Transmitter (WFT-E5A)](https://www.usa.canon.com/support/p/wireless-file-transmitter-wft-e5a)

The camera transmitter uploads images to an FTP server running on Linux. After files arrive in that Linux folder, PhotoShoot monitors the local folder and immediately processes each new image.

As photos are taken, they appear in the gallery automatically.

## How it works

1. Canon WFT uploads new images to a Linux FTP server folder.
2. PhotoShoot watches that local folder (no direct FTP client logic in the app).
3. The app creates thumbnails, histograms, and catalog entries for each image.
4. Connected clients receive live updates and can view the latest photos right away.

## Raspberry Pi setup (works great)

This app runs well on a [Raspberry Pi](https://www.raspberrypi.com/) using `Raspberry Pi OS Lite (64-bit)` (no desktop). That OS is Debian-based.

### 1) Install Git and clone the project

Install `git`, then clone this repository to your Pi.

### 2) Install .NET (ARM)

Use Microsoft guidance for ARM single-board computers:

[Deploy .NET apps on ARM single-board computers](https://learn.microsoft.com/en-us/dotnet/iot/deployment)

Install the latest LTS SDK/runtime with:

`curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel LTS`

### 3) Install and configure FTP

Install `vsftpd`, then configure it to write incoming files to your chosen folder (for example `/home/ftpuser`).

### 4) Create processing folders

Create folders for generated files, for example:

- `/home/greg/thumbnails`
- `/home/greg/histograms`

### 5) Update `PhotoShoot/appsettings.json`

Set the `ImageMonitor` paths to match your Pi:

- `InputFolder`: FTP drop folder
- `ThumbnailFolder`: thumbnail output folder
- `HistogramFolder`: histogram output folder

Current example:

- `InputFolder`: `/home/ftpuser`
- `ThumbnailFolder`: `/home/greg/thumbnails`
- `HistogramFolder`: `/home/greg/histograms`

### 6) Run the app

From the repo root:

`dotnet run -c Release`

Once running, new photos uploaded by the camera appear in the gallery automatically.
