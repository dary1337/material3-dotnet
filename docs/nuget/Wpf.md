# Material3.Wpf

**Material 3 (Material You)** for **WPF**. A runtime `ThemeManager` generates the full M3 color-role scheme from one
seed color (HCT) and publishes it as live `DynamicResource` brushes, so the whole UI recolors on the fly — plus
themed M3 controls, an icon-set-agnostic `PathIcon`, motion helpers, an app-level `Modal` (scrim + card) and a
centered popup tooltip. Built on [`Material3.Core`](https://www.nuget.org/packages/Material3.Core).

![Material 3 color roles, dark theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/color-roles-dark.webp)

- Dynamic-color theming — one seed → live light/dark, every role as a `DynamicResource` brush
- Controls: filled / tonal / outlined / text / shiny buttons, icon button and icon toggle, FAB (round and
  extended), text field, chips, switch, radio, three-state checkbox, segmented button, slider, menus, tooltip,
  scrollbar, linear and circular progress, card, list item, divider, expander
- Inputs: `TextField` (filled / outlined, floating label), `SearchBar`, the `M3Combo` select, `ColorField` colour picker, `ValueListEditor` and `DataTableEditor` for lists and records, the
  `InputFilter` number filter for any `TextBox`
- Navigation and status: `Tabs` (primary / secondary), `Badge`, assist / filter / input / suggestion `Chip`s, rich `Tip`
- Page furniture: `PageHeader`, `NoticeBanner`, `EmptyState`, `PopupCard`, `Snackbar`
- Loading: `Skeleton` shimmer blocks, the `Busy` spinner and the `StepChecklist` progress list
- `Dialog` blocking dialog — tone glyph, three-level actions, hold-to-confirm, a confirm that owns its work
- Elevation: shadow effects plus the tinted `SurfaceElevation` roles treatment
- `Modal` app-level modal + scrim, `AnimatedPopup`, centered `Tip` tooltip, `PathIcon`, motion helpers
- `AutoHideScroll` overlay-scrollbar timing, `WheelScroll` system-paced mouse wheel and `DwmWindow` Windows 11
  corner rounding

```
dotnet add package Material3.Wpf
```

```xml
<Application xmlns:m3="clr-namespace:Material3.Wpf;assembly=Material3.Wpf">
  <Application.Resources>
    <ResourceDictionary Source="pack://application:,,,/Material3.Wpf;component/Controls.xaml" />
  </Application.Resources>
</Application>
```

Not in WPF yet: navigation bar / rail / drawer, date and time pickers, title bar.

**Full guide:** https://github.com/dary1337/material3-dotnet/blob/main/docs/wpf.md
&nbsp;·&nbsp; **Repo & demo galleries:** https://github.com/dary1337/material3-dotnet
