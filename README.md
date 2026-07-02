# Material 3 for .NET

[![Material3.Core](https://img.shields.io/nuget/v/Material3.Core?logo=nuget&label=Material3.Core)](https://www.nuget.org/packages/Material3.Core)
[![Material3.WinForms](https://img.shields.io/nuget/v/Material3.WinForms?logo=nuget&label=Material3.WinForms)](https://www.nuget.org/packages/Material3.WinForms)
[![Material3.Wpf](https://img.shields.io/nuget/v/Material3.Wpf?logo=nuget&label=Material3.Wpf)](https://www.nuget.org/packages/Material3.Wpf)

[![Download WinForms gallery](https://img.shields.io/badge/Download-WinForms%20gallery-2962FF?logo=windows&logoColor=white)](https://github.com/dary1337/material3-dotnet/releases/latest/download/Material3.WinForms.Gallery.exe)
[![Download WPF gallery](https://img.shields.io/badge/Download-WPF%20gallery-7C4DFF?logo=windows&logoColor=white)](https://github.com/dary1337/material3-dotnet/releases/latest/download/Material3.Wpf.Gallery.exe)
[![GitHub stars](https://img.shields.io/github/stars/dary1337/material3-dotnet?style=social)](https://github.com/dary1337/material3-dotnet)

**Material 3 (Material You) for .NET desktop — WinForms & WPF.** Dynamic color from a single seed,
runtime light/dark switching, the full M3 type scale, elevation, state layers and motion. One shared
HCT engine (`Material3.Core`), two UI stacks. No native dependencies.

<p align="center">
  <img src="https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/dynamic-color.gif" alt="Dragging the seed hue recolors the whole palette live in the Material 3 gallery" width="820">
</p>
<p align="center"><sub>Drag the seed hue — the entire palette recolors live through the HCT pipeline.</sub></p>

Existing "Material for WinForms" libraries implement Material **2** and are largely unmaintained.
This project targets the current spec: HCT tonal palettes, color roles, surface containers and
the 2023+ component look.

> Status (0.7): **WinForms** — foundation, component catalog and designer support complete.
> **WPF** — the shared HCT engine, live dynamic-color theming, `M3Icon`, motion helpers, and a core set of
> M3 controls (filled/tonal/outlined/text buttons, text field, chips, menus, tooltip, scrollbar, progress,
> card, expander) all ship and work; the control suite is still growing toward WinForms parity
> (selection, navigation and overlay components come next).

## Packages

One HCT engine, three packages — a shared `Core` plus two UI stacks:

| Package | Target | Status |
|---|---|---|
| `Material3.Core` | netstandard2.0 | HCT/CAM16 engine, tonal palettes, color roles, `MaterialTheme` — platform-neutral `Argb`, no UI deps |
| `Material3.WinForms` | net472 / net8.0-windows | Full M3 control catalog + Visual Studio designer support — **preview, usable** |
| `Material3.Wpf` | net472 / net8.0-windows | Dynamic-color `M3Theme` (live `DynamicResource` brushes from one seed), `M3Icon`, motion helpers, plus a core M3 control set (see [coverage](docs/wpf.md#control-coverage)) — **usable; growing toward WinForms parity** |

Two demo galleries are published with each release: **`Material3.WinForms.Gallery`** (WinForms) and **`Material3.Wpf.Gallery`** (WPF).

## Documentation

- **[Material3.WinForms →](docs/winforms.md)** — controls, quickstart, designer support, high-DPI.
- **[Material3.Wpf →](docs/wpf.md)** — dynamic-color theming, controls, WPF ↔ WinForms coverage.
- **[Material3.Core](https://www.nuget.org/packages/Material3.Core)** — the platform-neutral HCT engine (`Argb`, netstandard2.0).

## Screenshots

The same gallery pages in **light and dark** — every control follows the active scheme.
Rows: color roles · buttons & FAB · selection · cards & lists · overlays & pickers.

| Light | Dark |
|:---:|:---:|
| ![Color roles, light theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/color-roles-light.webp) | ![Color roles, dark theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/color-roles-dark.webp) |
| ![Buttons & FAB, light theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/buttons-light.webp) | ![Buttons & FAB, dark theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/buttons-dark.webp) |
| ![Selection controls, light theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/selection-light.webp) | ![Selection controls, dark theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/selection-dark.webp) |
| ![Cards & lists, light theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/cards-lists-light.webp) | ![Cards & lists, dark theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/cards-lists-dark.webp) |
| ![Overlays & pickers, light theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/overlays-light.webp) | ![Overlays & pickers, dark theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/overlays-dark.webp) |

<sub>Screenshots and demos captured on **v0.5**.</sub>

## Requirements

- .NET Framework 4.7.2+ or .NET 8 (`net8.0-windows`)
- Windows 10+ recommended (DWM caption theming and rounded corners degrade gracefully on older builds)

## Acknowledgements

The HCT color pipeline is a C# port of Google's
[material-color-utilities](https://github.com/material-foundation/material-color-utilities)
(Apache 2.0). Icons are [Material Symbols](https://fonts.google.com/icons) (Apache 2.0).

## License

[MIT](LICENSE) © dary1337
