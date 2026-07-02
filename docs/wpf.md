# Material3.Wpf

[← monorepo overview](../README.md) · [NuGet](https://www.nuget.org/packages/Material3.Wpf)

Material 3 (Material You) for **WPF**. A runtime `M3Theme` generates the full M3 color-role scheme from one
seed color (HCT) and publishes it as live `DynamicResource` brushes — the whole UI recolors on the fly — plus
themed M3 controls, an icon-set-agnostic `M3Icon`, motion helpers, an app-level `M3Modal` (scrim + card) and a
centered popup tooltip. Built on [`Material3.Core`](https://www.nuget.org/packages/Material3.Core).

```
dotnet add package Material3.Wpf
```

## Quickstart

Merge the control dictionary (it self-contains its tokens) and apply a theme once at startup:

```xml
<Application xmlns:m3="clr-namespace:Material3.Wpf;assembly=Material3.Wpf">
  <Application.Resources>
    <ResourceDictionary Source="pack://application:,,,/Material3.Wpf;component/Controls.xaml" />
  </Application.Resources>
</Application>
```

```csharp
// One seed → every role published as a live DynamicResource brush; call again to recolor or toggle dark.
M3Theme.Apply(MaterialTheme.FromSeed(Argb.FromArgb(0x67, 0x50, 0xA4)), isDark: true, Application.Current.Resources);
```

Wrap the window content in one `<m3:M3ModalLayer>` to enable `M3Modal.Show(card)` (an app-wide scrim that
blocks everything behind it), use `m3:Tip.Text="…"` for a centered popup tooltip, and `<m3:AnimatedPopup>` in
place of `<Popup>` for an M3 close animation.

## Control coverage

The shared engine (dynamic color, type/shape scale, `M3Icon`, motion) is the same on both stacks. The
**control suite** is where WPF is still catching up to WinForms — this is what ships today vs what's next.
Legend: **✓ shipped · – planned**.

| Category | Component | WPF | WinForms |
|---|---|:---:|:---:|
| **Foundation** | Dynamic color (`M3Theme` / `ThemeManager`) | ✓ | ✓ |
| | Type scale · Shape scale | ✓ | ✓ |
| | Icon (icon-set-agnostic) | ✓ | ✓ |
| | Motion helpers | ✓ | ✓ |
| | Painted elevation (levels 0–5) | – | ✓ |
| **Actions** | Buttons — filled · tonal · outlined · text | ✓ | ✓ |
| | Buttons — warning · shiny (attention) | ✓ | – |
| | Icon button | – | ✓ |
| | FAB | – | ✓ |
| **Inputs & selection** | Text field | ✓ *(box + placeholder)* | ✓ *(filled/outlined, floating label)* |
| | Chips | ✓ *(status)* | ✓ *(assist/filter/input/suggestion)* |
| | Checkbox · Radio · Switch | – | ✓ |
| | Segmented button | – | ✓ |
| | Slider | – | ✓ |
| **Containers** | Card | ✓ | ✓ *(elevated/filled/outlined)* |
| | Expander | ✓ | – |
| | List item · Divider | – | ✓ |
| **Feedback** | Linear progress | ✓ | ✓ |
| | Circular progress · Skeleton | – | ✓ |
| | Snackbar | – | ✓ |
| | Tooltip | ✓ | ✓ |
| **Nav & overlays** | Menu · Context menu | ✓ | ✓ |
| | Scrollbar | ✓ | ✓ |
| | Dropdown select | – | ✓ |
| | Dialog *(+ date/time pickers)* | – | ✓ |
| | Modal host + scrim (`M3Modal`) | ✓ | – |
| | Tabs | – | ✓ |
| | Navigation bar · rail · drawer | – | ✓ |
| | Badge · Title bar | – | ✓ |
