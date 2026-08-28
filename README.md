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

### 2) Install Docker

[Install Docker Engine on Debian](https://docs.docker.com/engine/install/debian/)

> **Note:** .NET does not need to be installed separately. The Docker image bundles the runtime.

### 3) Install and configure FTP

Install `vsftpd`, then configure it to write incoming files to your chosen folder (for example `/home/ftpuser`).

### 4) Configure `docker-compose.yml`

Edit `docker-compose.yml` and update the bind mount under `volumes:` to point to your FTP drop folder:

```yaml
volumes:
  - /home/ftpuser:/data/images:ro   # ← set this to your FTP folder
```

Thumbnail and histogram folders are managed automatically as Docker named volumes — no need to create them manually.

You can also override any `ImageMonitor` setting via the `environment:` block using the `__` separator, for example:

```yaml
environment:
  - ImageMonitor__PollIntervalSeconds=10
```

### 5) Build and run with Docker

From the repo root:

```sh
docker compose up -d --build
```

To view logs:

```sh
docker compose logs -f
```

To stop:

```sh
docker compose down
```

Once running, new photos uploaded by the camera appear in the gallery automatically.
