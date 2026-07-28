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
| | Buttons — warning · error *(filled + tonal)* · shiny (attention) | ✓ | – |
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
| | Popup helpers (`CenterPopup` · `PopupWatch` · `PopupToggle` · `Chevron`) | ✓ | – |
| | Tabs | – | ✓ |
| | Navigation bar · rail · drawer | – | ✓ |
| | Badge · Title bar | – | ✓ |
| **Layout** | `VirtualizingWrapPanel` | ✓ | – |

## Modals

`M3Modal.Show(card, options)` returns a handle; `Close()` plays the exit and then removes the card. The
`ModalOptions` instance you pass stays live, so an async action can flip `DismissOnEsc` / `DismissOnScrimClick`
off for the duration of its work and the dialog cannot be dismissed out from under it — that is the whole
mechanism behind a confirm dialog that closes only once its operation finishes (see the *Async confirm dialog*
demo in the WPF gallery). Use `OnDismiss` instead when a bound flag, not the modal, owns the open state.

## Motion

`Motion.SwapContent(control, newContent)` swaps a `ContentControl`'s content, cross-fading the presenter while
the control's width eases from the old size to the new one — for a toggle whose caption tracks the state it
drives. The width only animates when the control's own content dictates it: a stretched control, or one the app
gave an explicit `Width`, is sized by something other than its label, so only the cross-fade plays. Give a
toggle in a panel that also holds wider siblings an explicit `HorizontalAlignment` other than `Stretch` if you
want the width motion — WPF centers a stretched element the moment it is given a width, and the helper refuses
to animate rather than make it jump.

## Popup helpers

- `m3:CenterPopup.Enable="True"` centers a popup over its trigger and places it below, flipping above when
  below would clip; add `m3:CenterPopup.PreferAbove="True"` for a bottom-anchored trigger.
- `m3:AnimatedPopup` plays the open animation and watches its own anchor — no `Opened` handler to wire per
  view. Set `WatchAnchor="False"` when the anchor is not inside a scrolling region.
- `PopupWatch.Watch(popup)` does that watching for a plain `Popup`: it closes once the anchor scrolls or
  virtualizes out of view — a WPF popup otherwise stays put while its target moves away.
- `m3:Chevron.IsOpen` on a trigger's trailing glyph rotates it 180° while the menu is open — bind the popup's
  `IsOpen` or the flag behind it. The glyph points at the side the menu opens on: `ChevronDown` for a menu
  that drops down, `ChevronUp` for one that rises.
- `PopupToggle.Open(popup)` (or a `ToggleGuard` per popup for a bound `IsOpen`) makes a trigger button
  actually toggle a `StaysOpen=False` popup: such a popup closes on mouse-DOWN outside it, so the trigger's
  `Click` on mouse-UP would otherwise reopen what the user just dismissed.
- `m3:Motion.ScaleOnOpen="True"` adds the M3 scale to a popup's fade. `CenterPopup` sets it for you; set it
  yourself only on a popup whose `CustomPopupPlacementCallback` centers the content on the target. WPF places a
  popup from its child's *rendered* bounds, so on any edge-aligned placement the scale drags it off the anchor —
  which is why the fade is the default and the scale is opt-in.

## VirtualizingWrapPanel

WPF ships no virtualizing wrap panel — `WrapPanel` realizes every item. Use it as an `ItemsPanelTemplate` and
give it the exact cell size (item + margins):

```xml
<ItemsPanelTemplate><m3:VirtualizingWrapPanel ItemWidth="200" ItemHeight="128" /></ItemsPanelTemplate>
```

A grid revealed from `Collapsed` paints on the first pass, and removing an item does not desync the
container generator.
