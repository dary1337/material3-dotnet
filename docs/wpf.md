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
| | Painted elevation (levels 0–5) | ✓ | ✓ |
| | Window corners (`DwmWindow` / `WindowChrome`) | ✓ | ✓ |
| **Actions** | Buttons — filled · tonal · outlined · text | ✓ | ✓ |
| | Buttons — warning · error *(filled + tonal)* · shiny (attention) | ✓ | – |
| | Icon button | ✓ | ✓ |
| | Icon toggle | ✓ | ✓ *(MaterialIconButton.IsToggle)* |
| | FAB *(round + extended, three sizes)* | ✓ | ✓ |
| **Inputs & selection** | Text field | ✓ *(box + placeholder)* | ✓ *(filled/outlined, floating label)* |
| | Chips | ✓ *(status)* | ✓ *(assist/filter/input/suggestion)* |
| | Radio · Switch | ✓ | ✓ |
| | Checkbox *(three-state)* | ✓ | ✓ |
| | Segmented button | ✓ | ✓ |
| | Slider | ✓ | ✓ |
| **Containers** | Card | ✓ | ✓ *(elevated/filled/outlined)* |
| | Expander | ✓ | – |
| | Page header (`PageHeader`) | ✓ | – |
| | List item · Divider · list container | ✓ | ✓ |
| **Feedback** | Linear progress | ✓ | ✓ |
| | Skeleton | ✓ | ✓ |
| | Busy spinner (`Busy`) | ✓ | – |
| | Notice banner (`NoticeBanner`) | ✓ | – |
| | Empty state (`EmptyState`) | ✓ | – |
| | Circular progress | ✓ | ✓ |
| | Snackbar | ✓ | ✓ |
| | Tooltip | ✓ | ✓ |
| **Nav & overlays** | Menu · Context menu | ✓ | ✓ |
| | Scrollbar *(+ auto-hide)* | ✓ | ✓ |
| | Popup surface (`PopupCard`) | ✓ | – |
| | Dropdown select | – | ✓ |
| | Dialog *(+ date/time pickers)* | – | ✓ |
| | Modal host + scrim (`M3Modal`) | ✓ | – |
| | Popup helpers (`CenterPopup` · `PopupWatch` · `PopupToggle` · `Chevron`) | ✓ | – |
| | Tabs | – | ✓ |
| | Navigation bar · rail · drawer | – | ✓ |
| | Badge · Title bar | – | ✓ |
| **Layout** | `VirtualizingWrapPanel` | ✓ | – |

## Elevation

Two ingredients, and on a dark scheme only one of them is visible — a black shadow over a near-black surface
says nothing, so the tint is what carries the height.

| # | Ingredient | Where it comes from |
|---|---|---|
| 1 | Shadow | `Elevation1`–`Elevation5` effects in `Tokens.xaml`. Level 0 has no resource: it is the absence of an effect |
| 2 | Surface | `SurfaceElevation0`–`SurfaceElevation5` brushes, published by `M3Theme.Apply` next to the colour roles |

The shadows are `Color="Black"` rather than the `Shadow` role because an `Effect` takes a `Color`, which no
`DynamicResource` can re-supply on a theme switch. M3 derives `Shadow` as neutral tone 0 in both schemes, so
nothing is lost.

## Selection, actions and lists

| Type | Knobs | Why it behaves that way |
|---|---|---|
| `m3:Fab` | `IconKind` `Size` (Small 40 / Standard 56 / Large 96), `Content` | One template: a label extends it into the pill, no label collapses it back to the circle |
| `M3CheckBox` *(style)* | `IsThreeState` | Indeterminate carries a dash, not a half-check — "some of the children" must not read as "all of them" |
| `m3:SegmentedButton` | items, `MultiSelect` `SelectedIndex` `SelectedIndices` `SetSelected` `SelectionChanged` | Selection lives on the row: checking one unchecks the rest, so no caller keeps a group in sync |
| `M3Slider` *(style)* | any `Slider`, `m3:SliderMotion.Animated` | The filled track IS the decrease repeat button, so it is already exactly as wide as the value. A value that did not come from a drag eases into place — easing a drag would read as lag |
| `m3:M3ListItem` | `Leading` `IconKind` `Headline` `SupportingText` `Trailing` `TrailingText` `IsSelected` | Picks 56 or 72 from whether there is a second line, so a mixed list still lines up |
| `Divider` / `DividerVertical` *(styles)* | any `Border` | Inset one with a `Margin`; `Separator` is left to menus |
| `ListBoxItem` *(implicit)* | `Background` `Padding`, any `ListBox` | Transparent until given a fill, so a row on a page stays a row and a tile grid can own its surface. Selection outranks hover, and the focus ring is an overlay that costs no layout |
| `m3:CircularProgress` | `Value` `Minimum` `Maximum` `IsIndeterminate` `Diameter` `StrokeThickness` `IndicatorBrush` `TrackBrush` | The indeterminate arc breathes 20°→270° as it turns, so the head chases the tail |
| `m3:Snackbar` | `Snackbar.Show(anchor, message[, actionText, onAction][, duration])`, `DismissCurrent()` | Goes into the window's adorner layer, so it needs no host element; one shows at a time and the rest queue |

`M3ListItem` is the one prefixed name in the set: `ListItem` is already a WPF document element, and a consumer
with `System.Windows.Documents` in scope would have to disambiguate every use.

Two of these take a brush property instead of `Foreground` (`CircularProgress`, and `M3ListItem`'s text, which
is coloured in its template). `Foreground` is an inherited property, so it arrives already set from whatever the
control was dropped into — a role assigned by a style setter never gets a say.

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

## Page furniture · loading

Lookless, so an `x:Name` inside a content slot compiles — a `UserControl` namescope fails with MC3093.

| Type | Slots / knobs | Why it behaves that way |
|---|---|---|
| `m3:PageHeader` | `Overline` `Title` `Description`, `OverlineTrailing` `TitleTrailing` `Actions` | Owns the gap below it, so pages never set one |
| `m3:NoticeBanner` | `Severity` `IconKind` `Text` `Actions` `CanDismiss`, `Dismissed` | Never removes itself: only the host knows if it should return. An action goes in at 32 high and with no vertical margin — the row is one line, and a margin inside a centred slot shifts the button off the centre line |
| `m3:EmptyState` | `IconKind` `Title` `Text` `Action` | |
| `m3:PopupCard` | content = `MenuItemButton` rows | Grows to the opener's width (own `MinWidth` still wins): a menu narrower than its trigger reads as an unrelated panel |
| `m3:Skeleton` | `Width` `Height` `CornerRadius` | Shimmer runs on the render thread, so it moves while the dispatcher rebuilds; stops itself on hide and on unload |
| `m3:Busy.IsBusy` | on `M3Icon` — swaps the glyph for the spinner | Inert elsewhere, so a template can trigger off it (`IconToggle` does) |
| `m3:Busy.SpinWhileVisible` | on an already-spinner glyph | Drops the clock the moment it hides |

Both `Busy` modes resolve `"Loading"` through `M3Icon.Register` — unregistered, the spinner renders blank.

## Auto-hiding scrollbar

Opt in once, every bar follows. Look unchanged; this is only the timing — 1.5 s idle, 220 ms fade, and no show
on first layout, because the bar answers the user rather than announcing itself.

```xml
<Style TargetType="ScrollBar" BasedOn="{StaticResource M3ScrollBar}">
  <Setter Property="m3:AutoHideScroll.Enabled" Value="True" />
</Style>
```

Two rules, both silent when broken — the bar just turns into the native Windows one:

| # | Rule | Why |
|---|---|---|
| 1 | `BasedOn` the **keyed** `M3ScrollBar`, never `{x:Type ScrollBar}` | Application resources load deferred, so that key resolves to the style being declared |
| 2 | Declare it as a **direct child of `Application.Resources`**, not inside a dictionary merged into it | A `Style`'s `BasedOn` resolves while its own dictionary is parsed, and a dictionary merged into `Application.Resources` cannot see its siblings. An element-level dictionary is fine — there the lookup reaches the app through the tree |

## Window corners

`DwmWindow.TrySetCornerPreference(window, WindowCornerPreference.Round)` — the compositor rounds it, so it is
anti-aliased and the shadow follows.

| Member | Answers |
|---|---|
| `IsSupported` | Build ≥ 22000, read via `RtlGetVersion` — `Environment.OSVersion` answers from the **host app's** manifest, which a library cannot control |
| `IsRounded(window)` | Whether it ends up rounded: `AllowsTransparency` hands compositing to WPF, and maximized is never rounded |
| `RoundRadiusDip` | The radius a WPF-composed overlay needs to match a native corner |

## VirtualizingWrapPanel

WPF ships no virtualizing wrap panel — `WrapPanel` realizes every item. Use it as an `ItemsPanelTemplate` and
give it the exact cell size (item + margins):

```xml
<ItemsPanelTemplate><m3:VirtualizingWrapPanel ItemWidth="200" ItemHeight="128" /></ItemsPanelTemplate>
```

A grid revealed from `Collapsed` paints on the first pass, and removing an item does not desync the
container generator.
