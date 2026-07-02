# Material3.Wpf

**Material 3 (Material You)** for **WPF**. A runtime `M3Theme` generates the full M3 color-role scheme from one
seed color (HCT) and publishes it as live `DynamicResource` brushes, so the whole UI recolors on the fly — plus
themed M3 controls, an icon-set-agnostic `M3Icon`, motion helpers, an app-level `M3Modal` (scrim + card) and a
centered popup tooltip. Built on [`Material3.Core`](https://www.nuget.org/packages/Material3.Core).

![Material 3 color roles, dark theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/color-roles-dark.webp)

- Dynamic-color theming — one seed → live light/dark, every role as a `DynamicResource` brush
- Controls: filled / tonal / outlined / text / shiny buttons, text field, chips, menus, tooltip, scrollbar,
  progress, card, expander
- `M3Modal` app-level modal + scrim, `AnimatedPopup`, centered `Tip` tooltip, `M3Icon`, motion helpers

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

The control suite is growing toward WinForms parity.

**Full guide:** https://github.com/dary1337/material3-dotnet/blob/main/docs/wpf.md
&nbsp;·&nbsp; **Repo & demo galleries:** https://github.com/dary1337/material3-dotnet
