#!/usr/bin/env python3
"""WPF reference shooter runner (spec section 13).

Renders the scenes in tools/wpf-reference/scenes with real WPF under Wine, for every family and scheme in
families.json, into <out>/<scene>/<family>.<scheme>.<state>.png plus a layout dump JSON per shot.

For each family and scheme it:
  1. writes the AvaWpf Layer 0 snapshot (SystemColors, system fonts, metrics) into the Wine prefix registry,
  2. starts WpfShooter.exe under Wine on a headless X server,
  3. answers the shooter's hover/pressed requests by moving the pointer with xdotool,
  4. checks that WPF read the snapshot colors back.

Paths (argument, then environment, then default):
  --root      AVAWPF_ROOT   the repository root (default: two levels above this script)
  --prefix    WINEPREFIX    the Wine prefix (default: $AVAWPF_ROOT/artifacts/wineprefix)
  --wpf-src   WPF_SRC       a dotnet/wpf checkout, optional, used to check the theme dictionary names
                            (default: $AVAWPF_ROOT/../wpf when it exists)
  --out                     the output folder (default: $AVAWPF_ROOT/tools/wpf-reference/out, gitignored)
  --dotnet    DOTNET        the dotnet executable (default: dotnet on PATH, then $DOTNET_ROOT/dotnet, then ~/.dotnet/dotnet)

Fallback: if WPF cannot run under Wine on a machine, set --vm-host (AVAWPF_WPF_VM) to an SSH host of a Windows VM
with the repository on a shared folder (--vm-root, AVAWPF_WPF_VM_ROOT). See README.md, "Windows VM fallback".
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import signal
import struct
import subprocess
import sys
import tempfile
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent

# WPF SystemColors role -> HKCU\Control Panel\Colors value name. Roles not listed use the same name.
ROLE_TO_REGISTRY = {
    "Control": "ButtonFace",
    "ControlLight": "ButtonLight",
    "ControlLightLight": "ButtonHilight",
    "ControlDark": "ButtonShadow",
    "ControlDarkDark": "ButtonDkShadow",
    "ControlText": "ButtonText",
    "Desktop": "Background",
    "ActiveCaptionText": "TitleText",
    "ActiveCaption": "ActiveTitle",
    "InactiveCaption": "InactiveTitle",
    "InactiveCaptionText": "InactiveTitleText",
    "GradientActiveCaption": "GradientActiveTitle",
    "GradientInactiveCaption": "GradientInactiveTitle",
    "Highlight": "Hilight",
    "HighlightText": "HilightText",
    "HotTrack": "HotTrackingColor",
    "Info": "InfoWindow",
    "InfoText": "InfoText",
    "MenuHighlight": "MenuHilight",
}

# Snapshot font prefix -> HKCU\Control Panel\Desktop\WindowMetrics LOGFONT value.
FONT_METRICS = {
    "Message": "MessageFont",
    "Menu": "MenuFont",
    "Status": "StatusFont",
    "Caption": "CaptionFont",
    "SmallCaption": "SmCaptionFont",
    "Icon": "IconFont",
}

# Snapshot number -> WindowMetrics value (stored in twips: -15 per pixel).
SIZE_METRICS = {
    "VerticalScrollBarWidth": "ScrollWidth",
    "HorizontalScrollBarHeight": "ScrollHeight",
    "CaptionHeight": "CaptionHeight",
    "CaptionWidth": "CaptionWidth",
    "SmallCaptionHeight": "SmCaptionHeight",
    "SmallCaptionWidth": "SmCaptionWidth",
    "MenuHeight": "MenuHeight",
    "BorderWidth": "BorderWidth",
}

# Fonts copied into the prefix from src/AvaWpf.Fonts/Assets/Fonts: file -> registry display name.
# Wine has its own Tahoma; the repository's copies are the ones AvaWpf bundles, so both sides use the same files.
FONT_FILES = {
    "selawk.ttf": "Selawik (TrueType)",
    "selawkb.ttf": "Selawik Bold (TrueType)",
    "selawksb.ttf": "Selawik Semibold (TrueType)",
    "selawkl.ttf": "Selawik Light (TrueType)",
    "tahoma.ttf": "Tahoma (TrueType)",
    "tahomabd.ttf": "Tahoma Bold (TrueType)",
}

def clear_use_typo_metrics(font: bytes) -> bytes:
    """Clears USE_TYPO_METRICS (OS/2 fsSelection bit 7). Wine's Tahoma sets it and Microsoft's does not, so with it
    WPF lays Tahoma out at 1 em (11 px lines at 8 pt) instead of Windows' 1.207 em. AvaWpf.Fonts clears the same bit in
    memory, so both sides use Microsoft's line height."""
    import struct
    data = bytearray(font)
    num_tables = struct.unpack_from(">H", data, 4)[0]
    for i in range(num_tables):
        tag, _, offset, length = struct.unpack_from(">4sIII", data, 12 + i * 16)
        if tag == b"OS/2" and length >= 64:
            selection = struct.unpack_from(">H", data, offset + 62)[0]
            struct.pack_into(">H", data, offset + 62, selection & ~(1 << 7))
    return bytes(data)


# Fonts that must not be in the prefix. AvaWpf's ms_sans_serif.ttf holds only embedded bitmaps (its outlines are
# empty). WPF never draws embedded bitmaps, so with that file WPF would draw no text at all.
FONT_FILES_REMOVED = ["ms_sans_serif.ttf"]

FONT_SUBSTITUTES = {
    # Segoe UI is not redistributable: Selawik stands in for it, as in AvaWpf.Fonts.
    "Segoe UI": "Selawik",
    "Segoe UI Semibold": "Selawik Semibold",
    "Segoe UI Light": "Selawik Light",
    "Segoe UI Bold": "Selawik",
    # WPF cannot use the bitmap MS Sans Serif. On Windows, WPF gets the outline Microsoft Sans Serif instead (the
    # "MS Sans Serif outline" diff in triage.toml). That font is not redistributable either; Tahoma stands in.
    "MS Sans Serif": "Tahoma",
    "Microsoft Sans Serif": "Tahoma",
}


def log(msg: str) -> None:
    print(f"[run.py] {msg}", flush=True)


# --------------------------------------------------------------------------------------------------------------------
# Snapshots


def parse_snapshots(path: Path) -> dict[str, dict]:
    """Parses Snapshots.g.cs: name -> {"colors": {role: 0xAARRGGBB}, "numbers": {...}, "strings": {...}}."""
    text = path.read_text(encoding="utf-8")
    result: dict[str, dict] = {}
    starts = [(m.start(), m.group(1)) for m in re.finditer(r'\["([A-Za-z0-9.]+)"\] = new SystemSnapshot\(', text)]
    for i, (pos, name) in enumerate(starts):
        end = starts[i + 1][0] if i + 1 < len(starts) else len(text)
        body = text[pos:end]
        blocks = re.split(r"new Dictionary<string, (?:uint|double|string)>", body)
        if len(blocks) < 4:
            raise ValueError(f"cannot parse snapshot {name}")
        colors = {k: int(v, 16) for k, v in re.findall(r'\["(\w+)"\] = 0x([0-9A-Fa-f]{8})', blocks[1])}
        numbers = {k: float(v) for k, v in re.findall(r'\["(\w+)"\] = (-?[0-9.]+)', blocks[2])}
        strings = dict(re.findall(r'\["(\w+)"\] = "([^"]*)"', blocks[3]))
        result[name] = {"colors": colors, "numbers": numbers, "strings": strings}
    if not result:
        raise ValueError(f"no snapshots found in {path}")
    return result


def first_face(families: str) -> str:
    """The first real face of a snapshot font list ("Segoe UI, fonts:AvaWpf#Selawik, sans-serif" -> "Segoe UI")."""
    for part in families.split(","):
        part = part.strip()
        if part and not part.startswith("fonts:") and part.lower() not in ("sans-serif", "serif", "monospace"):
            return part
    return "Tahoma"


# --------------------------------------------------------------------------------------------------------------------
# Registry


def reg_str(value: str) -> str:
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def logfont(face: str, height_px: int, weight: int, italic: bool) -> str:
    """A LOGFONTW (92 bytes) as a .reg hex: value."""
    face_bytes = face.encode("utf-16-le")[:62].ljust(64, b"\0")
    data = struct.pack("<iiiiiBBBBBBBB", -height_px, 0, 0, 0, weight, 1 if italic else 0, 0, 0, 1, 0, 0, 0, 0) + face_bytes
    return "hex:" + ",".join(f"{b:02x}" for b in data)


def snapshot_reg(snapshot: dict, scale: float) -> str:
    """The .reg text for one snapshot at one scale. Fonts and sizes stay at 96 DPI: Wine scales them to the DPI."""
    lines = ["REGEDIT4", "", r"[HKEY_CURRENT_USER\Control Panel\Colors]"]
    for role, argb in sorted(snapshot["colors"].items()):
        name = ROLE_TO_REGISTRY.get(role, role)
        lines.append(f'{reg_str(name)}="{(argb >> 16) & 255} {(argb >> 8) & 255} {argb & 255}"')
    # Wine also has ButtonAlternateFace; keep it with the face color.
    if "Control" in snapshot["colors"]:
        c = snapshot["colors"]["Control"]
        lines.append(f'"ButtonAlternateFace"="{(c >> 16) & 255} {(c >> 8) & 255} {c & 255}"')

    nums, strs = snapshot["numbers"], snapshot["strings"]
    lines += ["", r"[HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics]"]
    for prefix, value in FONT_METRICS.items():
        family = strs.get(prefix + "FontFamily") or strs.get("MessageFontFamily", "Tahoma")
        size = nums.get(prefix + "FontSize", nums.get("MessageFontSize", 11))
        weight = int(nums.get(prefix + "FontWeight", 400))
        italic = strs.get(prefix + "FontStyle", "Normal") == "Italic"
        lines.append(f"{reg_str(value)}={logfont(first_face(family), round(size), weight, italic)}")
    for number, value in SIZE_METRICS.items():
        if number in nums:
            lines.append(f'{reg_str(value)}="{-15 * round(nums[number])}"')

    high_contrast = nums.get("HighContrast", 0) >= 1
    lines += ["", r"[HKEY_CURRENT_USER\Control Panel\Accessibility\HighContrast]",
              f'"Flags"="{126 | (1 if high_contrast else 0)}"']

    dpi = round(96 * scale)
    lines += ["", r"[HKEY_CURRENT_USER\Control Panel\Desktop]", f'"LogPixels"=dword:{dpi:08x}',
              '"FontSmoothing"="2"', '"FontSmoothingType"=dword:00000001',
              "", r"[HKEY_LOCAL_MACHINE\System\CurrentControlSet\Hardware Profiles\Current\Software\Fonts]",
              f'"LogPixels"=dword:{dpi:08x}']
    return "\r\n".join(lines) + "\r\n"

# Snapshot fonts that neither Wine nor AvaWpf.Fonts has (the Eggplant, Red White and Blue and Pumpkin-era schemes use
# Times New Roman; Luna captions use Trebuchet MS). Without a face, WPF stops with a fail-fast in FontFamily. AvaWpf
# gets these from fontconfig, so the prefix gets the same host font: fc-match picks the file, which is copied in and
# registered as a substitute.
HOST_FONT_FAMILIES = ["Times New Roman", "Trebuchet MS"]


def host_fonts() -> dict[str, tuple[str, Path]]:
    """family -> (host family, host file) from fc-match, for the HOST_FONT_FAMILIES that resolve to a TrueType file."""
    found = {}
    if shutil.which("fc-match") is None:
        return found
    for family in HOST_FONT_FAMILIES:
        r = subprocess.run(["fc-match", "-f", "%{family[0]}|%{file}", family], capture_output=True, text=True)
        if r.returncode != 0 or "|" not in r.stdout:
            continue
        host_family, file = r.stdout.split("|", 1)
        path = Path(file)
        if path.suffix.lower() == ".ttf" and path.exists():
            found[family] = (host_family, path)
    return found


def setup_reg(glyph_font: bool, host: dict[str, tuple[str, Path]] | None = None) -> str:
    """The one-time .reg text: Windows 10, fonts, substitutes, no Wine visual style, no crash dialog."""
    files = dict(FONT_FILES)
    substitutes = dict(FONT_SUBSTITUTES)
    if glyph_font:
        files[GLYPH_FONT_FILE] = GLYPH_FONT_FAMILY + " (TrueType)"
        substitutes["Segoe Fluent Icons"] = GLYPH_FONT_FAMILY
        substitutes["Segoe MDL2 Assets"] = GLYPH_FONT_FAMILY
    for family, (host_family, path) in (host or {}).items():
        files["host_" + path.name] = host_family + " (TrueType)"
        substitutes[family] = host_family
    lines = ["REGEDIT4", "",
             r"[HKEY_CURRENT_USER\Software\Wine]", '"Version"="win10"', "",
             # Wine's own visual style (light.msstyles) is not used: WPF draws everything itself.
             r"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ThemeManager]", '"ThemeActive"="0"', "",
             r"[HKEY_CURRENT_USER\Software\Wine\WineDbg]", '"ShowCrashDialog"=dword:00000000', "",
             # WPF's own hardware acceleration switch; the shooter also sets RenderMode.SoftwareOnly.
             r"[HKEY_CURRENT_USER\Software\Microsoft\Avalon.Graphics]", '"DisableHWAcceleration"=dword:00000001', "",
             r"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\Fonts]"]
    for file, display in files.items():
        lines.append(f"{reg_str(display)}={reg_str(file)}")
    lines.append('"MS Sans Serif (TrueType)"=-')
    lines += ["", r"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\FontSubstitutes]"]
    for face, sub in substitutes.items():
        lines.append(f"{reg_str(face)}={reg_str(sub)}")
    # Wine's DirectWrite (which WPF uses) reads its family replacements here, not from FontSubstitutes.
    lines += ["", r"[HKEY_CURRENT_USER\Software\Wine\Fonts\Replacements]"]
    for face, sub in substitutes.items():
        lines.append(f"{reg_str(face)}={reg_str(sub)}")
    return "\r\n".join(lines) + "\r\n"


# --------------------------------------------------------------------------------------------------------------------
# Wine and X


class Env:
    def __init__(self, args: argparse.Namespace):
        self.root = Path(args.root or os.environ.get("AVAWPF_ROOT") or HERE.parent.parent).resolve()
        self.prefix = Path(args.prefix or os.environ.get("WINEPREFIX") or self.root / "artifacts" / "wineprefix").resolve()
        wpf = args.wpf_src or os.environ.get("WPF_SRC")
        self.wpf_src = Path(wpf).resolve() if wpf else (self.root.parent / "wpf")
        self.out = Path(args.out).resolve() if args.out else self.root / "tools" / "wpf-reference" / "out"
        self.wine = args.wine or os.environ.get("WINE") or "wine"
        self.dotnet = find_dotnet(args.dotnet)
        self.display: str | None = None
        self.xvfb: subprocess.Popen | None = None

    def wine_env(self, boot: bool = False) -> dict[str, str]:
        env = dict(os.environ)
        env["WINEPREFIX"] = str(self.prefix)
        env["WINEDEBUG"] = env.get("WINEDEBUG", "-all")
        if boot:
            # No Mono/Gecko install prompts while creating the prefix. Only then: Wine's PE loader needs its builtin
            # mscoree to load the shooter's ReadyToRun assemblies, so it must not be disabled for the shooter itself.
            env["WINEDLLOVERRIDES"] = "mscoree=;mshtml="
        if self.display:
            env["DISPLAY"] = self.display
        return env


def find_dotnet(arg: str | None) -> str:
    for candidate in (arg, os.environ.get("DOTNET"), shutil.which("dotnet"),
                      os.path.join(os.environ["DOTNET_ROOT"], "dotnet") if os.environ.get("DOTNET_ROOT") else None,
                      str(Path.home() / ".dotnet" / "dotnet")):
        if candidate and Path(candidate).exists():
            return candidate
    return "dotnet"


def win_path(p: Path) -> str:
    """A Unix path as a Wine path on the Z: drive (Wine maps Z: to /)."""
    return "Z:" + str(p).replace("/", "\\")


def start_xvfb(env: Env, display: str | None, use_display: bool) -> None:
    if display:
        env.display = display
        log(f"using X display {display}")
        return
    if use_display and os.environ.get("DISPLAY"):
        env.display = os.environ["DISPLAY"]
        log(f"using X display {env.display} from $DISPLAY")
        return
    if not shutil.which("Xvfb"):
        sys.exit("Xvfb not found; install it or pass --display")
    for n in range(91, 200):
        if not Path(f"/tmp/.X11-unix/X{n}").exists() and not Path(f"/tmp/.X{n}-lock").exists():
            break
    else:
        sys.exit("no free X display number")
    env.display = f":{n}"
    env.xvfb = subprocess.Popen(["Xvfb", env.display, "-screen", "0", "1600x1200x24", "-nolisten", "tcp"],
                                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    for _ in range(100):
        if Path(f"/tmp/.X11-unix/X{n}").exists():
            break
        time.sleep(0.05)
    log(f"started Xvfb on {env.display}")


def stop_xvfb(env: Env) -> None:
    if env.xvfb:
        env.xvfb.send_signal(signal.SIGTERM)
        env.xvfb.wait(timeout=10)


def run_wine(env: Env, *args: str, check: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run([env.wine, *args], env=env.wine_env(), check=check,
                          stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def regedit(env: Env, text: str) -> None:
    with tempfile.NamedTemporaryFile("w", suffix=".reg", delete=False, encoding="ascii", newline="") as f:
        f.write(text)
        path = Path(f.name)
    try:
        run_wine(env, "regedit", "/S", win_path(path))
    finally:
        path.unlink(missing_ok=True)


GLYPH_FONT_FILE = "avawpf_segoe_glyphs.ttf"
GLYPH_FONT_FAMILY = "AvaWpf Segoe Glyphs"


def make_glyph_font(env: Env, dst: Path) -> bool:
    """Builds the prefix's stand-in for Segoe Fluent Icons / Segoe MDL2 Assets.

    WPF Fluent draws its check marks and chevrons with Segoe Fluent Icons codepoints. Without that font, WPF's glyph
    fallback under Wine ends in a fail-fast inside its line layout. AvaWpf draws the same glyphs from its bundled
    AvaWpfFluentGlyphs.ttf at other codepoints (tools/gen-icons/segoe-to-fluentui.toml, FluentGlyph.g.cs). This copies
    that font, adds the Segoe codepoints for the same glyphs, and renames it, so WPF and AvaWpf draw the same shapes.
    Needs fontTools; returns False without it.
    """
    try:
        import tomllib
        from fontTools.ttLib import TTFont
    except ImportError:
        log("warning: fontTools is missing (pip install --user fonttools); Fluent glyphs will not render")
        return False
    src = env.root / "src" / "AvaWpf.Fonts" / "Assets" / "Fonts" / "AvaWpfFluentGlyphs.ttf"
    table = tomllib.loads((env.root / "tools" / "gen-icons" / "segoe-to-fluentui.toml").read_text(encoding="utf-8"))
    consts = dict(re.findall(r'public const string (\w+) = "\\u([0-9A-Fa-f]{4,5})"',
                             (env.root / "src" / "AvaWpf.Theme" / "Glyphs" / "FluentGlyph.g.cs").read_text(encoding="utf-8")))
    font = TTFont(str(src))
    best = font.getBestCmap()
    added = {}
    for section in table.values():
        for segoe, entry in section.items():
            ava = consts.get(entry["name"])
            if ava and int(ava, 16) in best:
                added[int(segoe, 16)] = best[int(ava, 16)]
    for sub in font["cmap"].tables:
        if sub.isUnicode():
            sub.cmap.update(added)
    for rec in font["name"].names:
        if rec.nameID in (1, 4, 16):
            rec.string = GLYPH_FONT_FAMILY
        elif rec.nameID == 6:
            rec.string = GLYPH_FONT_FAMILY.replace(" ", "")
    font.save(str(dst))
    return True


def init_prefix(env: Env) -> None:
    """Creates the prefix when missing, then installs fonts and the one-time registry settings (idempotent)."""
    if not (env.prefix / "system.reg").exists():
        log(f"creating Wine prefix {env.prefix}")
        env.prefix.mkdir(parents=True, exist_ok=True)
        subprocess.run([env.wine, "wineboot", "-i"], env=env.wine_env(boot=True), check=True,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        subprocess.run(["wineserver", "-w"], env=env.wine_env(boot=True), check=False)
    fonts_src = env.root / "src" / "AvaWpf.Fonts" / "Assets" / "Fonts"
    fonts_dst = env.prefix / "drive_c" / "windows" / "Fonts"
    fonts_dst.mkdir(parents=True, exist_ok=True)
    for file in FONT_FILES:
        src = fonts_src / file
        if not src.exists():
            sys.exit(f"font {src} missing")
        dst = fonts_dst / file
        data = src.read_bytes()
        if file.startswith("tahoma"):
            data = clear_use_typo_metrics(data)
        if not dst.exists() or dst.read_bytes() != data:
            dst.write_bytes(data)
    for file in FONT_FILES_REMOVED:
        (fonts_dst / file).unlink(missing_ok=True)
    glyphs = make_glyph_font(env, fonts_dst / GLYPH_FONT_FILE)
    host = host_fonts()
    for _, path in host.values():
        dst = fonts_dst / ("host_" + path.name)
        if not dst.exists() or dst.stat().st_size != path.stat().st_size:
            shutil.copyfile(path, dst)
    regedit(env, setup_reg(glyphs, host))


# --------------------------------------------------------------------------------------------------------------------
# Shooter


def build_shooter(env: Env) -> Path:
    proj = HERE / "WpfShooter" / "WpfShooter.csproj"
    log("building WpfShooter (self-contained win-x64)")
    subprocess.run([env.dotnet, "build", str(proj), "-c", "Release", "-r", "win-x64", "--self-contained", "-nologo",
                    "-v", "quiet"], check=True)
    exe = HERE / "WpfShooter" / "bin" / "Release" / "net10.0-windows" / "win-x64" / "WpfShooter.exe"
    if not exe.exists():
        sys.exit(f"{exe} not found after build")
    return exe


def xdotool(env: Env, *args: str) -> None:
    subprocess.run(["xdotool", *args], env={**os.environ, "DISPLAY": env.display or ""}, check=True,
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


# The shooter window sits at (32, 32). The pointer parks on the window's top-left pixel: inside the window, so WPF sees
# the pointer leave the control (Wine does not always report leaving the window), and off the control, which is
# centered with a margin.
PARK = ("33", "33")


def element_center(layout_path: Path, target: str) -> tuple[int, int]:
    layout = json.loads(layout_path.read_text(encoding="utf-8"))
    for e in layout["elements"]:
        if e["id"] == target or (e["name"] == target and not e["templatePart"]):
            return e["screen"]["centerX"], e["screen"]["centerY"]
    raise KeyError(f"{target} not in {layout_path}")


def run_shooter(env: Env, exe: Path, entry: dict, snapshot: dict, scenes: list[str], timeout: float) -> int:
    ipc = env.out / "_ipc"
    shutil.rmtree(ipc, ignore_errors=True)
    ipc.mkdir(parents=True)
    args = [env.wine, str(exe),
            "--family", entry["family"], "--scheme", entry["scheme"],
            "--dictionary", entry["dictionary"], "--surface", entry["surface"], "--accent", entry["accent"],
            "--scenes", win_path(HERE / "scenes"), "--out", win_path(env.out), "--ipc", win_path(ipc)]
    if scenes:
        args += ["--scene", ",".join(scenes)]
    if snapshot["numbers"].get("HighContrast", 0) >= 1:
        # Wine does not report the registry flag through SPI_GETHIGHCONTRAST; the shooter primes WPF's cache.
        args.append("--high-contrast")
    xdotool(env, "mousemove", *PARK)
    proc = subprocess.Popen(args, env=env.wine_env(), stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
    handled: set[int] = set()
    deadline = time.monotonic() + timeout
    while proc.poll() is None:
        if time.monotonic() > deadline:
            proc.kill()
            log(f"shooter timed out after {timeout:.0f} s")
            return 124
        for req in sorted(ipc.glob("request.*.json")):
            seq = int(req.name.split(".")[1])
            if seq in handled:
                continue
            handled.add(seq)
            r = json.loads(req.read_text(encoding="utf-8"))
            layout = Path(r["layout"].replace("\\", "/").removeprefix("Z:"))
            x, y = element_center(layout, r["target"])
            xdotool(env, "mousemove", "--sync", str(x), str(y))
            if r["action"] == "press":
                xdotool(env, "mousedown", "1")
            (ipc / f"ack.{seq}").write_text("ack")
            while not (ipc / f"done.{seq}").exists() and proc.poll() is None:
                time.sleep(0.02)
            if r["action"] == "press":
                xdotool(env, "mouseup", "1")
            xdotool(env, "mousemove", *PARK)
            (ipc / f"parked.{seq}").write_text("parked")
        time.sleep(0.02)
    stderr = proc.stderr.read() if proc.stderr else ""
    for line in stderr.splitlines():
        if "[error]" in line or "[warn]" in line or (proc.returncode != 0 and "MESA" not in line):
            log("  shooter: " + line)
    return proc.returncode


def verify_colors(env: Env, entry: dict, snapshot: dict, scale: float) -> list[str]:
    """Compares the SystemColors WPF read with the snapshot run.py wrote."""
    suffix = "" if abs(scale - 1) < 0.01 else f"@{scale:g}x"
    path = env.out / "_system" / f"{entry['family']}.{entry['scheme']}.system{suffix}.json"
    if not path.exists():
        return [f"{path} missing"]
    dump = json.loads(path.read_text(encoding="utf-8"))
    problems = []
    for role, argb in snapshot["colors"].items():
        got = dump["colors"].get(role)
        want = f"#FF{argb & 0xFFFFFF:06X}"
        if got and got.upper() != want:
            problems.append(f"{role}: WPF read {got}, snapshot {want}")
    return problems


def check_dictionaries(env: Env, entries: list[dict]) -> None:
    themes = env.wpf_src / "src" / "Microsoft.DotNet.Wpf" / "src" / "Themes"
    if not themes.is_dir():
        return
    for e in entries:
        m = re.match(r"/(PresentationFramework\.\w+);component/(.+)$", e["dictionary"])
        if not m:
            continue
        folder = themes / m.group(1)
        wanted = m.group(2).lower()
        found = any(str(p.relative_to(folder)).lower() == wanted for p in folder.rglob("*.xaml"))
        if not found:
            log(f"warning: {e['dictionary']} not found under {folder}")


def run_vm(args: argparse.Namespace, entries: list[dict]) -> int:
    """Windows VM fallback: runs the same shooter over SSH in a Windows guest that sees the repository on a share."""
    host = args.vm_host or os.environ.get("AVAWPF_WPF_VM")
    root = args.vm_root or os.environ.get("AVAWPF_WPF_VM_ROOT")
    if not host or not root:
        sys.exit("--vm needs --vm-host/AVAWPF_WPF_VM and --vm-root/AVAWPF_WPF_VM_ROOT")
    exe = root.rstrip("\\") + r"\tools\wpf-reference\WpfShooter\bin\Release\net10.0-windows\win-x64\WpfShooter.exe"
    rc = 0
    for e in entries:
        cmd = [exe, "--family", e["family"], "--scheme", e["scheme"], "--dictionary", e["dictionary"],
               "--surface", e["surface"], "--accent", e["accent"],
               "--scenes", root + r"\tools\wpf-reference\scenes", "--out", root + r"\tools\wpf-reference\out"]
        if args.scene:
            cmd += ["--scene", ",".join(args.scene)]
        # No --ipc: the VM path renders static states only (pointer input would need a guest-side agent).
        log(f"VM {host}: {e['family']}.{e['scheme']} (SystemColors in the guest are not changed)")
        rc |= subprocess.run(["ssh", host, subprocess.list2cmdline(cmd)]).returncode
    return rc


# --------------------------------------------------------------------------------------------------------------------


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--scene", action="append", help="scene name (repeatable or comma separated; default: all)")
    ap.add_argument("--family", action="append", help="family (repeatable or comma separated; default: all)")
    ap.add_argument("--scheme", action="append", help="scheme id (repeatable or comma separated; default: all)")
    ap.add_argument("--scale", default="1", help="comma-separated display scales, e.g. 1,1.5 (default: 1)")
    ap.add_argument("--out", help="output folder")
    ap.add_argument("--root", help="repository root (AVAWPF_ROOT)")
    ap.add_argument("--prefix", help="Wine prefix (WINEPREFIX)")
    ap.add_argument("--wpf-src", help="dotnet/wpf checkout (WPF_SRC)")
    ap.add_argument("--wine", help="wine executable (WINE)")
    ap.add_argument("--dotnet", help="dotnet executable (DOTNET)")
    ap.add_argument("--display", help="X display to use instead of starting Xvfb")
    ap.add_argument("--use-display", action="store_true", help="use $DISPLAY instead of starting Xvfb")
    ap.add_argument("--no-build", action="store_true", help="do not build WpfShooter")
    ap.add_argument("--clean", action="store_true", help="delete the output folder first")
    ap.add_argument("--timeout", type=float, default=600, help="seconds per family and scheme (default: 600)")
    ap.add_argument("--list", action="store_true", help="list families and schemes, then exit")
    ap.add_argument("--vm", action="store_true", help="use the Windows VM fallback instead of Wine")
    ap.add_argument("--vm-host", help="SSH host of the Windows VM (AVAWPF_WPF_VM)")
    ap.add_argument("--vm-root", help="repository path inside the VM (AVAWPF_WPF_VM_ROOT)")
    args = ap.parse_args()

    def split(values: list[str] | None) -> list[str]:
        return [v for item in (values or []) for v in item.split(",") if v]

    args.scene = split(args.scene)
    families = {f.lower() for f in split(args.family)}
    schemes = {s.lower() for s in split(args.scheme)}
    scales = [float(s) for s in args.scale.split(",")]

    entries = json.loads((HERE / "families.json").read_text(encoding="utf-8"))["entries"]
    entries = [e for e in entries
               if (not families or e["family"].lower() in families) and (not schemes or e["scheme"].lower() in schemes)]
    if args.list:
        for e in entries:
            print(f"{e['family']}.{e['scheme']:<20} {e['dictionary']}  snapshot={e['snapshot']}")
        return 0
    if not entries:
        sys.exit("no family/scheme matches the filters (see --list)")
    if args.vm:
        return run_vm(args, entries)

    env = Env(args)
    snapshots = parse_snapshots(env.root / "src" / "AvaWpf.Theme" / "System" / "Snapshots.g.cs")
    check_dictionaries(env, entries)
    if args.clean:
        shutil.rmtree(env.out, ignore_errors=True)
    env.out.mkdir(parents=True, exist_ok=True)

    exe = HERE / "WpfShooter" / "bin" / "Release" / "net10.0-windows" / "win-x64" / "WpfShooter.exe"
    if not args.no_build:
        exe = build_shooter(env)
    for tool in ("xdotool", env.wine):
        if not shutil.which(tool):
            sys.exit(f"{tool} not found")

    start_xvfb(env, args.display, args.use_display)
    failures = []
    try:
        init_prefix(env)
        for scale in scales:
            for e in entries:
                snap = snapshots[e["snapshot"]]
                log(f"{e['family']}.{e['scheme']} (snapshot {e['snapshot']}, scale {scale:g})")
                regedit(env, snapshot_reg(snap, scale))
                rc = run_shooter(env, exe, e, snap, args.scene, args.timeout)
                problems = verify_colors(env, e, snap, scale)
                for p in problems:
                    log(f"  color check: {p}")
                if rc != 0 or problems:
                    failures.append(f"{e['family']}.{e['scheme']}@{scale:g}: exit {rc}, {len(problems)} color mismatches")
    finally:
        stop_xvfb(env)

    log(f"output: {env.out}")
    if failures:
        for f in failures:
            log("FAILED " + f)
        return 1
    log("all families rendered")
    return 0


if __name__ == "__main__":
    sys.exit(main())
