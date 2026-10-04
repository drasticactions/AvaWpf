#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
shoot.sh [OPTIONS]

Renders gallery pages to PNG with the headless platform (no display needed).

  --page NAME      gallery page, default Buttons; "all" renders every page
  --theme FAMILY   Aero2, AeroLite, Aero, Luna, Royale, Classic or Fluent; "all" renders every family
  --scheme NAME    color scheme (default: the family default)
  --variant NAME   Light, Dark or HighContrast, default Light
  --size WxH       window size, default 1200x800
  --out DIR        output folder, default artifacts/shots
  -h               this help

Files are named <out>/<theme>.<scheme>.<variant>.<page>.png.
TEXT
}

page=Buttons theme=Aero2 scheme= variant=Light size=1200x800 out="$root/artifacts/shots"
while [ $# -gt 0 ]; do
    case "$1" in
        --page) page=${2:?}; shift 2 ;;
        --theme) theme=${2:?}; shift 2 ;;
        --scheme) scheme=${2:?}; shift 2 ;;
        --variant) variant=${2:?}; shift 2 ;;
        --size) size=${2:?}; shift 2 ;;
        --out) out=${2:?}; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument '$1'" >&2; exit 1 ;;
    esac
done

pages=("$page")
if [ "$page" = all ]; then
    pages=(Buttons Toggles Text Lists Trees Tabs Menus Range Scrolling Dates Containers ToolBar StatusBar ListView "Avalonia only" Windows Typography SystemColors Compare)
fi

themes=("$theme")
if [ "$theme" = all ]; then
    themes=(Aero2 AeroLite Aero Luna Royale Classic Fluent)
fi

project="$root/samples/AvaWpf.Gallery.Desktop"
dotnet build "$project" -c Release --nologo -v quiet >/dev/null
mkdir -p "$out"

for t in "${themes[@]}"; do
    for p in "${pages[@]}"; do
        name="$t.${scheme:-default}.$variant.${p// /_}.png"
        args=(--theme "$t" --variant "$variant" --page "$p" --size "$size" --screenshot "$out/$name")
        if [ -n "$scheme" ]; then
            args+=(--scheme "$scheme")
        fi

        dotnet run --project "$project" -c Release --no-build -- "${args[@]}"
    done
done
