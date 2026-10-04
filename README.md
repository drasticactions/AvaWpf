# AvaWpf

AvaWpf is an experimental Avalonia theme and control set, based on [WPFs](https://github.com/dotnet/wpf) built-in themes. It covers

- Aero2
- AeroLite
- Aero
- Luna
- Classic
- Fluent

and includes Light, Dark, and High Contrast themes.

It's broken down into several libraries:

| Package | What |
|---|---|
| `AvaWpf.Theme` | The seven theme families, SystemColors/SystemParameters/SystemFonts, the ported WPF chrome, `ThemeScope`, `ThemeWindow` and `WindowFrame` |
| `AvaWpf.Controls` | ToolBar and ToolBarTray, StatusBar, ResizeGrip, ListView with GridView |
| `AvaWpf.Animations` | The theme motion, WPF's popup animations and the window open/close motion |
| `AvaWpf.DataGrid` | Themes for `Avalonia.Controls.DataGrid` |
| `AvaWpf.Ribbon` | A port of the WPF Ribbon |
| `AvaWpf.Fonts` | Open substitute fonts and the Fluent icon subset |
| `AvaWpf.Icons` | Optional: the full Fluent UI System Icons fonts and a name → codepoint class |

**NOTE**: This is not intended as a "drop-in" replacement for WPF, nor is it meant to be "pixel-perfect" or exact with Windows. It's more of a technical exercise. If you find yourself depending on this for whatever reason, you should fork it.

## Use

```xml
<Application xmlns:wpf="https://github.com/avawpf" ...>
  <Application.Styles>
    <wpf:AvaWpfTheme Theme="Luna" ColorScheme="Metallic" />
    <wpf:AvaWpfControlsTheme />
  </Application.Styles>
</Application>
```

```csharp
AppBuilder.Configure<App>().UsePlatformDetect().WithAvaWpfFonts().StartWithClassicDesktopLifetime(args);
```