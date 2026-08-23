# PhotoShoot

PhotoShoot is a Blazor Server app that watches a local folder for new pictures, generates thumbnails, and updates the web UI in real time.

## Story

I bought a Canon Wireless File Transmitter (WFT-E7A):
https://www.usa.canon.com/support/p/wireless-file-transmitter-wft-e7a

I configured the camera transmitter to copy images to an FTP server running on Linux. Once files land on the Linux folder, PhotoShoot monitors that local folder and immediately processes each new image.

That means as photos are taken, they show up in the gallery automatically.

## How it works

1. Canon WFT uploads new images to a Linux FTP server folder.
2. PhotoShoot watches that local folder (no direct FTP client logic in the app).
3. The app creates thumbnails and catalogs the images.
4. Connected clients get live updates and can view the latest photos right away.
