# RedScreen

A free Windows tray app that makes your screen warm, red and dim in the evening.

Unlike Windows Night light, RedScreen lets you make your own modes and schedule them by the clock.

## Features

- **Modes:** named presets with a colour temperature (500 K, fully red, to 10 000 K) and a brightness level.
- **Automations:** switch to a mode at a set time on chosen weekdays, with an optional gradual transition that ends at that time.
- **Daytime:** a time window (07:00–18:00 by default) where the filter is off. It also turns the filter off in the morning if the PC was asleep or shut down.
- **Tray menu:** quick access to modes, colour temperature, brightness, language and starting with Windows. The tray icon shows the current screen colour.
- **Multiple monitors** are supported.
- English and Norwegian.

RedScreen starts empty. Create your own modes and automations in the window.

## Installation

Download the latest `RedScreen-<version>-win-x64.zip` from the [Releases](https://github.com/DagF1nn/redscreen/releases) page, unzip it and run `RedScreen.exe`. Nothing else needs to be installed.

Requires Windows 10 or 11 (64-bit).

RedScreen is not code-signed yet, so Windows may show "Windows protected your PC". Click **More info** and then **Run anyway**. On PCs with Smart App Control turned on, Windows may block it entirely until signing is in place.

RedScreen starts with Windows by default. You can turn this off with **Start with Windows** in the tray menu.

## Known limitations

- RedScreen changes the graphics card's gamma table. Some games, HDR and drivers reset this table, for example when entering or leaving exclusive fullscreen. RedScreen detects this and restores the colour within about a quarter of a second, so you may see a short flash.
- Brightness is reduced through the gamma table, not the monitor's own backlight.

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build
```

Settings are stored in `%APPDATA%\RedScreen\settings.json`.

## Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org) (application pending).

- The official repository is [github.com/DagF1nn/redscreen](https://github.com/DagF1nn/redscreen).
- Only `RedScreen.exe` in the release zip files is signed. It is built from this repository by the [release workflow](.github/workflows/release.yml) on GitHub Actions. Nothing built elsewhere is signed.
- Every signing request is approved by hand.

Team roles:

- Committers and reviewers: [DagF1nn](https://github.com/DagF1nn)
- Approvers: [DagF1nn](https://github.com/DagF1nn)

### Privacy policy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

RedScreen does not connect to the internet. Settings are stored only on your own PC, in `%APPDATA%\RedScreen\settings.json`.

## License

[MIT](LICENSE)
