# RedScreen

A free Windows tray app that makes your screen warm, red and dim in the evening, like f.lux or CareUEyes.

Unlike Windows Night light, RedScreen lets you make your own modes and schedule them by the clock.

## Features

- **Modes:** named presets with a colour temperature (500 K, fully red, to 10 000 K) and a brightness level.
- **Automations:** switch to a mode at a set time on chosen weekdays, with an optional gradual transition that ends at that time.
- **Daytime:** a time window (07:00–18:00 by default) where the filter is off. It also turns the filter off in the morning if the PC was asleep or shut down.
- **Tray menu:** quick access to modes, colour temperature, brightness and language. The tray icon shows the current screen colour.
- **Multiple monitors** are supported.
- English and Norwegian.

RedScreen starts empty. Create your own modes and automations in the window.

## Installation

Download the latest version from the [Releases](https://github.com/DagF1nn/redscreen/releases) page.

Requires Windows 10 or 11.

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

Code signing is not set up yet. When it is, releases will be signed through SignPath Foundation.

- Committers and reviewers: [DagF1nn](https://github.com/DagF1nn)
- Approvers: [DagF1nn](https://github.com/DagF1nn)

### Privacy policy

This program will not transfer any information to other networked systems.

## License

[MIT](LICENSE)
