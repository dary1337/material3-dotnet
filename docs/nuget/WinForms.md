# Material3.WinForms

**Material 3 (Material You)** controls and design tokens for **Windows Forms**. Dynamic color from a single
seed, runtime light/dark switching, the full M3 type scale, elevation, state layers and motion — plus Visual
Studio designer support. No mandatory base form, no native dependencies. Built on
[`Material3.Core`](https://www.nuget.org/packages/Material3.Core).

![Material 3 controls, dark theme](https://raw.githubusercontent.com/dary1337/material3-dotnet/main/docs/img/buttons-dark.webp)

- Full M3 control catalog — buttons, text fields, chips, cards, lists, dialogs, snackbar, navigation, title bar
- Designer support: toolbox + property grid + a design surface that paints with the live theme
- `MaterialThemeManager` drives `Seed` / `Variant` / `IsDark` straight from the property grid

```
dotnet add package Material3.WinForms
```

```csharp
using Material3.WinForms.Theming;

ThemeManager.Apply(MaterialTheme.FromSeed(Color.FromArgb(0x67, 0x50, 0xA4)), isDark: true);
```

**Full guide:** https://github.com/dary1337/material3-dotnet/blob/main/docs/winforms.md
&nbsp;·&nbsp; **Repo & demo galleries:** https://github.com/dary1337/material3-dotnet
