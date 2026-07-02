# Material3.WinForms

[← monorepo overview](../README.md) · [NuGet](https://www.nuget.org/packages/Material3.WinForms)

Material 3 (Material You) controls and design tokens for **Windows Forms** — dynamic color, runtime light/dark
switching, the full M3 type scale, elevation, state layers and motion, plus Visual Studio designer support.
No mandatory base form, no native dependencies. Built on [`Material3.Core`](https://www.nuget.org/packages/Material3.Core).

```
dotnet add package Material3.WinForms
```

## Features

- **Dynamic color** — full light + dark `ColorScheme` generated from one seed color through a
  C# port of the HCT/CAM16 pipeline (no native dependencies). Three variants: `TonalSpot`
  (M3 default), `Neutral` (near-monochrome), `Vibrant`.
- **Runtime theme switching** — `ThemeManager.IsDark = false` repaints every subscribed control;
  no restart, no per-control wiring.
- **Color roles** — all M3 roles including the six surface containers, inverse roles, outline
  pair, plus shared `Success`/`Warning` extensions.
- **Type scale** — all 15 styles (Display → Label) on Segoe UI with letter-spacing and
  line-height metadata GDI fonts can't carry.
- **Elevation 0–5** — painted soft shadows (WinForms has no compositor) + per-level surface tint.
- **State layers** — spec opacities for hover / focus / pressed / dragged, used consistently by
  every control.
- **Motion tokens** — M3 duration scale and cubic-bezier easings (standard + emphasized)
  evaluated exactly like CSS timing functions.
- **Material Symbols** — 30+ icons embedded as SVG, rasterized with caching and tinted to any
  role color.
- **Designer support** — controls drop from the toolbox with their M3 properties grouped under a
  *Material Design* category, and a non-visual `MaterialThemeManager` component drives the theme
  from the property grid — no `ThemeManager.Apply` in `Main` — while previewing it live on the
  design surface. SmartTags and a glyph picker on key controls (.NET Framework designer).

## Controls

| Family | Included |
|---|---|
| Actions | `MaterialButton` (5 variants), `MaterialIconButton` (4 styles + toggle), `MaterialFab` (small / standard / large / extended), `MaterialSegmentedButton` |
| Text inputs | `MaterialTextField` (filled + outlined, floating label, error state, icons), `MaterialSearchBar` |
| Selection | `MaterialSwitch`, `MaterialCheckBox` (incl. indeterminate), `MaterialRadioButton`, `MaterialChip` (assist / filter / input / suggestion), `MaterialSlider`, `MaterialOptionCard`, `DropdownSelect` |
| Navigation | `MaterialTabs` (primary / secondary), `MaterialNavigationBar`, `MaterialNavigationRail`, `MaterialNavigationDrawer` |
| Communication | `MaterialProgressBar` (linear), `MaterialCircularProgress` (determinate + indeterminate), `MaterialSnackbar`, `MaterialBadge`, `MaterialTooltip` (plain + rich), `MaterialSkeleton` (compose any shimmer placeholder) + `SkeletonCard` preset, `StepChecklist` |
| Containment | `MaterialCard` (elevated / filled / outlined), `MaterialListItem`, `MaterialDivider`, `RoundedPanel`, `MaterialScrollPanel` (overlay scrollbar), `MaterialMenu` |
| Dialogs | `MaterialDialog`, `MaterialMessageBox` (themed info / error / confirm), `MaterialDatePickerDialog` (calendar grid), `MaterialTimePickerDialog` (time input) |
| Window | `BorderlessForm` (native resize/snap without chrome), `MaterialTitleBar` (minimize / maximize / close, each hideable), `WindowChrome` (DWM caption theming), `FormDragAnywhere`, `TaskbarProgress`, `FormAnimation` |
| Text | `SoftLabel` (consistent GDI+ rendering) |

## Quickstart

```csharp
using Material3.WinForms.Theming;

[STAThread]
static void Main() {
    Application.EnableVisualStyles();

    // One line of theming: every Material control follows this scheme.
    ThemeManager.Apply(MaterialTheme.FromSeed(Color.FromArgb(0x67, 0x50, 0xA4)), isDark: true);

    Application.Run(new MainForm());
}
```

Prefer the designer? Drop a **`MaterialThemeManager`** onto your form and set `Seed` / `Variant` /
`IsDark` in the property grid — it applies the theme at run time (so you can skip the `Main` call
above) and previews it on the design surface.

```csharp
// Switch mode at runtime — all controls repaint themselves.
ThemeManager.IsDark = !ThemeManager.IsDark;

// Or swap the whole palette.
ThemeManager.Theme = MaterialTheme.FromSeed(Color.SeaGreen, SchemeVariant.Vibrant);
```

```csharp
var save = new MaterialButton {
    Text = "Save",
    Variant = MaterialButtonVariant.Filled,
    IconGlyph = MaterialIcons.Check,
};
```

Custom drawing uses the same tokens the stock controls do:

```csharp
using Material3.WinForms.Theming;   // MaterialColors — current scheme roles
using Material3.WinForms.Tokens;    // Shape, Spacing, StateLayers, Motion, Elevation
using Material3.WinForms.Typography; // MaterialType — the 15-style type scale
```

## Designer support

The controls are built for the Visual Studio designer, not just code:

- **Toolbox & property grid** — every control is a `[ToolboxItem]`; its M3 properties sit under a
  *Material Design* category with descriptions and defaults, and the design surface paints with the
  live theme instead of a blank rectangle.
- **`MaterialThemeManager` component** — drop it on a form and pick `Seed` / `Variant` / `IsDark` in
  the grid; it applies the theme at run time (no manual `ThemeManager.Apply`) and repaints the
  preview so design-time matches the running app.
- **SmartTags & editors** *(.NET Framework designer)* — quick `Variant` / glyph actions on
  `MaterialButton` and `MaterialTextField`, plus a Material Symbols picker for `IconGlyph`.

On .NET 8 the toolbox, property grid and live preview work the same; the SmartTag/editor extras are
.NET Framework-only for now (the out-of-process .NET designer would need a separate design-time
assembly).

## High-DPI

The controls scale their owner-drawn geometry to the monitor DPI (via `Control.DeviceDpi`), so they
stay crisp at 125/150/200%. DPI awareness is a **process-level** setting that the host application
must opt into — a referenced DLL cannot set it. In your app:

- declare awareness in your `app.manifest`: `<dpiAware>true</dpiAware>` (System-DPI) is enough for
  `Control.DeviceDpi` to report the real DPI. For Per-Monitor V2, add
  `<dpiAwareness>PerMonitorV2</dpiAwareness>` to the manifest — or, on .NET Framework, an `app.config`
  `<System.Windows.Forms.ApplicationConfigurationSection>` with `<add key="DpiAwareness" value="PerMonitorV2" />`.
  On .NET 8 you can instead call `Application.SetHighDpiMode(...)`.
- set `AutoScaleMode = AutoScaleMode.Dpi` on your forms.

That's all — the Material controls then render crisply with no extra code. See
`samples/Material3.WinForms.Gallery/app.manifest` for a working example. If you do your own owner-drawing with
the shared tokens, `Material3.WinForms.Dpi.Scale(control, px)` is the same helper the controls use.
