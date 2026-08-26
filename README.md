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
