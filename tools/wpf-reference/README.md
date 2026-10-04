# WPF reference shooter

This tool renders scenes with real WPF, in every WPF theme, so that you can compare AvaWpf with WPF side by side
(spec section 13). It runs under Wine on a Linux machine. It is not part of `AvaWpf.slnx` and does not run in CI.

| Path | Purpose |
|---|---|
| `WpfShooter/` | A .NET 10 `net10.0-windows` WPF app. It builds each scene, merges one WPF theme dictionary, and writes PNGs and layout dumps. |
| `WpfShooter/SceneBuilder.Wpf.cs` | The WPF loader for `scenes/*.json`. The AvaWpf gallery has the matching loader. |
| `scenes/*.json` | The scene definitions. |
| `families.json` | The families and schemes: WPF theme dictionary, AvaWpf snapshot, accent and background. |
| `run.py` | The runner. It prepares the Wine prefix, writes the SystemColors, starts the shooter, and supplies the pointer input. |
| `shoot-avawpf.sh` | Renders the AvaWpf side of every scene for every entry of `families.json` with the gallery. |
| `report.py` | The HTML report: WPF, AvaWpf and a diff heatmap per shot. |
| `triage.toml` | The hand triage of the diffs. `report.py` reads it. |

## Requirements

- Wine 10 or later (tested with Wine 11.18, wow64). No winetricks and no installed .NET runtime are necessary.
- `Xvfb` and `xdotool`.
- The .NET 10 SDK. `run.py` finds `dotnet` in `DOTNET`, on `PATH`, in `$DOTNET_ROOT`, or in `~/.dotnet`.
- Python 3.11 or later, with `fontTools` (for the Fluent glyph font). `report.py` also needs Pillow and numpy.
  To install them: `pip install --user fonttools pillow numpy`.

## Usage

```sh
# Render all scenes for all families and schemes into tools/wpf-reference/out
python3 tools/wpf-reference/run.py

# Render only some scenes, families or schemes
python3 tools/wpf-reference/run.py --scene button,checkbox --family Aero2,Classic --scheme Default,WindowsStandard

# List the families and schemes
python3 tools/wpf-reference/run.py --list

# Render at 150 % (the prefix DPI is set to 144 and the shooter restarts)
python3 tools/wpf-reference/run.py --family Classic --scheme WindowsStandard --scale 1,1.5

# Render the AvaWpf side into artifacts/avawpf-shots (all scenes, families and schemes; -j sets the parallel jobs)
tools/wpf-reference/shoot-avawpf.sh
tools/wpf-reference/shoot-avawpf.sh --scene button,checkbox --family Classic --scheme WindowsStandard

# Build the report (AvaWpf shots are optional: missing ones show as "not rendered")
python3 tools/wpf-reference/report.py
xdg-open artifacts/wpf-reference-report/index.html
```

A full WPF run (33 family and scheme entries, 19 scenes) takes approximately 15 minutes. `shoot-avawpf.sh` takes
approximately 2 minutes. The report also writes `summary.tsv` (one line per shot: scene, family, scheme, state, percent,
triage status and reason) and prints the mean percentage of each family.

### Paths

Each path comes from an argument, then an environment variable, then a default relative to the repository:

| Argument | Variable | Default |
|---|---|---|
| `--root` | `AVAWPF_ROOT` | two folders above `run.py` |
| `--prefix` | `WINEPREFIX` | `$AVAWPF_ROOT/artifacts/wineprefix` |
| `--wpf-src` | `WPF_SRC` | `$AVAWPF_ROOT/../wpf` (optional; used only to check the dictionary names in `families.json`) |
| `--out` | | `$AVAWPF_ROOT/tools/wpf-reference/out` (gitignored) |
| `--wine` | `WINE` | `wine` |
| `--dotnet` | `DOTNET` | see Requirements |
| `--display` | | none: `run.py` starts its own `Xvfb` on a free display. `--use-display` uses `$DISPLAY`. |

`report.py` takes `--wpf` (default `tools/wpf-reference/out`), `--avawpf` (default `artifacts/avawpf-shots`, written
by `shoot-avawpf.sh`, which runs the gallery with `--scene <folder or json> --out artifacts/avawpf-shots --theme X
--scheme Y --variant V --shot-prefix <family>.<scheme>`), `--out` (default
`artifacts/wpf-reference-report`), `--triage`, `--family`, `--threshold` (default 2.3) and `--zoom`.

## Output

```
out/<scene>/<family>.<scheme>.<state>.png     the shot (RenderTargetBitmap)
out/<scene>/<family>.<scheme>.<state>.json    the layout dump of the shot
out/_system/<family>.<scheme>.system.json     what WPF read from the prefix: SystemColors, SystemFonts, faces
out/_logs/<family>.<scheme>.log               the shooter log
```

At a scale other than 1, the names get a suffix, for example `Classic.WindowsStandard.normal@1.5x.png`.

The family names are `Aero2`, `AeroLite`, `Aero`, `Luna`, `Royale`, `Classic` and `Fluent`. The scheme names are
AvaWpf's `ColorSchemes` constants (`Default`, `NormalColor`, `Metallic`, `Homestead`, `WindowsStandard`, `Brick`…).
Three entries are variants and not schemes: `Classic.Dark` (the `Classic.Dark` snapshot), `Fluent.Dark` and
`Fluent.HighContrast`. The `avawpf` block of each entry in `families.json` gives the AvaWpf family, scheme and variant.

The gallery writes a layout dump next to each AvaWpf shot too: the target and every named element in its template, in
DIPs relative to the scene. With `AVAWPF_SCENE_DUMP_ALL=1` it also lists the unnamed elements and prints the target's
pseudo-classes. The gallery runs the dispatcher for 600 ms after the input of a hover, pressed or focused state, as the
shooter waits for theme animations, so Avalonia transitions finish.

The WPF layout dump records every named element and every named template part: its type, its bounds in DIPs relative to
the scene, and its bounds and center in screen pixels. A template part has the id `target/<part name>`.

## Scenes

The WPF loader (`SceneBuilder.Wpf.cs`) and the AvaWpf loader read the same schema:

```json
{
  "control": "Button",
  "width": 120,
  "height": 50,
  "margin": 8,
  "properties": { "Content": "Button", "Width": 75, "Height": 23 },
  "states": ["normal", "hover", "pressed", "focused", "default", "disabled"]
}
```

- `control`: the type name. It is the same in WPF and Avalonia (`Button`, `CheckBox`, `RadioButton`, `ToggleButton`,
  `RepeatButton`, `TextBox`, `ComboBox`, `ListBox`, `ProgressBar`, `Slider`, `ScrollBar`, `Expander`, `GroupBox`,
  `TabControl`, `Menu`, `TreeView`, `Label`).
- `width`, `height`: the client size in pixels. The control is centered with `margin` (default 8).
- `properties`: plain JSON values. The type converter of the property converts them. `Items` fills an ItemsControl
  with strings. For a TabControl, each string becomes a TabItem header.
- `states`:

  | State | Meaning |
  |---|---|
  | `normal` | As built |
  | `hover` | Real pointer over the control center |
  | `pressed` | Real pointer over the control center, left button down |
  | `focused` | `Keyboard.Focus` on the control, with focus visuals on |
  | `default` | `IsDefault = true` (Button) |
  | `checked` | `IsChecked = true` |
  | `disabled` | `IsEnabled = false` |

The scene name is the file name. The host background is the family surface from `families.json`:
`SystemColors.Control` for the XAML families, `ApplicationBackgroundBrush` for Fluent.

## How it works

### The shooter

`run.py` starts the shooter once for each family and scheme. The shooter:

1. Sets `RenderOptions.ProcessRenderMode = SoftwareOnly`.
2. Merges the theme dictionary from `families.json` into the application resources, for example
   `/PresentationFramework.Aero2;component/themes/aero2.normalcolor.xaml` or
   `/PresentationFramework.Classic;component/themes/classic.xaml`.
3. Sets the accent ramp explicitly (`SystemColors.AccentColor*Key`). WPF reads the accent from WinRT `UISettings`,
   which Wine does not have. The ramp uses the same HSL steps as AvaWpf's `AccentColors.Compute`. The accent is
   `#0078D4` for Fluent and `#0078D7` for the other families.
4. Shows a borderless window at (32, 32) that holds an `AdornerDecorator` and the scene. The adorner layer is inside
   the capture, so focus visuals are in the shots.
5. For each scene and state, builds the control, sets the static state, captures the decorator with
   `RenderTargetBitmap`, and writes the layout dump.

Keyboard focus stays in the scene host in every state except `focused`. Thus a default button shows as defaulted.
WPF shows focus visuals only after keyboard input. For the `focused` state, the shooter turns on WPF's internal
`KeyboardNavigation.AlwaysShowFocusVisual`. For all other states it turns it off.

### Pointer input

Hover and pressed need real input. The shooter and `run.py` use files in `out/_ipc/` for the handshake:

1. The shooter writes the layout dump and `request.<n>.json` (`hover` or `press`).
2. `run.py` moves the pointer to the control center from the layout dump (`xdotool mousemove`). For `press`, it also
   does `xdotool mousedown 1`. Then it writes `ack.<n>`.
3. The shooter waits until `IsMouseOver` (or `IsPressed`) becomes true, waits 600 ms for theme animations, captures,
   and writes `done.<n>`.
4. `run.py` releases the button, parks the pointer on the top-left pixel of the window, and writes `parked.<n>`.

The pointer parks inside the window: when the pointer leaves the window, Wine does not always tell WPF that the
mouse left.

### The Wine prefix

`run.py` creates the prefix on the first run (`wineboot -i`), then on every run:

- Sets the Windows version to Windows 10.
- Turns off Wine's own visual style (`ThemeManager\ThemeActive = 0`).
- Sets `HKCU\Software\Microsoft\Avalon.Graphics\DisableHWAcceleration = 1`.
- Copies the fonts that AvaWpf bundles from `src/AvaWpf.Fonts/Assets/Fonts` into `C:\windows\Fonts` and registers
  them: Selawik (all four weights), Tahoma and Tahoma Bold.
- Registers Selawik as the `Segoe UI` substitute, in `HKLM\...\FontSubstitutes` and also in
  `HKCU\Software\Wine\Fonts\Replacements`. Wine's DirectWrite, which WPF uses, reads the Wine key.
- Registers Tahoma as the `MS Sans Serif` and `Microsoft Sans Serif` substitute. WPF draws only outlines. AvaWpf's
  `ms_sans_serif.ttf` has only embedded bitmaps (its outlines are empty), so WPF draws no text with it, and `run.py`
  does not install it. On Windows, WPF also gets an outline font in place of the bitmap MS Sans Serif (Microsoft
  Sans Serif). This is the accepted "MS Sans Serif outline" difference in `triage.toml`.
- Copies the host's `fc-match` face for the snapshot fonts that neither side bundles (Times New Roman in Eggplant and
  Red, White and Blue; Trebuchet MS for Luna captions) and registers it as their substitute. Without a face WPF stops
  with a fail-fast in `FontFamily.FirstFontFamily`.
- Builds `avawpf_segoe_glyphs.ttf` and registers it as the `Segoe Fluent Icons` and `Segoe MDL2 Assets` substitute.
  This font is a copy of `AvaWpfFluentGlyphs.ttf` with the Segoe codepoints added, from
  `tools/gen-icons/segoe-to-fluentui.toml` and `FluentGlyph.g.cs`. WPF Fluent then draws the same check mark and
  chevrons as AvaWpf.

Before each family and scheme, `run.py` reads the snapshot from `src/AvaWpf.Theme/System/Snapshots.g.cs` and writes:

- The SystemColors into `HKCU\Control Panel\Colors`. The WPF role names map to the registry names as follows:
  Control → ButtonFace, ControlLight → ButtonLight, ControlLightLight → ButtonHilight, ControlDark → ButtonShadow,
  ControlDarkDark → ButtonDkShadow, ControlText → ButtonText, Desktop → Background, ActiveCaption → ActiveTitle,
  ActiveCaptionText → TitleText, InactiveCaption → InactiveTitle, InactiveCaptionText → InactiveTitleText,
  GradientActiveCaption → GradientActiveTitle, GradientInactiveCaption → GradientInactiveTitle,
  Highlight → Hilight, HighlightText → HilightText, HotTrack → HotTrackingColor, Info → InfoWindow,
  MenuHighlight → MenuHilight. The other roles use the same name.
- The system fonts (LOGFONT) and sizes into `HKCU\Control Panel\Desktop\WindowMetrics`: message, menu, status,
  caption, small caption and icon fonts, and the scroll bar, caption, menu and border sizes.
- The high contrast flag (`HKCU\Control Panel\Accessibility\HighContrast`). Wine does not report this flag through
  `SPI_GETHIGHCONTRAST`. Thus for a high contrast snapshot, `run.py` also passes `--high-contrast`, and the shooter
  sets WPF's cached `SystemParameters.HighContrast` before a theme reads it.
- The DPI (`LogPixels`) for the scale, and grayscale font smoothing.

After each run, `run.py` compares the SystemColors in `out/_system/<family>.<scheme>.system.json` with the snapshot.
A difference is a failure.

## Status of Phase 0 Spike B

WPF runs under Wine 11.18 with no native DLL overrides. The spike passes:

- Aero2 and Classic render Button and CheckBox correctly in every state, including real hover and pressed.
- WPF reads the SystemColors from the registry: the color check matches all snapshot colors for every family and
  scheme. Classic schemes (Windows Standard `#D4D0C8`, Brick, high contrast…) change the Classic rendering as
  expected.
- WPF reads the system fonts from the registry: Aero2 uses Segoe UI (Selawik) 12 px, and Classic uses Tahoma 11 px
  (MS Sans Serif in some schemes, which becomes Tahoma, see below), as in the snapshots.
- All seven families render, including Fluent (with the glyph font above).

### Problems found and their workarounds

| Problem | Workaround |
|---|---|
| `InvariantGlobalization` breaks WPF text: `MS.Internal.FontCache.MajorLanguages` needs the `en` culture. Wine has no `icu.dll`. | `System.Globalization.UseNls = true` in the project. The NLS APIs come from Wine. |
| With `WINEDLLOVERRIDES=mscoree=`, the shooter cannot load its ReadyToRun assemblies ("Module not found" for `System.Runtime.dll`). Wine's PE loader needs its builtin `mscoree`. | Use that override only for `wineboot -i`, so that Wine does not ask to install Mono. |
| WPF Fluent stops with a fail-fast in line layout when a `Segoe Fluent Icons` glyph has no font. | The substitute glyph font above. |
| Wine's default prefix has Wine's own colors (`ButtonFace` `#F5F5F5`), aliased text (no font smoothing) and Tahoma 8 pt system fonts. | `run.py` writes the snapshot colors, fonts and font smoothing before each family. |
| WPF draws no text in `MS Sans Serif` (Classic schemes) when the font is AvaWpf's bitmap-only `ms_sans_serif.ttf`. | Do not install it. Substitute Tahoma, as described above. |
| Wine reports high contrast as off, whatever the registry says. | `--high-contrast`, as described above. |
| When the pointer leaves the window, WPF can keep `IsMouseOver` true. | Park the pointer inside the window. The shooter also waits for `IsMouseOver` to become false before it captures a state without input. |

No D3D, `wpfgfx_cor3`, DirectWrite or `UIAutomationCore` workaround is necessary.

### Known limits

- The shooter captures only the control. Popups (ComboBox drop-downs, tooltips, menus) are separate windows and are
  not in the capture yet.
- A scale other than 1 sets the prefix DPI. `RenderTargetBitmap` then renders at that DPI, and WPF layout rounding
  uses it too.
- `KeyboardCues`, `DropShadow` and the animation settings in the snapshots are not written into the prefix.
- WPF has no dark Aero2, AeroLite, Aero, Luna or Royale. AvaWpf invents those (spec 7.6), so `families.json` has no
  WPF entry for them.

## Windows VM fallback

If WPF does not run under Wine on a machine, the spec fallback is a Windows 10 or 11 VM. In the VM, the repository
must be on a shared folder, and an SSH server must run:

```sh
export AVAWPF_WPF_VM=user@win-vm            # or --vm-host
export AVAWPF_WPF_VM_ROOT='Z:\AvaWpf'       # the repository path in the VM, or --vm-root
python3 tools/wpf-reference/run.py --vm --family Aero2
```

Build the shooter on the Linux side first (run `run.py` once, or `dotnet build` the project). The VM path renders only
static states. It does not change the SystemColors of the guest, and it does not supply pointer input. Use it only
when Wine fails.
