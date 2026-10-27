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
| Actions | `MaterialButton` (5 M3 variants, warning / error / shiny, `IsBusy`), `MaterialIconButton` (standard / filled / tonal / outlined / error, toggle, `IsBusy`), `MaterialFab` (small / standard / large / extended), `MaterialSegmentedButton` |
| Text inputs | `MaterialTextField` (filled + outlined, floating label, error state, icons, `InputKind` number filter), `MaterialSearchBar`, `MaterialColorField`, `MaterialValueListEditor` (+ fixed slots), `MaterialDataTableEditor` |
| Selection | `MaterialSwitch`, `MaterialCheckBox` (incl. indeterminate), `MaterialRadioButton`, `MaterialChip` (assist / filter / input / suggestion, colours of your own), `MaterialSlider`, `MaterialOptionCard` (radio / check / none, tone), `MaterialDropdownSelect` |
| Navigation | `MaterialTabs` (primary / secondary), `MaterialNavigationBar`, `MaterialNavigationRail`, `MaterialNavigationDrawer` |
| Communication | `MaterialProgressBar` (linear), `MaterialCircularProgress` (determinate + indeterminate), `MaterialSnackbar`, `MaterialBadge`, `MaterialTooltip` (plain + rich), `MaterialSkeleton` (compose any shimmer placeholder) + `MaterialSkeletonCard` preset, `MaterialStepChecklist`, `MaterialNoticeBanner`, `MaterialEmptyState` |
| Containment | `MaterialCard` (elevated / filled / outlined), `MaterialListItem`, `MaterialDivider`, `MaterialRoundedPanel`, `MaterialScrollPanel` (overlay scrollbar), `MaterialMenu`, `MaterialExpander`, `MaterialPageHeader` |
| Dialogs | `MaterialDialog` (tone, hold to confirm, confirm that runs work), `MaterialMessageBox` (themed info / error / confirm), `MaterialDatePickerDialog` (calendar grid), `MaterialTimePickerDialog` (time input) |
| Window | `BorderlessForm` (native resize/snap without chrome), `MaterialTitleBar` (minimize / maximize / close, each hideable), `DwmWindow` (caption theming, `WindowCornerPreference` rounding), `FormDragAnywhere`, `TaskbarProgress` (+ sessions), `WindowPlacement`, `FormAnimation` |
| Text | `MaterialLabel` (consistent GDI+ rendering) |

## Getting started

```csharp
using Material3.Core;
using Material3.WinForms.Theming;

[STAThread]
static void Main() {
    Application.EnableVisualStyles();

    // One line of theming: every Material control follows this scheme.
    ThemeManager.Apply(MaterialTheme.FromSeed(Argb.FromArgb(0x67, 0x50, 0xA4)), isDark: true);

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
ThemeManager.Theme = MaterialTheme.FromSeed(Color.SeaGreen.ToCore(), SchemeVariant.Vibrant);
```

```csharp
var save = new MaterialButton {
    Text = "Save",
    Variant = MaterialButtonVariant.Filled,
    Glyph = MaterialIcons.Check,
};
```

Custom drawing uses the same tokens the stock controls do:

```csharp
using Material3.WinForms.Theming;   // MaterialColors — current scheme roles
using Material3.WinForms.Tokens;    // Shape, Spacing, StateLayers, Motion, Elevation
using Material3.WinForms.Typography; // TypeScale — the 15-style type scale
```

## Sizes, type and text

Every size, radius, glyph and font is a default the app can override; geometry properties are in 96-DPI px and scale with the monitor.

| # | Lever | Reach | Example |
|---|---|---|---|
| 1 | `TypeScale.XxxStyle` setters, `TypeScale.SetFontFamily` | App-wide; raises `TypeScale.Changed` | `TypeScale.SetFontFamily("Inter", "Inter SemiBold")` |
| 2 | `Font` on a control | One control; it stops following its type role | `button.Font = new Font("Inter", 11f)` |
| 3 | `XxxFont` properties for secondary text | One text slot; `null` follows the role | `listItem.SupportingFont`, `optionCard.DescriptionFont`, `drawer.HeadlineFont` |
| 4 | Geometry properties | One control | `IconSize`, `IconGap`, `CornerRadius`, `ButtonHeight`, `BarHeight`, `TrackHeight`, `BoxSize` |
| 5 | `AutoSize` | Intrinsic-size controls; on by default | Turn it off and set `Size` to stop the control from sizing itself |
| 6 | Static settings | Popups the library builds | `MaterialTooltip.MaxWidth`, `MaterialSnackbar.BarHeight`, `Elevation.TintOpacity` |
| 7 | Dialog properties | One dialog, set before `ShowDialog` | `MaterialDialog.DialogWidth`, `MaterialDatePickerDialog.CellSize`, `MaterialTimePickerDialog.DigitFont` |

`MaterialMenu.Show(anchor)` and `MaterialDropdownSelect` open centred under the anchor and at least as wide as it; `MaterialTooltip` sits centred above its control. These popups fade in and never take focus from the window.

One height scale, the same on both stacks, so controls placed side by side line up:

| # | Height | Token (WPF resource · WinForms `ComponentSizes`) | Used by |
|---|---|---|---|
| 1 | 32 | `CompactControlHeight` | Compact buttons and dense rows |
| 2 | 40 | `ControlHeight` | Every button and icon button, selects (`M3Combo`, `MaterialDropdownSelect`), segmented buttons, plain and pill text boxes, the search bar, a check box, radio or switch row (the M3 state layer) |
| 3 | 56 | `FieldHeight` | Only the M3 text field with a floating label, which needs the room |

A control follows its M3 type role (a button `LabelLarge`, a list item `BodyLarge`) until the app assigns `Font`.

The strings the library shows (OK, Cancel, Search, caption-button names, picker captions) come from
`Material3.Core.LibraryText` and follow `CurrentUICulture`; `LibraryText.Set` and `LibraryText.Resolver` override them,
and the picker dialogs, `MaterialSearchBar.Placeholder` and `MaterialMessageBox` take a text of their own where `null` means the library's.

## Designer support

The controls are built for the Visual Studio designer, not just code:

- **Toolbox & property grid** — every control is a `[ToolboxItem]`; its M3 properties sit under a
  *Material Design* category with descriptions and defaults, and the design surface paints with the
  live theme instead of a blank rectangle.
- **`MaterialThemeManager` component** — drop it on a form and pick `Seed` / `Variant` / `IsDark` in
  the grid; it applies the theme at run time (no manual `ThemeManager.Apply`) and repaints the
  preview so design-time matches the running app.
- **SmartTags & editors** *(.NET Framework designer)* — quick `Variant` / glyph actions on
  `MaterialButton` and `MaterialTextField`, plus a Material Symbols picker for `Glyph`.

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
