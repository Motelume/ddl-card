# Android / ColorOS 16 user guide

## Install

1. Copy `DDLCard-Android-v0.1.0.apk` to the phone and open it.
2. If ColorOS asks, allow installation from the file manager used to open the APK.
3. Open DDLCard once, then allow notifications and precise alarms from the prompt at the top.
4. In ColorOS battery/background settings, allow DDLCard to run in the background so reminder delivery is not delayed.

This first public build uses a development signing key. A later store/release build must use a permanent release key; installing that future build may require uninstalling this one first.

## Add the desktop card

Long-press an empty area of the home screen, choose **Widgets**, find **DDLCard**, then add either:

- **4x4:** the nearest 3 active deadlines;
- **4x6:** the nearest 6 active deadlines.

The launcher controls widget placement and size. Android does not allow an ordinary widget to hide outside the screen edge, so the optional edge-collapse behavior exists only on the Windows floating card.

## Presets and backup

Open **Preset and backup** in the app header.

- **Preset code** copies default reminder settings and compatible appearance fields, but never tasks.
- **Export backup** creates a JSON file containing local tasks and settings.
- **Restore backup** previews the task count and asks before replacing phone data.

The Windows and Android apps never sync automatically. You may move a JSON backup manually if you deliberately want the same data on another device.
