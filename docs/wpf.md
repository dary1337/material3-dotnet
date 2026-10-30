# Material3.Wpf

[← monorepo overview](../README.md) · [NuGet](https://www.nuget.org/packages/Material3.Wpf)

Material 3 (Material You) for **WPF**. A runtime `ThemeManager` generates the full M3 color-role scheme from one
seed color (HCT) and publishes it as live `DynamicResource` brushes — the whole UI recolors on the fly — plus
themed M3 controls, an icon-set-agnostic `PathIcon`, motion helpers, an app-level `Modal` (scrim + card) and a
centered popup tooltip. Built on [`Material3.Core`](https://www.nuget.org/packages/Material3.Core).

```
dotnet add package Material3.Wpf
```

## Getting started

Merge the control dictionary (it self-contains its tokens) and apply a theme once at startup:

```xml
<Application xmlns:m3="clr-namespace:Material3.Wpf;assembly=Material3.Wpf">
  <Application.Resources>
    <ResourceDictionary Source="pack://application:,,,/Material3.Wpf;component/Controls.xaml" />
  </Application.Resources>
</Application>
```

```csharp
// One seed → every role published as a live DynamicResource brush.
ThemeManager.Apply(MaterialTheme.FromSeed(Argb.FromArgb(0x67, 0x50, 0xA4)), isDark: true, Application.Current.Resources);

// Later, one at a time — each republishes into the same dictionary.
ThemeManager.IsDark = !ThemeManager.IsDark;
ThemeManager.Theme = MaterialTheme.FromSeed(Argb.FromArgb(0x2E, 0x7D, 0x32), SchemeVariant.Vibrant);
```

Wrap the window content in one `<m3:ModalLayer>` to enable `Modal.Show(card)` (an app-wide scrim that
blocks everything behind it), use `m3:Tip.Text="…"` for a centered popup tooltip, and `<m3:AnimatedPopup>` in
place of `<Popup>` for an M3 close animation.

The glyphs the library's own templates draw are built in (`PathIcon.BuiltInKeys`: `Check`, `Close`, the four
chevrons, `Plus`, `Minus`, `Magnify`, `OpenInNew`, `Loading` and the dialog tones). A key the app registers wins,
so one icon set stays consistent.

## Naming

| # | What | Rule | Why |
|---|---|---|---|
| 1 | Types | No prefix — `m3:Chip`, `Dialog`, `ThemeManager` | The `Material3.Wpf` namespace and the `m3:` XAML prefix already say it |
| 2 | `ListTile` | Not `ListItem` | `System.Windows.Documents.ListItem` is in every default WPF code-behind's usings |
| 3 | `PathIcon` | Not `Icon` | Inside a `Window`, `Icon` is the window's own property, so `Icon.Register(…)` would not compile |
| 4 | Glyph keys | `Glyph`, `LeadingGlyph`, `TrailingGlyph` on every control, the same in the WinForms package | One word for a registered glyph key; `Kind` is left for categories such as `ChipKind` |
| 5 | Tints | One `Tone` enum for chips, banners and dialogs; WinForms buttons take the same `MaterialTone` | The same meaning reads the same everywhere |
| 6 | Boolean state | `Is…` (`IsSelected`, `IsDot`, `IsEnabled`), the same in the WinForms package | One rule across both packages; WinForms keeps a name its base control already fixed (`Checked`, `ReadOnly`) |
| 7 | Keyed styles | `M3<Control>` for the M3 look of a stock control (`M3CheckBox`, `M3Switch`, `M3Combo`); the M3 component name otherwise (`FilledButton`, `Card`) | Resource keys share one app-wide namespace, so a restyle must not take a bare control name |
| 8 | Tests | A sentence that states the behaviour (`ARadioCardUnchoosesItsGroupAcrossPanelsOfOneTree`), in both test projects | A failing name says what broke, and the two suites read alike |

## Sizes, type and text

Every size, radius, glyph and font in the templates is a default the app can override. The pickers, built in code, read the
same keys (`DatePickerCellSize`, `TimePickerFieldSize`, `TimePickerPeriodWidth`) and the type styles; the rest is named in the code.

| # | Lever | Reach | Example |
|---|---|---|---|
| 1 | Style setter (`Padding`, `MinHeight`, `FontSize`, `FontFamily`, `FontWeight`) | One control, or every one through a `BasedOn` style | `<Setter Property="FontSize" Value="13" />` on `{x:Type m3:Chip}` |
| 2 | `m3:Metrics.CornerRadius` · `GlyphSize` · `GlyphMargin` · `IndicatorSize` | Template geometry that has no CLR property | `<Button Style="{StaticResource FilledButton}" m3:Metrics.GlyphSize="20" />` |
| 3 | Token resources | App-wide, read as `DynamicResource` | `<CornerRadius x:Key="RadiusPill">12</CornerRadius>` after the merged dictionary |
| 4 | Keyed styles for parts built in code or deep in a template | `Dialog` and `Tip` cards, the nav badge | Redefine `DialogCard` with `BasedOn="{StaticResource DialogCard}"` |
| 5 | Control properties | One control | `PageHeader.DescriptionMaxWidth`, `EmptyState.TextMaxWidth`, `ValueListEditor.RowHeight`, `Badge.PillHeight`, `TextField.FieldHeight`, `SliderMotion.TrackThickness` · `TrackGap`, `OptionCard.HoverOpacity` |
| 6 | Static settings | Placement the library computes | `CenteredToolTip.Gap` |

What each `Metrics` property means, per control family:

| # | Property | Read by | Meaning |
|---|---|---|---|
| 1 | `CornerRadius` | Buttons, chips, fields, menus, tooltips, cards, banners, list tiles, nav items, tabs, badges, slider track | The outer corner radius |
| 2 | `GlyphSize` | Buttons, chips, menu items, nav items, list tiles, banners, fields, title bar | The side of the leading glyph |
| 3 | `GlyphMargin` | The same, plus `Dialog`'s header glyph | The space around the glyph (its gap to the label) |
| 4 | `IndicatorSize` | Check box, radio, switch, option card, slider thumb, nav pill, tab underline, badge dot, step ring | The selection mark's size |

| # | Token | Default |
|---|---|---|
| 1 | `RadiusXs` · `RadiusSm` · `RadiusMd` · `RadiusLg` · `RadiusPill` · `RadiusXl` | 4 · 8 · 12 · 16 · 20 · 28 |
| 2 | `FocusRingRadius` | 8 |
| 3 | `Space1` … `Space7` | 4 · 8 · 12 · 16 · 24 · 32 · 48 |
| 4 | `CompactControlHeight` · `ControlHeight` · `FieldHeight` | 32 · 40 · 56 |
| 5 | `DialogWidth` · `DatePickerCellSize` | 420 · 40 |
| 6 | `TimePickerFieldSize` · `TimePickerFieldFontSize` | 96,72 · 45 |
| 7 | `FontFamily` · `FontWeightMedium` | Segoe UI · SemiBold |

A template radius is capped at half the element's shorter side (`{m3:FitCornerRadius}`), so a pill radius on a shorter control keeps round ends.
A select's list opens centred under it and at least as wide as it.

One height scale, the same on both stacks, so controls placed side by side line up:

| # | Height | Token (WPF resource · WinForms `ComponentSizes`) | Used by |
|---|---|---|---|
| 1 | 32 | `CompactControlHeight` | Compact buttons and dense rows |
| 2 | 40 | `ControlHeight` | Every button and icon button, selects (`M3Combo`, `MaterialDropdownSelect`), segmented buttons, plain and pill text boxes, the search bar, a check box, radio or switch row (the M3 state layer) |
| 3 | 56 | `FieldHeight` | Only the M3 text field with a floating label, which needs the room |

Code-built cards take the keyed styles `DialogCard`, `TipCard`, `RichTipCard`, `TipText`, `RichTipText` and `TipTitle`.
A navigation item's badge takes `NavigationBadge`; define that key in the item's resources to move or resize one dot.
A `Badge`'s `Background`, `Foreground` and `BorderBrush` override its `Tone`.

### Library text

The strings the library itself shows (OK, Cancel, Search, the caption buttons' names, the picker captions) come from
`Material3.Core.LibraryText`, so they follow `CultureInfo.CurrentUICulture`. Built in: English and Russian; any other language goes through `LibraryText.Resolver`.

| # | Need | Call |
|---|---|---|
| 1 | Replace one string | `LibraryText.Set(TextKey.Ok, "Apply")`; `null` restores it |
| 2 | Plug in your own resources | `LibraryText.Resolver = (key, culture) => Strings.ResourceManager.GetString(key.ToString(), culture)`; `null` falls through |
| 3 | Switch language at run time | Set `CurrentUICulture`, then `LibraryText.Refresh()`; every `{m3:Text Key}` binding updates |
| 4 | Use a library string in your XAML | `Content="{m3:Text Cancel}"` |

Resolution order: `Set` override, then `Resolver`, then the built-in table for the culture and its parents, then English.

## Control coverage

The shared engine (dynamic color, type/shape scale, icons, motion) is the same on both stacks, and so is most of the
control suite; a row marked – has no WinForms counterpart in this release.
Legend: **✓ shipped · – not in this release**.

| Category | Component | WPF | WinForms |
|---|---|:---:|:---:|
| **Foundation** | Dynamic color (`ThemeManager` / `ThemeManager`) | ✓ | ✓ |
| | Type scale *(all 15 M3 roles)* · Shape scale | ✓ | ✓ |
| | Icon (icon-set-agnostic) | ✓ | ✓ |
| | Motion helpers | ✓ | ✓ |
| | Painted elevation (levels 0–5) | ✓ | ✓ |
| | Window corners (`DwmWindow`) | ✓ | ✓ |
| **Actions** | Buttons — elevated · filled · tonal · outlined · text | ✓ | ✓ |
| | Buttons — warning · error *(filled + tonal)* · shiny (attention) | ✓ | ✓ |
| | Leading glyph on a button (`ButtonContent.LeadingGlyph`) | ✓ | ✓ |
| | Busy button *(spinner in the glyph, clicks ignored)* | ✓ | ✓ |
| | Icon button — standard · filled · tonal · outlined · error | ✓ | ✓ |
| | Icon toggle — standard · filled · tonal · outlined | ✓ | ✓ |
| | FAB *(round + extended, three sizes)* | ✓ | ✓ |
| **Inputs & selection** | Text field *(filled/outlined, floating label, error)* | ✓ | ✓ |
| | Chips *(assist/filter/input/suggestion)* | ✓ | ✓ |
| | Chip colours of your own | ✓ | ✓ |
| | Status chip *(tone + size)* | ✓ | – |
| | Search bar | ✓ | ✓ |
| | Radio · Switch | ✓ | ✓ |
| | Checkbox *(three-state)* | ✓ | ✓ |
| | Segmented button *(check on the selected segment)* | ✓ | ✓ |
| | Slider | ✓ | ✓ |
| | Colour field | ✓ | ✓ |
| | List and record editors | ✓ | ✓ |
| | Number input filter *(text box and floating field)* | ✓ | ✓ |
| **Containers** | Card *(elevated/filled/outlined)* | ✓ | ✓ |
| | Option card *(radio · check · none, tone)* | ✓ | ✓ |
| | Expander | ✓ | ✓ |
| | Page header | ✓ | ✓ |
| | List item · Divider · list container | ✓ | ✓ |
| **Feedback** | Linear progress | ✓ | ✓ |
| | Skeleton *(+ ready-made card)* | ✓ | ✓ |
| | Notice banner | ✓ | ✓ |
| | Empty state | ✓ | ✓ |
| | Circular progress | ✓ | ✓ |
| | Step checklist (`StepChecklist`) | ✓ | ✓ |
| | Snackbar | ✓ | ✓ |
| | Tooltip *(plain + rich)* | ✓ | ✓ |
| **Nav & overlays** | Menu · Context menu *(glyph, shortcut, disabled row)* | ✓ | ✓ |
| | Scrollbar *(+ auto-hide)* | ✓ | ✓ |
| | Popup surface (`PopupCard`) | ✓ | – |
| | Dropdown select (`M3Combo`) | ✓ | ✓ |
| | Dialog *(tone, destructive, hold to confirm, confirm that runs work)* | ✓ | ✓ |
| | Date / time pickers | ✓ | ✓ |
| | Modal host + scrim (`Modal`) | ✓ | – |
| | Popup helpers (`CenterPopup` · `PopupWatch` · `PopupToggle` · `Chevron`) | ✓ | – |
| | Tabs *(primary/secondary)* | ✓ | ✓ |
| | Navigation bar · rail · drawer *(badge tone, busy, selected glyph)* | ✓ | ✓ |
| | Badge | ✓ | ✓ |
| | Title bar | ✓ | ✓ |
| | Taskbar progress *(sessions)* | ✓ | ✓ |
| | Window placement | ✓ | ✓ |
| **Layout** | `VirtualizingWrapPanel` | ✓ | – |
| | System-paced mouse wheel (`WheelScroll`) | ✓ | – |

## Elevation

Two ingredients, and on a dark scheme only one of them is visible — a black shadow over a near-black surface
says nothing, so the tint is what carries the height.

| # | Ingredient | Where it comes from |
|---|---|---|
| 1 | Shadow | `Elevation1`–`Elevation5` effects in `Tokens.xaml`. Level 0 has no resource: it is the absence of an effect |
| 2 | Surface | `SurfaceElevation0`–`SurfaceElevation5` brushes, published by `ThemeManager.Apply` next to the colour roles |

The shadows are `Color="Black"` rather than the `Shadow` role because an `Effect` takes a `Color`, which no
`DynamicResource` can re-supply on a theme switch. M3 derives `Shadow` as neutral tone 0 in both schemes, so
nothing is lost.

## Selection, actions and lists

| Type | Knobs | Why it behaves that way |
|---|---|---|
| `m3:Fab` | `Glyph` `Size` (Small 40 / Standard 56 / Large 96), `Content` | One template: a label extends it into the pill, no label collapses it back to the circle |
| `M3CheckBox` *(style)* | `IsThreeState` | Indeterminate carries a dash, not a half-check — "some of the children" must not read as "all of them" |
| `m3:SegmentedButton` | items, `MultiSelect` `SelectedIndex` `SelectedIndices` `SetSelected` `SelectionChanged` | Selection lives on the row: checking one unchecks the rest, so no caller keeps a group in sync |
| `M3Slider` *(style)* | any `Slider`, `m3:SliderMotion.IsAnimated` | The filled track IS the decrease repeat button, so it is already exactly as wide as the value. A value that did not come from a drag eases into place — easing a drag would read as lag |
| `m3:ListTile` | `Leading` `Glyph` `Headline` `SupportingText` `Trailing` `TrailingText` `IsSelected` | Picks 56 or 72 from whether there is a second line, so a mixed list still lines up |
| `Divider` / `DividerVertical` *(styles)* | any `Border` | Inset one with a `Margin`; `Separator` is left to menus |
| `ListBoxItem` *(implicit)* | `Background` `Padding`, any `ListBox` | Transparent until given a fill, so a row on a page stays a row and a tile grid can own its surface. Selection outranks hover, and the focus ring is an overlay that costs no layout |
| `m3:CircularProgress` | `Value` `Minimum` `Maximum` `IsIndeterminate` `Diameter` `StrokeThickness` `IndicatorBrush` `TrackBrush` | The indeterminate arc breathes 20°→270° as it turns, so the head chases the tail |
| `m3:Chip` | `Kind` (`Status` `Assist` `Filter` `Input` `Suggestion`) `Text` `Glyph` `IsSelected`, `Click` `Removed`; `Tone` `Size` for status | A status chip is a label, not a button: it takes no focus and raises no click. An input chip never removes itself — only the host knows the token is gone |
| `m3:Tabs` | `Variant` (`Primary` `Secondary`), ordinary `TabItem`s | The indicator follows the selected tab through every layout pass, so a header that changes width or a resize keeps it in place; only a selection change slides it |
| `m3:Badge` | `Count` `IsDot` `Tone` | Zero shows nothing and past 999 it reads `999+`. It does not place itself: put it in the corner of a Grid cell over what it annotates |
| `m3:OptionCard` | `Leading` content, `Indicator` (`Radio` `Check` `None`) `IsChecked` `GroupName` `Tone` | A radio card behaves like a `RadioButton`: a click only chooses, and it unchooses its panel's siblings or its `GroupName` window-wide. `Tone` tints the outline before the option is picked |
| `M3RoundCheckBox` *(style)* | any `CheckBox` | A round check for an on/off that lives inside a selectable card, so it does not read as a second list selection |
| `ElevatedCard` / `FilledCard` / `OutlinedCard` *(styles)* | a `ContentControl` / any `Border` / any `Border` | The three M3 card containers; `Card` stays the plain SurfaceContainer one. The elevated one is templated so its shadow stays off the content |
| `ErrorIconButton` *(style)* | any `Button` | The hover turns ErrorContainer, so the colour warns about the click instead of decorating every icon. Keeps a `Background` you give it |
| `m3:ButtonContent.LeadingGlyph` | on the shipped button styles | The glyph follows the label's colour, the disabled one included |
| `m3:Snackbar` | `Snackbar.Show(anchor, message[, actionText, onAction][, duration])`, `DismissCurrent([anchor])` | Goes into the window's adorner layer, so it needs no host element; one shows at a time in each window and the rest of that window queue |


Two of these take a brush property instead of `Foreground` (`CircularProgress`, and `ListTile`'s text, which
is coloured in its template). `Foreground` is an inherited property, so it arrives already set from whatever the
control was dropped into — a role assigned by a style setter never gets a say.

## Inputs and editors

| Type | Knobs | Why it behaves that way |
|---|---|---|
| `m3:TextField` | `Text` `Label` `Variant` (`Filled` `Outlined`) `SupportingText` `ErrorText` `IsError` `LeadingGlyph` `TrailingGlyph` `IsPassword` `IsReadOnly` `MaxLength`, `TrailingGlyphClick` | The label is laid out at rest and floats by a transform, so the editor never reflows. `IsPassword` swaps in a `PasswordBox` whose content still flows through `Text` |
| `m3:SearchBar` | `Query` `Placeholder`, `QueryChanged` `QuerySubmitted` | Esc clears the query first and lets the key through once it is empty, so an Esc that closes the page still works |
| `M3Combo` *(style)* | any `ComboBox`, `m3:Placeholder.Text` | The menu is a `PopupCard`, so it is never narrower than the field; the highlight follows the arrow keys, not just the mouse |
| `M3PillTextBox` *(style)* | any `TextBox` | A 30 px pill for an inline rename; the outline turns Primary while it has focus |
| `M3FocusRing` *(style)* | as any control's `FocusVisualStyle` | WPF draws a focus visual only when focus arrived from the keyboard, so the ring answers Tab and never a click |
| `m3:InputFilter.Kind` | `Text` `Number` `Integer`, on any `TextBox`; `InputFilter.Accepts(kind, text)` | Judges the text as it would read after the keystroke or paste, so `-` is taken only first and a second `.` is refused. A half-typed `-` or `1.` passes |
| `m3:ColorField` | `Color` `ShowAlpha` `IsReadOnly` `ReadOnlyCaption`, `ColorChanged` | A caption replaces the hex and locks the field: a colour that follows another one says so instead of offering an edit that would break the link. Without the alpha rail a pasted alpha is ignored |
| `m3:ValueListEditor` | `Load` `Values` `AddRow` `RemoveRow` `MoveRow` `Kind` `FixedLength` `SlotLabels` `Orientation` `MaxVisibleRows` `Placeholder` `AddText` `EmptyText` `IsValid`, `ValuesChanged` | A list is never a text box holding commas. `FixedLength` turns it into labelled slots that cannot be added, removed or moved. A row on screen leaves `Rows` when its exit ends, not at the call |
| `m3:DataTableEditor` | `Load(columns, records)` `Records` `AddRow` `RemoveRow` `CanAddRows` `CanRemoveRows` `IsSelectable` `MaxVisibleRows`, `ValuesChanged` | Columns are data, so one control serves every list of one shape. Equal columns line up with the header without a measuring pass |

Both editors scroll and virtualize inside themselves past `MaxVisibleRows`.

## Navigation

`NavigationRail` (80 px column), `NavigationBar` (equal-width row) and `NavigationDrawer` (full-width pills under
an optional `Headline`) share one selection model: they are `ListBox`es of `NavigationItem`, exactly one item is
selected once there is one, and clicking the selected item keeps it.

| `NavigationItem` | Behaviour |
|---|---|
| `Glyph` `SelectedGlyph` | M3 fills the active icon; `SelectedGlyph` is that filled one. A drawer item with no glyph is text only |
| `BadgeCount` `IsBadgeDot` `BadgeTone` | A count or a dot over the glyph's corner, Error by default |
| `IsBusy` | A spinner in the badge's place for work still running behind the destination; it hides the badge |

## Window

| Type | Knobs | Why it behaves that way |
|---|---|---|
| `m3:TitleBar` | `Title` `Icon` (default: the window's), `Header` `Actions` slots, `ShowMinimize` `ShowMaximize` `ShowClose`, `IsMaximized` | Installs the `WindowChrome` and keeps the system's minimize / maximize animations; a maximized window is inset by the resize border so the caption is not clipped |
| `WindowPlacement` | `Capture(window)` → string, `Apply(window, string)` before `Show` | A placement that would land off every screen is ignored, and a size below the window's minimum is raised to it |
| `TaskbarProgress` | `Begin(window)` → session: `SetNormal` `SetIndeterminate` `SetPaused` `SetError` `SetComplete`, `Dispose` | The oldest open session of a window drives its bar, so two producers never fight over it |
| `CenteredToolTip` | `Register()` once | Centres every stock `ToolTip` over its element, above when there is room |

## Modals

`Modal.Show(card, options)` returns a handle; `Close()` plays the exit and then removes the card. The
`ModalOptions` instance you pass stays live, so an async action can flip `DismissOnEsc` / `DismissOnScrimClick`
off for the duration of its work and the dialog cannot be dismissed out from under it — that is the whole
mechanism behind a confirm dialog that closes only once its operation finishes (see the *Async confirm dialog*
demo in the WPF gallery). Use `DismissHandler` instead when a bound flag, not the modal, owns the open state.

Each window's `ModalLayer` keeps its own stack: `Show(card)` picks the active window, `Show(owner, card)` the
window holding `owner`, and Esc or a scrim click closes only that window's top modal.

`Dialog.Show(owner, options)` is the ready-made dialog: it blocks, needs no `ModalLayer`, and returns a
`DialogChoice` — `Confirm`, `Secondary`, `Cancel` for a cancel, Esc or a click on the scrim, or `Failed` when
`ConfirmWork` throws.

| `DialogOptions` | Behaviour |
|---|---|
| `Title` `Body` / `Content` | A `Content` with a `MaxWidth` above 420 widens the dialog to it |
| `Tone` | Picks the glyph and its role: `Information`/OnSurfaceVariant for Neutral, `Information`/Primary for Accent (the default), `CheckCircle`/Success, `AlertOutline`/Warning, `AlertCircle`/Error |
| `ConfirmText` `CancelText` `SecondaryText` | With a secondary present, cancel steps down to a text button — three actions, three levels of emphasis |
| `Destructive` | The confirm turns error-tonal. Independent of `Tone`: an error notice still closes with a neutral button |
| `HoldToConfirm` | The confirm fills while held and commits once full; an early release shakes it. Enter does not reach it |
| `ConfirmWork` | Runs before the close; the confirm spins and nothing dismisses the dialog meanwhile. If it throws, `Show` answers `Failed` and `Show(owner, options, out failure)` hands back the exception, as WinForms answers `DialogResult.Abort` with `WorkFailure` |
| `SupportingAction` | A text button at the far end of the row that neither confirms nor cancels |
| `Links` | `OpenInNew`-marked links under the body that run without closing |

`DatePickerDialog.Show(owner, DatePickerOptions)` and `TimePickerDialog.Show(owner, TimePickerOptions)` answer with
the value or null for a cancel. Every label in their options is overridable; the culture picks the first weekday
and whether the time picker shows AM/PM, and a typed time is clamped into range.

## Motion

| Where | What moves |
|---|---|
| `ValueListEditor`, `DataTableEditor` rows | Added rows grow in, removed ones shrink out, a moved row slides from its old place |
| `Tabs` | The new tab's content fades in under the sliding indicator |
| Filter `Chip` | The check grows in on select and out on deselect, so the label shifts instead of jumping |
| `Badge`, `SearchBar` clear | Pop in when they appear |
| `TextField` | The supporting/error line fades in when its text changes |
| `StepChecklist` | A failed step fills red and its cross pops |
| `M3Combo` items | The selected fill fades in and out |

`Motion.AnimationsEnabled = false` or Windows' *Show animations* off (`SystemParameters.ClientAreaAnimation`) makes
every state land at once. Template storyboards (hover/press layers, the `M3Combo` fill) can't read the switch and
keep their short fades; progress spinners keep spinning, since they report status

`Motion.SwapContent(control, newContent)` swaps a `ContentControl`'s content, cross-fading the presenter while
the control's width eases from the old size to the new one — for a toggle whose caption tracks the state it
drives. The width only animates when the control's own content dictates it: a stretched control, or one the app
gave an explicit `Width`, is sized by something other than its label, so only the cross-fade plays. Give a
toggle in a panel that also holds wider siblings an explicit `HorizontalAlignment` other than `Stretch` if you
want the width motion — WPF centers a stretched element the moment it is given a width, and the helper refuses
to animate rather than make it jump.

## Popup helpers

- `m3:CenterPopup.IsEnabled="True"` centers a popup over its trigger and places it below, flipping above when
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
| `m3:NoticeBanner` | `Tone` `Glyph` `Text` `Actions` `CanDismiss`, `Dismissed` | Never removes itself: only the host knows if it should return. An action goes in at 32 high and with no vertical margin — the row is one line, and a margin inside a centred slot shifts the button off the centre line |
| `m3:EmptyState` | `Glyph` `Title` `Text` `Action` | |
| `m3:PopupCard` | content = `MenuItemButton` rows | Grows to the opener's width (own `MinWidth` still wins): a menu narrower than its trigger reads as an unrelated panel |
| `m3:Skeleton` | `Width` `Height` `CornerRadius` | Shimmer runs on the render thread, so it moves while the dispatcher rebuilds; stops itself on hide and on unload |
| `m3:Busy.IsBusy` | on `PathIcon` — swaps the glyph for the spinner | Inert elsewhere, so a template can trigger off it (`IconToggle` does) |
| `m3:Busy.SpinWhileVisible` | on an already-spinner glyph | Drops the clock the moment it hides |
| `m3:StepChecklist` | `Steps` `ActiveIndex` `ErrorIndex`, `Complete()` `CompleteAsync(hold)` | Moving `ActiveIndex` on morphs every passed step into done, so a jump still reads as each finishing. New `Steps` start over; moving on clears an error |


## Auto-hiding scrollbar

Opt in once, every bar follows. Look unchanged; this is only the timing — 1.5 s idle, 220 ms fade, and no show
on first layout, because the bar answers the user rather than announcing itself.

```xml
<Style TargetType="ScrollBar" BasedOn="{StaticResource M3ScrollBar}">
  <Setter Property="m3:AutoHideScroll.IsEnabled" Value="True" />
</Style>
```

Two rules, both silent when broken — the bar just turns into the native Windows one:

| # | Rule | Why |
|---|---|---|
| 1 | `BasedOn` the **keyed** `M3ScrollBar`, never `{x:Type ScrollBar}` | Application resources load deferred, so that key resolves to the style being declared |
| 2 | Declare it as a **direct child of `Application.Resources`**, not inside a dictionary merged into it | A `Style`'s `BasedOn` resolves while its own dictionary is parsed, and a dictionary merged into `Application.Resources` cannot see its siblings. An element-level dictionary is fine — there the lookup reaches the app through the tree |

## Mouse wheel

`WheelScroll.Register()` once at startup steps every pixel-scrolling `ScrollViewer` by the Windows wheel setting
at a browser's 33 px line, where WPF's fixed 16 px made the same setting feel half as fast. A viewer that scrolls
by item is left to WPF, and one with nothing to scroll lets the wheel reach the page around it.

## Code helpers

| Type | Use |
|---|---|
| `element.Themed(property, "Role")` | `DynamicResource` from code: a brush read once stays the old colour after `ThemeManager.Apply` |
| `NullToVisibilityConverter` (`IsInverted`) · `EqualsToVisibilityConverter` | An empty-state placeholder that steps aside, an image shown once it resolved, the mark on a menu's active row |

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
