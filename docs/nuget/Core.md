# Material3.Core

The platform-neutral **Material 3 (Material You)** color engine for .NET — a C# port of the HCT / CAM16 pipeline.
Generate a full light **and** dark `ColorScheme` (every M3 role) from a single seed color, with tonal palettes and
scheme variants. The color type is the UI-neutral `Argb` — no `System.Drawing` or WPF dependency — so the UI
packages adapt at their boundary.

```csharp
using Material3.Core;

var palette = CorePalette.FromSeed(Argb.FromArgb(0x67, 0x50, 0xA4));
ColorScheme dark = ColorScheme.Dark(palette);
Argb primary = dark.Primary;
```

- HCT tonal palettes + CAM16; all M3 color roles, surface containers and inverse/outline pairs
- `SchemeVariant`: `TonalSpot` (M3 default), `Neutral`, `Vibrant`
- `netstandard2.0`, dependency-free

Consumed by [`Material3.WinForms`](https://www.nuget.org/packages/Material3.WinForms) and
[`Material3.Wpf`](https://www.nuget.org/packages/Material3.Wpf).

**Docs, controls and demo galleries:** https://github.com/dary1337/material3-dotnet
